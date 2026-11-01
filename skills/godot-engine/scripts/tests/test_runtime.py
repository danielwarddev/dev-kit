import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from runtime import isolated_settings, run_scenario, validate_scenario
from operations import run_engine


class RequestTests(unittest.TestCase):
    def test_rejects_unsafe_paths_unknown_fields_and_invalid_steps(self):
        valid = {"scene": "res://main.tscn", "steps": [{"op": "wait", "frames": 1}]}
        validate_scenario(valid)
        for scene in (None, 12, "main.tscn", "res://../main.tscn", "res://x\\main.tscn"):
            with self.subTest(scene=scene), self.assertRaises(ValueError):
                validate_scenario(dict(valid, scene=scene))
        for step in (
            {"op": "wait", "frames": True}, {"op": "wait", "frames": 0},
            {"op": "wait", "frames": 1, "unused": True}, {"op": "unknown"},
            {"op": "capture", "name": "../outside"},
            {"op": "sample", "node": "..", "property": "visible"},
        ):
            with self.subTest(step=step), self.assertRaises(ValueError):
                validate_scenario(dict(valid, steps=[step]))

    def test_rejects_duplicate_capture_names(self):
        with self.assertRaisesRegex(ValueError, "unique"):
            validate_scenario({"scene": "res://main.tscn", "steps": [
                {"op": "capture", "name": "same"}, {"op": "capture", "name": "same"},
            ]})

    def test_save_settings_replace_feature_overrides_without_changing_other_settings(self):
        result = isolated_settings(
            ' [application] ; Settings\nconfig/name="Original"\nconfig/use_custom_user_dir.windows = false\n'
            'config/custom_user_dir_name = "RealSaves"\n[display]\nwindow/size/viewport_width=320\n',
            "isolated",
        )
        self.assertIn('config/name="Original"', result)
        self.assertNotIn("RealSaves", result)
        self.assertNotIn(".windows", result)
        self.assertIn('config/custom_user_dir_name="isolated"', result)
        self.assertIn("window/size/viewport_width=320", result)

    def test_rejects_headless_capture_and_existing_output(self):
        with tempfile.TemporaryDirectory() as temporary:
            project = Path(temporary) / "project"
            project.mkdir()
            (project / "project.godot").write_text("config_version=5\n")
            with self.assertRaisesRegex(ValueError, "rendered"):
                run_scenario(project, {"scene": "res://main.tscn", "steps": [
                    {"op": "capture", "name": "scene"},
                ]}, Path(temporary) / "output", headless=True)
            with self.assertRaisesRegex(ValueError, "rendered"):
                run_scenario(project, {"scene": "res://main.tscn", "steps": [
                    {"op": "click", "node": "UI/Button"},
                ]}, Path(temporary) / "output", headless=True)
            with self.assertRaises(FileExistsError):
                run_scenario(project, {"scene": "res://main.tscn", "steps": [
                    {"op": "wait", "frames": 1},
                ]}, Path(temporary))

    def test_timeout_preserves_output_and_restores_no_source_files(self):
        with tempfile.TemporaryDirectory() as temporary:
            project = Path(temporary) / "project"
            project.mkdir()
            settings = project / "project.godot"
            settings.write_text("config_version=5\n")
            output = Path(temporary) / "output"
            with patch("runtime.subprocess.run", side_effect=subprocess.TimeoutExpired(
                "godot", 1, output=b"partial", stderr=b"details",
            )), self.assertRaisesRegex(RuntimeError, "timed out"):
                run_scenario(project, {"scene": "res://main.tscn", "steps": [
                    {"op": "wait", "frames": 1},
                ]}, output, timeout=1)
            self.assertEqual((output / "stdout.log").read_text(), "partial")
            self.assertEqual(settings.read_text(), "config_version=5\n")

    def test_engine_shutdown_error_invalidates_an_otherwise_completed_report(self):
        with tempfile.TemporaryDirectory() as temporary:
            project = Path(temporary) / "project"
            project.mkdir()
            (project / "project.godot").write_text("config_version=5\n")
            output = Path(temporary) / "output"

            def failing_engine(arguments, **kwargs):
                request = json.loads(Path(arguments[-1]).read_text())
                (output / "result.json").write_text(json.dumps({
                    "run_id": request["run_id"], "complete": True,
                }))
                return subprocess.CompletedProcess(arguments, 0, "GODOT_RECIPE_OK", "ERROR: leaked resource")

            with patch("runtime.subprocess.run", side_effect=failing_engine), self.assertRaises(RuntimeError):
                run_scenario(project, {"scene": "res://main.tscn", "steps": [
                    {"op": "wait", "frames": 1},
                ]}, output)
            self.assertFalse(json.loads((output / "result.json").read_text())["complete"])


