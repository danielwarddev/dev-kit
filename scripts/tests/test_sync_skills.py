import importlib.util
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

from test_scripts import BASH, KIT, run_bash


def load_module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


sync = load_module("sync_skills", KIT / "scripts" / "sync-skills_Resources" / "sync.py")
installer = load_module("sync_installer", KIT / "install.py")


class SyncSkillsTests(unittest.TestCase):
    def setUp(self):
        workspace = tempfile.TemporaryDirectory(prefix="Skill sync tests ")
        self.addCleanup(workspace.cleanup)
        self.root = Path(workspace.name)
        self.kit = self.root / "dev kit"
        for name in ("skills/example", "config", "templates", ".git"):
            (self.kit / name).mkdir(parents=True)
        shutil.copytree(KIT / "scripts", self.kit / "scripts", ignore=shutil.ignore_patterns("tests"))
        (self.kit / "LICENSE").write_text("license")
        (self.kit / "skills" / "example" / "SKILL.md").write_text("installed skill")
        (self.kit / "config" / "AGENTS.md").write_text("installed instructions")
        (self.kit / "config" / ".editorconfig").write_text("installed config")
        self.project = self.root / "project"
        self.project.mkdir()

    @unittest.skipUnless(BASH, "Requires Bash")
    def test_deployed_copy_and_explicit_send_replace_matching_files_only(self):
        deployed = self.root / "installed kit"
        with patch.object(installer, "KIT", self.kit):
            installer.deploy_files(deployed)
        (self.kit / "skills" / "example" / "SKILL.md").write_text("uninstalled change")
        skills = self.project / ".claude" / "skills"
        (skills / "example").mkdir(parents=True)
        (skills / "example" / "obsolete").touch()
        (skills / "unrelated").mkdir()
        (skills / "unrelated" / "SKILL.md").write_text("local unrelated")
        (self.project / "CLAUDE.md").write_text("local Claude")
        (self.project / "Directory.Build.props").write_text("local build")
        for _ in range(2):
            result = run_bash(
                self.project, 'bash "$(native_path "$1")"',
                deployed / "pull-project-files",
            )
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual((skills / "example" / "SKILL.md").read_text(), "installed skill")
        self.assertFalse((skills / "example" / "obsolete").exists())
        self.assertEqual((skills / "unrelated" / "SKILL.md").read_text(), "local unrelated")
        self.assertEqual((self.project / "CLAUDE.md").read_text(), "local Claude")
        self.assertEqual((self.project / "Directory.Build.props").read_text(), "local build")
        (skills / "example" / "SKILL.md").write_text("improved skill")
        (skills / "example" / "node_modules").mkdir()
        (skills / "example" / "node_modules" / "dependency.js").touch()
        (self.project / "AGENTS.md").write_text("improved instructions")
        (self.project / ".editorconfig").write_text("improved config")
        (self.kit / "skills" / "example" / "obsolete").touch()
        (self.kit / "skills" / "repo-only").mkdir()
        (self.kit / "skills" / "repo-only" / "SKILL.md").write_text("repo only")
        for explicit_project in (False, True):
            result = run_bash(
                self.root if explicit_project else self.project,
                'bash "$(native_path "$1")" "$(native_path "$2")" "${@:3}"',
                deployed / "push-project-files", self.kit,
                *([self.project] if explicit_project else []),
            )
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual((self.kit / "skills" / "example" / "SKILL.md").read_text(), "improved skill")
        self.assertFalse((self.kit / "skills" / "example" / "obsolete").exists())
        self.assertFalse((self.kit / "skills" / "example" / "node_modules").exists())
        self.assertEqual((self.kit / "skills" / "repo-only" / "SKILL.md").read_text(), "repo only")
        self.assertEqual((self.kit / "skills" / "unrelated" / "SKILL.md").read_text(), "local unrelated")
        self.assertEqual((self.kit / "config" / "AGENTS.md").read_text(), "improved instructions")
        self.assertEqual((self.kit / "config" / ".editorconfig").read_text(), "improved config")
        self.assertEqual((deployed / "skills" / "example" / "SKILL.md").read_text(), "installed skill")

    @unittest.skipUnless(BASH, "Requires Bash")
    def test_checkout_copy_and_missing_send_destination(self):
        result = run_bash(
            self.root, 'bash "$(native_path "$1")" "$(native_path "$2")"',
            self.kit / "scripts" / "pull-project-files", self.project,
        )
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        result = run_bash(self.project, 'bash "$(native_path "$1")"', self.kit / "scripts" / "push-project-files")
        self.assertNotEqual(result.returncode, 0)
        result = run_bash(
            self.project, 'bash "$(native_path "$1")" "$(native_path "$2")"',
            self.kit / "scripts" / "push-project-files", self.root,
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Expected a dev-kit Git checkout", result.stderr)

    def test_missing_config_is_rejected_before_overwriting(self):
        (self.project / "AGENTS.md").write_text("keep")
        (self.kit / "config" / ".editorconfig").unlink()
        with self.assertRaisesRegex(ValueError, "Missing source file"):
            sync.transfer(
                self.kit / "skills", self.kit / "config",
                self.project / ".claude" / "skills", self.project,
            )
        self.assertEqual((self.project / "AGENTS.md").read_text(), "keep")
        self.assertFalse((self.project / ".claude").exists())

    def test_overlapping_paths_are_rejected(self):
        with self.assertRaisesRegex(ValueError, "overlap"):
            sync.transfer(
                self.kit / "skills", self.kit / "config",
                self.kit / "skills" / ".." / "skills", self.project,
            )

    def test_linked_skill_is_rejected_without_changing_destination(self):
        linked = self.kit / "skills" / "linked"
        try:
            linked.symlink_to(self.kit / "skills" / "example", target_is_directory=True)
        except OSError:
            self.skipTest("Creating directory symlinks is unavailable")
        (self.project / "AGENTS.md").write_text("keep")
        with self.assertRaisesRegex(ValueError, "Linked paths"):
            sync.transfer(
                self.kit / "skills", self.kit / "config",
                self.project / ".claude" / "skills", self.project,
            )
        self.assertEqual((self.project / "AGENTS.md").read_text(), "keep")
        self.assertFalse((self.project / ".claude").exists())

    def test_failed_replacement_restores_previous_files(self):
        target = self.project / ".claude" / "skills" / "example"
        target.mkdir(parents=True)
        (target / "SKILL.md").write_text("keep skill")
        (self.project / "AGENTS.md").write_text("keep instructions")
        rename = Path.rename

        def fail_config_replacement(path, destination):
            if path.name == "AGENTS.md" and path.parent.name.startswith(".sync-skills-"):
                raise PermissionError("locked file")
            return rename(path, destination)

        with patch.object(Path, "rename", fail_config_replacement):
            with self.assertRaisesRegex(PermissionError, "locked file"):
                sync.transfer(
                    self.kit / "skills", self.kit / "config",
                    target.parent, self.project,
                )
        self.assertEqual((target / "SKILL.md").read_text(), "keep skill")
        self.assertEqual((self.project / "AGENTS.md").read_text(), "keep instructions")
        self.assertFalse((self.project / ".editorconfig").exists())
        self.assertFalse(list(target.parent.parent.glob(".sync-skills-*")))
