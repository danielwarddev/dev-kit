import importlib.util
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET
import zipfile
from unittest.mock import patch

from test_scripts import BASH, ENVIRONMENT, KIT, run_bash


spec = importlib.util.spec_from_file_location("kit_installer", KIT / "install.py")
installer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(installer)


class InstallTests(unittest.TestCase):
    def setUp(self):
        self.workspace = tempfile.TemporaryDirectory(prefix="Dward install tests ")
        self.addCleanup(self.workspace.cleanup)
        self.root = Path(self.workspace.name)

    def assert_succeeded(self, result):
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def test_profile_update_preserves_custom_content_and_replaces_old_location(self):
        profile = self.root / "profile"
        profile.write_text("export CUSTOM_SETTING=keep\n", encoding="utf-8")
        first = installer.profile_contents(profile, "/old kit/scripts")
        profile.write_text(first, encoding="utf-8")
        self.assertEqual(installer.profile_contents(profile, "/old kit/scripts"), first)
        updated = installer.profile_contents(profile, "/new kit's/scripts")
        self.assertIn("export CUSTOM_SETTING=keep", updated)
        self.assertNotIn("/old kit/scripts", updated)
        self.assertEqual(updated.count(installer.START), 1)
        profile.write_text(updated, encoding="utf-8")
        self.assertEqual(installer.profile_contents(profile, "/new kit's/scripts"), updated)
        if BASH:
            result = run_bash(
                self.root,
                'source "$(native_path "$1")"; source "$(native_path "$1")"; '
                'printf "%s" "$PATH"',
                profile,
            )
            self.assert_succeeded(result)
            self.assertEqual(result.stdout.split(":").count("/new kit's/scripts"), 1)

    def test_malformed_profile_is_rejected_without_overwriting_it(self):
        profile = self.root / "profile"
        original = "custom settings\n" + installer.START
        profile.write_text(original, encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "Malformed"):
            installer.profile_contents(profile, "/kit/scripts")
        self.assertEqual(profile.read_text(), original)

    def test_existing_login_profile_is_used_instead_of_shadowing_it(self):
        profile = self.root / ".profile"
        profile.touch()
        self.assertEqual(installer.default_profiles(self.root), [self.root / ".bashrc", profile])
        bash_login = self.root / ".bash_login"
        bash_login.touch()
        self.assertEqual(installer.default_profiles(self.root), [self.root / ".bashrc", bash_login])

    def test_package_comparison_ignores_zip_timestamps_but_detects_changed_content(self):
        first, second = self.root / "first.nupkg", self.root / "second.nupkg"
        for path, year in ((first, 2025), (second, 2026)):
            with zipfile.ZipFile(path, "w") as archive:
                archive.writestr(zipfile.ZipInfo("analyzer.dll", (year, 1, 1, 0, 0, 0)), b"original")
                archive.writestr(f"package/services/metadata/core-properties/{year}.psmdcp", b"metadata")
                archive.writestr("_rels/.rels", f"generated relationships {year}")
        self.assertTrue(installer.same_package(first, second))
        with zipfile.ZipFile(second, "w") as archive:
            archive.writestr("analyzer.dll", b"changed")
        self.assertFalse(installer.same_package(first, second))

    def test_deployment_is_an_independent_snapshot_and_updates_on_reinstall(self):
        source = self.root / "source"
        for name in ("scripts", "config", "skills", "templates"):
            (source / name).mkdir(parents=True)
        (source / "LICENSE").write_text("license", encoding="utf-8")
        command = source / "scripts" / "example"
        command.write_text("first", encoding="utf-8")
        (source / "scripts" / "tests").mkdir()
        (source / "config" / "AGENTS.md").write_text("instructions", encoding="utf-8")
        (source / "skills" / "__pycache__").mkdir()
        destination = self.root / ".dward dev kit"
        with patch.object(installer, "KIT", source):
            installer.deploy_files(destination)
            command.write_text("second", encoding="utf-8")
            self.assertEqual((destination / "example").read_text(), "first")
            (destination / "obsolete").touch()
            installer.deploy_files(destination)
        self.assertEqual((destination / "example").read_text(), "second")
        self.assertFalse((destination / "obsolete").exists())
        self.assertFalse((destination / "tests").exists())
        self.assertFalse((destination / "skills" / "__pycache__").exists())
        self.assertEqual((destination / "config" / "AGENTS.md").read_text(), "instructions")

    def test_deployment_refuses_unmanaged_directories_and_checkout_paths(self):
        destination = self.root / ".dward dev kit"
        destination.mkdir(parents=True)
        sentinel = destination / "keep"
        sentinel.write_text("unrelated", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "unmanaged"):
            installer.deploy_files(destination)
        self.assertEqual(sentinel.read_text(), "unrelated")
        with self.assertRaisesRegex(ValueError, "outside"):
            installer.validate_deployment(KIT / "deployment")

    def test_windows_path_preserves_unrelated_entries_and_removes_duplicates(self):
        destination = Path(r"C:\Users\me\.dward-dev-kit")
        previous = Path(r"C:\repos\dev-kit\scripts")
        current = f"C:\\Other;{previous};{destination};{destination}\\"
        updated = installer.windows_path_entries(current, destination, previous)
        self.assertEqual(updated, f"C:\\Other;{destination}")
        self.assertEqual(installer.windows_path_entries(updated, destination, previous), updated)

    @unittest.skipUnless(BASH and shutil.which("dotnet"), "Requires Bash and .NET")
    def test_install_repairs_partial_setup_and_repeats_without_duplicates(self):
        self.verify_install(partial=True)

    @unittest.skipUnless(BASH and shutil.which("dotnet"), "Requires Bash and .NET")
    def test_fresh_install_repeats_without_duplicates(self):
        self.verify_install(partial=False)

    def verify_install(self, partial):
        config = self.root / "NuGet.Config"
        feed = self.root / "feed"
        profile = self.root / "profile"
        user_home = self.root / "user home"
        user_home.mkdir()
        (user_home / "unrelated").write_text("keep", encoding="utf-8")
        destination = user_home / ".dward-dev-kit"
        if partial:
            config.write_text(
                '<configuration><!--keep--><packageSources>'
                '<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />'
                '<add key="DwardLocal" value="old-location" />'
                '</packageSources><disabledPackageSources>'
                '<add key="DwardLocal" value="true" /></disabledPackageSources>'
                '<config><add key="unrelated-setting" value="keep" /></config></configuration>',
                encoding="utf-8",
            )
        profile.write_text("export CUSTOM_SETTING=keep\n", encoding="utf-8")
        cli_home = self.root / "cli home"
        command = '''
            export DOTNET_CLI_HOME="$(native_path "$5")"
            bash "$(native_path "$1")" --nuget-config "$2" --feed "$3"                         --profile "$4" --destination "$6" --no-user-path
        '''
        snapshots = None
        for _ in range(2):
            result = run_bash(
                            self.root, command, KIT / "install", config, feed, profile, cli_home, destination,
                timeout=600,
            )
            self.assert_succeeded(result)
            self.assertIn("Installation complete", result.stdout)
            self.assertEqual(len(list(feed.glob("*.nupkg"))), 2)
            tree = ET.parse(config)
            source = [
                item for item in tree.findall("./packageSources/add")
                if item.get("key") == "DwardLocal"
            ]
            self.assertEqual(len(source), 1)
            self.assertEqual(source[0].get("value"), str(feed))
            self.assertEqual(
                len(tree.findall('./packageSourceMapping/packageSource[@key="DwardLocal"]/package[@pattern="Dward.*"]')),
                1,
            )
            self.assertEqual(
                len(tree.findall('./packageSourceMapping/packageSource[@key="nuget.org"]/package[@pattern="*"]')),
                1,
            )
            self.assertFalse(any(
                item.get("key") == "DwardLocal" and item.get("value", "").lower() == "true"
                for item in tree.findall("./disabledPackageSources/add")
            ))
            if partial:
                self.assertEqual(
                    tree.find('./config/add[@key="unrelated-setting"]').get("value"), "keep",
                )
                self.assertIn("<!--keep-->", config.read_text())
            self.assertEqual(profile.read_text().count(installer.START), 1)
            self.assertIn("export CUSTOM_SETTING=keep", profile.read_text())
            self.assertEqual((user_home / "unrelated").read_text(), "keep")
            self.assertTrue((destination / "new-ai-ready-library").is_file())
            self.assertNotIn(str(KIT / "scripts"), profile.read_text())
            current = {path: path.read_bytes() for path in [config, profile, *feed.glob("*.nupkg")]}
            if snapshots is not None:
                self.assertEqual(current, snapshots)
            snapshots = current
        environment = ENVIRONMENT | {"DOTNET_CLI_HOME": str(cli_home)}
        output = self.root / "Consumer"
        result = subprocess.run(
            ["dotnet", "new", "ai-ready-library", "-n", "Consumer", "-o", str(output), "--SkipRestore"],
            cwd=self.root, env=environment, capture_output=True, text=True, timeout=60,
        )
        self.assert_succeeded(result)
        project = output / "Consumer" / "Consumer.csproj"
        self.assert_succeeded(subprocess.run(
            ["dotnet", "restore", str(project), "--configfile", str(config),
             "--packages", str(self.root / "isolated packages")],
            cwd=self.root, env=environment, capture_output=True, text=True, timeout=180,
        ))
        result = run_bash(
            self.root,
            'source "$(native_path "$1")"; apply-project-files "$(native_path "$2")"',
            profile, output,
        )
        self.assert_succeeded(result)
        self.assertEqual(
            (output / "AGENTS.md").read_bytes(),
            (destination / "config" / "AGENTS.md").read_bytes(),
        )
        self.assert_succeeded(subprocess.run(
            ["dotnet", "build", str(project), "--no-restore", "-v", "quiet"],
            cwd=self.root, env=environment, capture_output=True, text=True, timeout=180,
        ))


if __name__ == "__main__":
    unittest.main()
