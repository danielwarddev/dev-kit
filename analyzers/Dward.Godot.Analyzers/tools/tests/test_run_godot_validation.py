import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
from pathlib import Path
from xml.sax.saxutils import escape

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from run_godot_validation import literal, make_request, run_engine


class GodotValidationTests(unittest.TestCase):
    def test_success_message_includes_elapsed_seconds(self):
        result = subprocess.CompletedProcess([], 0, stdout="GODOT_VALIDATION_OK: 1 scenes\n", stderr="")
        with patch("run_godot_validation.shutil.which", return_value=sys.executable), \
                patch("run_godot_validation.subprocess.run", return_value=result) as launch, \
                patch("run_godot_validation.time.perf_counter", side_effect=[10.0, 11.25]):
            output = run_engine(Path.cwd(), "godot", {})
        self.assertEqual(output, "GODOT_VALIDATION_OK: 1 scenes (1.25s)")
        arguments = launch.call_args.args[0]
        self.assertEqual(
            Path(arguments[arguments.index("--script") + 1]),
            Path(__file__).resolve().parents[1] / "godot_validation.gd",
        )

    def test_failure_message_includes_elapsed_seconds_and_preserves_details(self):
        result = subprocess.CompletedProcess([], 1, stdout="", stderr="Missing resource")
        with patch("run_godot_validation.shutil.which", return_value=sys.executable), \
                patch("run_godot_validation.subprocess.run", return_value=result), \
                patch("run_godot_validation.time.perf_counter", side_effect=[10.0, 12.5]):
            with self.assertRaisesRegex(RuntimeError, r"failed \(2\.50s\):\nMissing resource"):
                run_engine(Path.cwd(), "godot", {})

    def test_missing_completion_explains_why_zero_exit_is_not_success(self):
        result = subprocess.CompletedProcess([], 0, stdout="", stderr="")
        with patch("run_godot_validation.shutil.which", return_value=sys.executable), \
                patch("run_godot_validation.subprocess.run", return_value=result):
            with self.assertRaisesRegex(RuntimeError, "No GODOT_VALIDATION_OK completion marker") as error:
                run_engine(Path.cwd(), "godot", {})
        self.assertIn("zero exit code alone", str(error.exception))

    def test_timeout_message_includes_elapsed_seconds(self):
        with patch("run_godot_validation.shutil.which", return_value=sys.executable), \
                patch("run_godot_validation.subprocess.run", side_effect=subprocess.TimeoutExpired("godot", 120)), \
                patch("run_godot_validation.time.perf_counter", side_effect=[10.0, 130.0]):
            with self.assertRaisesRegex(RuntimeError, r"timed out \(120\.00s\)"):
                run_engine(Path.cwd(), "godot", {})

    def test_missing_godot_executable_reports_configuration_error(self):
        with self.assertRaisesRegex(RuntimeError, "Godot executable not found"):
            run_engine(Path.cwd(), "nonexistent-godot-validation-test-executable", {})

    def test_csharp_escape_sequences_do_not_require_json_compatible_strings(self):
        self.assertEqual(literal(r'"j\u0075mp"'), "jump")
        self.assertEqual(literal(r'"\0\a\v\x41\U00000042"'), "\0\a\vAB")

    def test_request_excludes_generated_files_and_retains_uid_path_pairs(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / ".godot").mkdir()
            (root / ".godot" / "ignored.cs").write_text('Input.ActionPress("ignored");')
            (root / "scene.tscn").write_text(
                '[ext_resource type="Script" uid="uid://abc" path="res://actor.gd" id="1"]\n'
            )
            (root / "actor.gd").write_text(
                '# "res://comment"\nvar path = "res://asset.png"\nInput.action_press("jump")\n'
            )
            request = make_request(root)
            self.assertEqual(request["scenes"], ["res://scene.tscn"])
            self.assertNotIn("actions", request)
            self.assertTrue(any(item.get("uid") == "uid://abc" for item in request["resources"]))
            self.assertFalse(any(item["path"] == "res://comment" for item in request["resources"]))


