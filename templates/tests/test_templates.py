import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET


def run_dotnet(directory, *arguments):
    environment = os.environ.copy()
    environment["MSBUILDDISABLENODEREUSE"] = "1"
    environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0"
    return subprocess.run(
        ["dotnet", *map(str, arguments)], cwd=directory, env=environment,
        capture_output=True, text=True, timeout=180,
    )


@unittest.skipUnless(shutil.which("dotnet"), "Requires the .NET SDK")
class TemplateConsumerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workspace = tempfile.TemporaryDirectory(prefix="Dward template tests ")
        cls.addClassCleanup(cls.workspace.cleanup)
        cls.root = Path(cls.workspace.name)
        cls.hive = cls.root / "template-hive"
        cls.kit = Path(__file__).resolve().parents[2]
        result = run_dotnet(
            cls.root, "new", "install", cls.kit,
            "--debug:custom-hive", cls.hive,
        )
        if result.returncode:
            raise AssertionError(result.stdout + result.stderr)

    def assert_succeeded(self, result):
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def test_generated_projects_consume_analyzers_with_and_without_solution(self):
        for short_name in ("ai-ready-console", "ai-ready-library", "ai-ready-mudblazor"):
            for skip_solution in (False, True):
                with self.subTest(template=short_name, skip_solution=skip_solution):
                    name = short_name.removeprefix("ai-ready-").title() + (
                        "Existing" if skip_solution else "Fresh"
                    )
                    output = self.root / name
                    arguments = [
                        "new", short_name, "-n", name, "-o", output,
                        "--SkipRestore", "--debug:custom-hive", self.hive,
                    ]
                    if skip_solution:
                        arguments.append("--SkipSolution")
                    self.assert_succeeded(run_dotnet(self.root, *arguments))
                    projects = sorted(output.glob("*/*.csproj"))
                    self.assertEqual(len(projects), 2)
                    for project in projects:
                        manifest = ET.parse(project)
                        references = manifest.findall(".//PackageReference")
                        analyzer_references = [
                            item for item in references
                            if item.get("Include") == "Dward.Analyzers"
                        ]
                        self.assertEqual(len(analyzer_references), 1)
                        self.assertEqual(analyzer_references[0].get("PrivateAssets"), "all")
                        self.assertFalse(any(
                            "Godot" in item.get("Include", "") for item in references
                        ))
                        self.assertEqual(manifest.findall(".//Analyzer"), [])
                        self.assertEqual(manifest.findall(".//Import"), [])
                    self.assertEqual((output / "AGENTS.md").exists(), not skip_solution)
                    self.assertEqual((output / "Directory.Build.props").exists(), not skip_solution)
                    self.assertEqual((output / ".claude").exists(), not skip_solution)
                    self.assertEqual(bool(list(output.glob("*.slnx"))), not skip_solution)
                    self.assertEqual(list(output.rglob("NuGet.Config")), [])
                    self.assertEqual(list(output.rglob("*.dll")), [])
                    self.assertEqual(list(output.rglob("*.nupkg")), [])
                    self.assertEqual(list(output.rglob("*.targets")), [])
                    skills = output / ".claude" / "skills"
                    if not skip_solution:
                        self.assertEqual(
                            sorted(path.name for path in skills.iterdir()),
                            ["csharp-standards", "feature-specs"],
                        )
                        for skill in ("csharp-standards", "feature-specs"):
                            canonical = self.kit / "skills" / skill
                            for source in canonical.rglob("*"):
                                if source.is_file():
                                    generated = skills / skill / source.relative_to(canonical)
                                    self.assertEqual(
                                        generated.read_bytes(), source.read_bytes(),
                                        str(generated),
                                    )

                    for project in projects:
                        self.assert_succeeded(run_dotnet(output, "restore", project, "-v", "quiet"))
                        self.assert_succeeded(run_dotnet(
                            output, "build", project, "--no-restore", "-v", "quiet",
                        ))
                        assets_path = project.parent / "obj" / "project.assets.json"
                        assets = json.loads(assets_path.read_text(encoding="utf-8-sig"))
                        self.assertIn("Dward.Analyzers/1.0.0", assets["libraries"])
                        self.assertFalse(any("Godot" in key for key in assets["libraries"]))
                        if ".UnitTests" in project.stem:
                            self.assert_succeeded(run_dotnet(
                                output, "test", project, "--no-build", "--no-restore", "-v", "quiet",
                            ))

                    project = next(
                        item for item in projects if ".UnitTests" not in item.stem
                    )
                    probe = project.parent / "AnalyzerProbe.cs"
                    probe.write_text(
                        "public sealed class AnalyzerProbe { }\n", encoding="utf-8",
                    )
                    result = run_dotnet(
                        output, "build", project, "--no-restore", "-v", "quiet",
                        "-p:TreatWarningsAsErrors=true",
                    )
                    self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                    self.assertIn("DWARD0004", result.stdout + result.stderr)
                    configuration = output / ".editorconfig"
                    configuration.write_text(
                        "root = true\n[*.cs]\n"
                        "dotnet_diagnostic.DWARD0004.severity = none\n",
                        encoding="utf-8",
                    )
                    result = run_dotnet(
                        output, "build", project, "--no-restore", "-v", "quiet",
                        "-p:TreatWarningsAsErrors=true",
                    )
                    self.assert_succeeded(result)
                    self.assertNotIn("DWARD0004", result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