@unittest.skipUnless(os.environ.get("GODOT_PATH"), "Set GODOT_PATH for engine-backed recipes")
class RuntimeTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix="recipe-tests-")
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        self.project = self.root / "project"
        self.project.mkdir()
        self.settings = (
            'config_version=5\n[application]\nconfig/name="RecipeFixture"\n'
            '[display]\nwindow/size/viewport_width=160\nwindow/size/viewport_height=120\n'
            '[rendering]\nrenderer/rendering_method="gl_compatibility"\n'
            '[input]\nactivate={"deadzone":0.5,"events":[Object(InputEventKey,"physical_keycode":32)]}\n'
        )
        (self.project / "project.godot").write_text(self.settings)
        (self.project / "main.gd").write_text(
            'extends Node2D\nvar key_count := 0\nvar click_count := 0\nvar ticks := 0\nvar ready_for_input := false\n'
            'func _ready():\n'
            '\t$UI/Button.pressed.connect(func(): click_count += 1)\n'
            '\tvar file := FileAccess.open("user://save.txt", FileAccess.WRITE)\n'
            '\tfile.store_string("disposable")\n'
            'func _input(event):\n'
            '\tif event.is_action_pressed("activate"):\n\t\tkey_count += 1\n',
        )
        with (self.project / "main.gd").open("a") as script:
            script.write('func _physics_process(_delta):\n\tticks += 1\n\tready_for_input = ticks >= 5\n')
        (self.project / "main.tscn").write_text(
            '[gd_scene load_steps=2 format=3]\n'
            '[ext_resource type="Script" path="res://main.gd" id="1"]\n'
            '[node name="Main" type="Node2D"]\nscript=ExtResource("1")\n'
            '[node name="Background" type="ColorRect" parent="."]\n'
            'offset_right=160.0\noffset_bottom=120.0\ncolor=Color(1,0,0,1)\nmouse_filter=2\n'
            '[node name="UI" type="CanvasLayer" parent="."]\n'
            '[node name="Button" type="Button" parent="UI"]\n'
            'offset_left=20.0\noffset_top=20.0\noffset_right=140.0\noffset_bottom=80.0\ntext="Click"\n',
        )

    def run_recipe(self, steps, *, headless=True):
        return run_scenario(self.project, {"scene": "res://main.tscn", "steps": steps},
                            self.root / "output", headless=headless, timeout=20)

    def test_mapped_keyboard_and_real_gui_click_and_save_isolation(self):
        result = self.run_recipe([
            {"op": "key", "action": "activate", "frames": 2},
            {"op": "assert", "node": ".", "property": "key_count", "equals": 1},
            {"op": "click", "node": "UI/Button"},
            {"op": "assert", "node": ".", "property": "click_count", "equals": 1},
            {"op": "sample", "node": "UI/Button", "property": "size:x"},
        ], headless=False)
        self.assertTrue(result["complete"])
        self.assertEqual(result["steps"][-1]["value"], 120)
        self.assertFalse(Path(result["user_data"]).exists())
        self.assertEqual((self.project / "project.godot").read_text(), self.settings)
        self.assertEqual(len(result["bypasses"]), 1)

    def test_action_state_is_explicitly_a_bypass_of_input_events(self):
        result = self.run_recipe([
            {"op": "action", "action": "activate", "frames": 2},
            {"op": "assert", "node": ".", "property": "key_count", "equals": 0},
        ])
        self.assertIn("bypasses key events", result["bypasses"][-1])

    def test_assertion_failure_preserves_partial_result_and_cleans_saves(self):
        with self.assertRaisesRegex(RuntimeError, "Assertion failed"):
            self.run_recipe([{"op": "assert", "node": ".", "property": "key_count", "equals": 99}])
        result = json.loads((self.root / "output" / "result.json").read_text())
        self.assertFalse(result["complete"])
        self.assertIn("observed 0", result["error"])
        self.assertFalse(Path(result["user_data"]).exists())

    def test_unknown_action_fails_instead_of_silently_doing_nothing(self):
        with self.assertRaisesRegex(RuntimeError, "Unknown input action"):
            self.run_recipe([{"op": "key", "action": "missing", "frames": 1}])

    def test_missing_scene_fails_without_success_marker(self):
        with self.assertRaisesRegex(RuntimeError, "failed"):
            run_scenario(self.project, {"scene": "res://missing.tscn", "steps": [
                {"op": "wait", "frames": 1},
            ]}, self.root / "output", headless=True, timeout=20)

    def test_waits_for_observed_readiness_instead_of_guessing_a_sleep(self):
        result = self.run_recipe([
            {"op": "wait_until", "node": ".", "property": "ready_for_input", "equals": True, "frames": 20},
            {"op": "assert", "node": ".", "property": "ready_for_input", "equals": True},
        ])
        self.assertGreaterEqual(result["steps"][0]["end_physics_frame"], result["steps"][0]["physics_frame"])

    def test_unmet_condition_fails_at_its_bound(self):
        with self.assertRaisesRegex(RuntimeError, "Condition timed out"):
            self.run_recipe([
                {"op": "wait_until", "node": ".", "property": "key_count", "equals": 1, "frames": 2},
            ])

    def test_rendered_capture_contains_viewport_pixels(self):
        result = self.run_recipe([{"op": "capture", "name": "screen"}], headless=False)
        self.assertEqual(result["steps"][0]["width"], 160)
        self.assertEqual(result["steps"][0]["height"], 120)
        image = (self.root / "output" / "screen.png").read_bytes()
        self.assertEqual(image[:8], b"\x89PNG\r\n\x1a\n")
        (self.project / "inspect.gd").write_text(
            'extends SceneTree\nfunc _initialize():\n'
            f'\tvar image := Image.load_from_file({json.dumps(str(self.root / "output" / "screen.png"))})\n'
            '\tif image.get_pixel(5,5) != Color.RED:\n\t\tpush_error("Capture did not render fixture content.")\n'
            '\t\tquit(1)\n\t\treturn\n\tprint("RENDER_CHECK_OK")\n\tquit(0)\n'
        )
        self.assertIn("RENDER_CHECK_OK", run_engine([
            "--headless", "--path", self.project, "--script", "res://inspect.gd",
        ]))

    def test_playtesting_cli_uses_shared_runtime(self):
        scenario = self.root / "scenario.json"
        scenario.write_text(json.dumps({"scene": "res://main.tscn", "steps": [
            {"op": "key", "action": "activate", "frames": 1},
            {"op": "assert", "node": ".", "property": "key_count", "equals": 1},
        ]}))
        script = Path(__file__).resolve().parents[3] / "godot-playtesting" / "scripts" / "run_scenario.py"
        result = subprocess.run([
            sys.executable, str(script), "--project", str(self.project), "--scenario", str(scenario),
            "--output", str(self.root / "output"), "--headless",
        ], capture_output=True, text=True, timeout=30)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertTrue(json.loads(result.stdout)["complete"])


if __name__ == "__main__":
    unittest.main()