@unittest.skipUnless(shutil.which("dotnet"), "Requires MSBuild through dotnet")
class GodotValidationBuildTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix="godot validation build ")
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        shutil.copyfile(
            Path(__file__).resolve().parents[1] / "GodotValidation.targets",
            self.root / "GodotValidation.targets",
        )
        (self.root / "run_godot_validation.py").write_text(
            'import sys\nprint("BUILD_GODOT_VALIDATION_PROBE", flush=True)\n'
            'sys.exit(1 if sys.argv[-1] == "fail" else 0)\n',
            encoding="utf-8",
        )
        self.project = self.root / "fixture.proj"
        self.project.write_text(
            '<Project><PropertyGroup>'
            f'<PythonExecutable>{escape(sys.executable)}</PythonExecutable>'
            '<GodotExecutable>pass</GodotExecutable>'
            '</PropertyGroup><Target Name="Build">'
            '<Error Condition="\'$(FailBuild)\' == \'true\'" Text="BUILD_FAILURE_PROBE" />'
            '</Target>'
            '<Import Project="GodotValidation.targets" /></Project>',
            encoding="utf-8",
        )

    def build(self, *properties):
        return subprocess.run(
            ["dotnet", "msbuild", str(self.project), "-t:Build", "-nologo", "-v:minimal", *properties],
            capture_output=True, text=True, timeout=60,
        )

    def test_normal_build_skips_validator_by_default(self):
        result = self.build(
            "-p:GodotExecutable=fail",
            f"-p:PythonExecutable={self.root / 'missing python.exe'}",
        )
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertNotIn("BUILD_GODOT_VALIDATION_PROBE", result.stdout)

    def test_opted_in_normal_build_runs_validator_once(self):
        result = self.build("-p:RunGodotValidation=true")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(result.stdout.count("BUILD_GODOT_VALIDATION_PROBE"), 1)

    def test_failed_build_does_not_run_validator(self):
        result = self.build("-p:RunGodotValidation=true", "-p:FailBuild=true")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("BUILD_FAILURE_PROBE", result.stdout)
        self.assertNotIn("BUILD_GODOT_VALIDATION_PROBE", result.stdout)

    def test_validator_failure_fails_build(self):
        result = self.build("-p:RunGodotValidation=true", "-p:GodotExecutable=fail")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("BUILD_GODOT_VALIDATION_PROBE", result.stdout)

    def test_missing_python_explains_installation_and_executable_configuration(self):
        result = self.build(
            "-p:RunGodotValidation=true",
            f"-p:PythonExecutable={self.root / 'missing python.exe'}",
        )
        output = result.stdout + result.stderr
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Cannot run Python executable", output)
        self.assertIn("Install Python 3", output)
        self.assertIn("-p:PythonExecutable=", output)
        self.assertNotIn("BUILD_GODOT_VALIDATION_PROBE", output)

    def test_design_time_and_outer_builds_and_explicit_skip_do_not_run_validator(self):
        for properties in (
            ("-p:RunGodotValidation=true", "-p:DesignTimeBuild=true"),
            ("-p:RunGodotValidation=true", "-p:IsCrossTargetingBuild=true"),
            ("-p:RunGodotValidation=false",),
        ):
            with self.subTest(properties=properties):
                result = self.build(
                    *properties, "-p:GodotExecutable=fail",
                    f"-p:PythonExecutable={self.root / 'missing python.exe'}",
                )
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertNotIn("BUILD_GODOT_VALIDATION_PROBE", result.stdout)


