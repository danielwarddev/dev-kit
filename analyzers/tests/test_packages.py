import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
from xml.sax.saxutils import escape
import zipfile
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
class AnalyzerPackageTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workspace = tempfile.TemporaryDirectory(prefix="Dward package tests ")
        cls.addClassCleanup(cls.workspace.cleanup)
        cls.root = Path(cls.workspace.name)
        cls.feed = cls.root / "feed"
        analyzers = Path(__file__).resolve().parents[1]
        for project in ("Dward.Analyzers", "Dward.Godot.Analyzers"):
            result = run_dotnet(
                analyzers, "pack", analyzers / project / f"{project}.csproj",
                "--configuration", "Release", "--no-restore",
                "--output", cls.feed, "--verbosity", "quiet",
                "-p:UseSharedCompilation=false",
            )
            if result.returncode:
                raise AssertionError(result.stdout + result.stderr)

    def setUp(self):
        self.consumer = self.root / self._testMethodName
        self.consumer.mkdir()
        self.project = self.consumer / "Consumer.csproj"
        self.source = self.consumer / "Subject.cs"
        (self.consumer / "NuGet.Config").write_text(
            '<configuration><packageSources><clear />'
            f'<add key="test-feed" value="{escape(str(self.feed))}" />'
            '</packageSources><packageSourceMapping><clear /></packageSourceMapping>'
            '</configuration>', encoding="utf-8",
        )

    def create_consumer(self, package, code, diagnostic=None):
        self.project.write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            '<TargetFramework>net10.0</TargetFramework>'
            '<UseSharedCompilation>false</UseSharedCompilation>'
            '<NuGetAudit>false</NuGetAudit>'
            f'<RestorePackagesPath>{escape(str(self.root / "packages"))}</RestorePackagesPath>'
            '<PythonExecutable>nonexistent-package-test-python</PythonExecutable>'
            '<GodotExecutable>nonexistent-package-test-godot</GodotExecutable>'
            '</PropertyGroup><ItemGroup>'
            f'<PackageReference Include="{package}" Version="1.0.0" '
            'PrivateAssets="all" GeneratePathProperty="true" />'
            '</ItemGroup></Project>', encoding="utf-8",
        )
        self.source.write_text(code, encoding="utf-8")
        configuration = "root = true\n[*.cs]\n"
        if diagnostic:
            configuration += f"dotnet_diagnostic.{diagnostic}.severity = error\n"
        (self.consumer / ".editorconfig").write_text(configuration, encoding="utf-8")

    def build(self, *arguments):
        return run_dotnet(
            self.consumer, "build", self.project, "--verbosity", "quiet", *arguments,
        )

    def assert_packaged_fix(self, diagnostic, before, after):
        result = self.build()
        output = result.stdout + result.stderr
        self.assertNotEqual(result.returncode, 0, output)
        self.assertIn(diagnostic, output)
        self.assertEqual(self.source.read_text(encoding="utf-8"), before)
        result = run_dotnet(
            self.consumer, "format", "analyzers", self.project, "--no-restore",
            "--diagnostics", diagnostic, "--include", "Subject.cs", "--verbosity", "quiet",
        )
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(self.source.read_text(encoding="utf-8").strip(), after)
        result = self.build("--no-restore")
        output = result.stdout + result.stderr
        self.assertEqual(result.returncode, 0, output)
        self.assertNotIn(diagnostic, output)
        self.assertNotIn("Validating Godot", output)
        self.assertNotIn("CS8032", output)
        self.assertNotIn("AD0001", output)
        output_directory = self.consumer / "bin" / "Debug" / "net10.0"
        self.assertEqual([path.name for path in output_directory.glob("*.dll")], ["Consumer.dll"])

    def test_general_package_delivers_diagnostics_and_code_fixes(self):
        code = "public sealed class Subject { }"
        self.create_consumer("Dward.Analyzers", code, "DWARD0004")
        self.assert_packaged_fix("DWARD0004", code, "public class Subject { }")

    def test_godot_package_delivers_export_fixes_without_engine_validation(self):
        code = (
            "namespace Godot { public class ExportAttribute : System.Attribute { } }\n"
            "public class Subject\n{\n"
            "    [Godot.Export] public int Count = 3;\n"
            "    public int Read() => Count;\n}"
        )
        self.create_consumer("Dward.Godot.Analyzers", code, "DWARDGD0001")
        self.assert_packaged_fix(
            "DWARDGD0001", code, code.replace("public int Count", "private int Count"),
        )

    def test_packaged_test_configuration_requires_explicit_opt_in(self):
        self.create_consumer("Dward.Analyzers", "public class Subject { public int sut = 3; }")
        result = self.build()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertNotIn("DWARD0006", result.stdout + result.stderr)
        content = self.project.read_text(encoding="utf-8")
        self.project.write_text(
            content.replace(
                "</Project>",
                '<ItemGroup><GlobalAnalyzerConfigFiles '
                'Include="$(PkgDward_Analyzers)\\build\\Dward.Tests.globalconfig" />'
                '</ItemGroup></Project>',
            ), encoding="utf-8",
        )
        result = self.build("--no-restore")
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("DWARD0006", result.stdout + result.stderr)

    def test_packages_have_no_runtime_assets_or_dependencies(self):
        for package in self.feed.glob("*.nupkg"):
            with self.subTest(package=package.name), zipfile.ZipFile(package) as archive:
                self.assertFalse(any(
                    name.startswith(("lib/", "runtimes/")) for name in archive.namelist()
                ))
                manifest_name = next(name for name in archive.namelist() if name.endswith(".nuspec"))
                manifest = ET.fromstring(archive.read(manifest_name))
                self.assertEqual(manifest.findall(".//{*}dependency"), [])


if __name__ == "__main__":
    unittest.main()