@unittest.skipUnless(os.environ.get("GODOT_PATH"), "Set GODOT_PATH for engine-backed Godot validation tests")
class GodotValidationEngineTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        (self.root / "project.godot").write_text(
            'config_version=5\n[input]\njump={"deadzone":0.5,"events":[]}\n'
        )
        (self.root / "actor.gd").write_text(
            'extends Node\n@export var target: Node\n@export var optional: Node\n'
        )
        self.scene = (
            '[gd_scene load_steps=2 format=3]\n'
            '[ext_resource type="Script" path="res://actor.gd" id="1"]\n'
            '[node name="Root" type="Node" node_paths=PackedStringArray("target")]\n'
            'script = ExtResource("1")\ntarget = NodePath("Child")\n'
            '[node name="Child" type="Node" parent="."]\n'
            '[node name="Other" type="Node" parent="."]\n'
        )
        (self.root / "base.tscn").write_text(self.scene)

    def validate(self):
        request = make_request(self.root)
        return run_engine(self.root, Path(os.environ["GODOT_PATH"]), request)

    def test_valid_assignments_and_omitted_optional_exports_pass(self):
        self.assertIn("GODOT_VALIDATION_OK", self.validate())

    def test_explicit_empty_optional_node_assignment_passes(self):
        (self.root / "base.tscn").write_text(
            self.scene.replace('target = NodePath("Child")', 'target = NodePath("")')
        )
        self.assertIn("GODOT_VALIDATION_OK", self.validate())

    def test_missing_node_paths_declaration_fails(self):
        (self.root / "base.tscn").write_text(
            self.scene.replace(' node_paths=PackedStringArray("target")', "")
        )
        with self.assertRaisesRegex(RuntimeError, "Export 'target' did not resolve") as error:
            self.validate()
        self.assertIn("node_paths declaration", str(error.exception))

    def test_missing_target_fails(self):
        (self.root / "base.tscn").write_text(self.scene.replace('NodePath("Child")', 'NodePath("Missing")'))
        with self.assertRaisesRegex(RuntimeError, "did not resolve") as error:
            self.validate()
        self.assertIn("renamed or moved target nodes", str(error.exception))

    def test_renamed_export_assignment_fails(self):
        (self.root / "actor.gd").write_text('extends Node\n@export var new_target: Node\n')
        with self.assertRaisesRegex(RuntimeError, "Unknown serialized property 'target'") as error:
            self.validate()
        self.assertIn("field/property that no longer exists", str(error.exception))
        self.assertIn("inherited/subscene override", str(error.exception))

    def test_inherited_and_instanced_scenes_allow_legitimate_overrides(self):
        (self.root / "derived.tscn").write_text(
            '[gd_scene load_steps=2 format=3]\n'
            '[ext_resource type="PackedScene" path="res://base.tscn" id="1"]\n'
            '[node name="Root" instance=ExtResource("1") node_paths=PackedStringArray("target")]\n'
            'target = NodePath("Other")\n'
        )
        (self.root / "host.tscn").write_text(
            '[gd_scene load_steps=2 format=3]\n'
            '[ext_resource type="PackedScene" path="res://base.tscn" id="1"]\n'
            '[node name="Host" type="Node"]\n'
            '[node name="Nested" parent="." instance=ExtResource("1") node_paths=PackedStringArray("target")]\n'
            'target = NodePath("Other")\n'
        )
        self.assertIn("GODOT_VALIDATION_OK", self.validate())

    def test_input_actions_are_left_to_the_analyzer(self):
        (self.root / "input.gd").write_text(
            'extends Node\nfunc example():\n\tInput.is_action_pressed("missing")\n'
        )
        self.assertIn("GODOT_VALIDATION_OK", self.validate())

    def test_missing_resource_and_stale_uid_fail(self):
        request = {"scenes": [], "resources": [
            {"path": "res://missing.png", "source": "fixture", "line": 1},
            {"path": "res://actor.gd", "uid": "uid://abc", "source": "fixture", "line": 2},
        ]}
        with self.assertRaisesRegex(RuntimeError, "Missing resource") as error:
            run_engine(self.root, Path(os.environ["GODOT_PATH"]), request)
        self.assertIn("Stale UID", str(error.exception))
        self.assertIn("UID and path do not identify the same asset", str(error.exception))
        self.assertIn("renamed, moved, or deleted", str(error.exception))

    def test_unresolved_uid_explains_metadata_and_import_checks(self):
        request = {"scenes": [], "resources": [
            {"path": "uid://abc", "source": "fixture", "line": 1},
        ]}
        with self.assertRaisesRegex(RuntimeError, "Unresolved UID") as error:
            run_engine(self.root, Path(os.environ["GODOT_PATH"]), request)
        self.assertIn("UID/import metadata", str(error.exception))
        self.assertIn("import the project", str(error.exception))

    def test_resource_cannot_escape_project(self):
        request = {"scenes": [], "resources": [
            {"path": "res://../outside", "source": "fixture", "line": 1},
        ]}
        with self.assertRaisesRegex(RuntimeError, "escapes project"):
            run_engine(self.root, Path(os.environ["GODOT_PATH"]), request)


if __name__ == "__main__":
    unittest.main()
