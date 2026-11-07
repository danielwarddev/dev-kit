import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET


KIT = Path(__file__).resolve().parents[2]
BASH = (
    r"C:\Program Files\Git\bin\bash.exe"
    if os.name == "nt" else shutil.which("bash")
)
ENVIRONMENT = os.environ | {
    "MSBUILDDISABLENODEREUSE": "1",
    "DOTNET_CLI_USE_MSBUILD_SERVER": "0",
    "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH": "false",
    "UseSharedCompilation": "false",
}
LOCAL_GIT = '''
    export GIT_CONFIG_GLOBAL=/dev/null GIT_CONFIG_NOSYSTEM=1
    export GIT_AUTHOR_NAME="Fixture" GIT_AUTHOR_EMAIL="fixture@example.invalid"
    export GIT_COMMITTER_NAME="$GIT_AUTHOR_NAME" GIT_COMMITTER_EMAIL="$GIT_AUTHOR_EMAIL"
'''


def run_bash(directory, command, *arguments, timeout=600):
    prelude = (
        'native_path() { cygpath -u "$1"; }; '
        if os.name == "nt" else 'native_path() { printf "%s" "$1"; }; '
    )
    return subprocess.run(
        [BASH, "-c", prelude + command, "test", *map(str, arguments)],
        cwd=directory, env=ENVIRONMENT, capture_output=True, text=True, timeout=timeout,
    )


@unittest.skipUnless(BASH and shutil.which("dotnet"), "Requires Bash and .NET")
class ScriptTests(unittest.TestCase):
    def setUp(self):
        self.workspace = tempfile.TemporaryDirectory(prefix="Dward script tests ")
        self.addCleanup(self.workspace.cleanup)
        self.root = Path(self.workspace.name)

    def assert_succeeded(self, result):
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def test_defaults_include_godot_reference_only_in_godot_mode(self):
        for mode in ("", "--godot"):
            with self.subTest(mode=mode):
                target = self.root / ("godot" if mode else "standard")
                target.mkdir()
                for _ in range(2):
                    self.assert_succeeded(run_bash(
                        self.root,
                        'bash "$(native_path "$1")" "$(native_path "$2")" "${@:3}"',
                        KIT / "scripts" / "apply-project-files", target,
                        *([mode] if mode else []),
                    ))
                skill = target / ".claude" / "skills" / "csharp-standards"
                self.assertEqual(
                    (skill / "references" / "godot-development.md").exists(),
                    bool(mode),
                )
                self.assertEqual(
                    "godot-development.md" in (skill / "SKILL.md").read_text(),
                    bool(mode),
                )
                self.assertEqual(
                    (skill / "references" / "writing-csharp.md").read_bytes(),
                    (KIT / "skills" / "csharp-standards" / "references"
                     / "writing-csharp.md").read_bytes(),
                )

    def test_defaults_include_blazor_skill_only_in_blazor_mode(self):
        for mode in ("", "--godot", "--blazor"):
            with self.subTest(mode=mode):
                target = self.root / (mode.lstrip("-") or "standard")
                target.mkdir()
                for _ in range(2):
                    self.assert_succeeded(run_bash(
                        self.root,
                        'bash "$(native_path "$1")" "$(native_path "$2")" "${@:3}"',
                        KIT / "scripts" / "apply-project-files", target,
                        *([mode] if mode else []),
                    ))
                skill = target / ".claude" / "skills" / "blazor-frontend"
                self.assertEqual(skill.exists(), mode == "--blazor")
                if mode == "--blazor":
                    source = KIT / "skills" / "blazor-frontend"
                    for file in source.rglob("*"):
                        if file.is_file():
                            self.assertEqual(
                                (skill / file.relative_to(source)).read_bytes(),
                                file.read_bytes(),
                            )
                    (skill / "SKILL.md").write_text("custom Blazor skill", encoding="utf-8")
                    self.assert_succeeded(run_bash(
                        self.root,
                        'bash "$(native_path "$1")" "$(native_path "$2")" --blazor',
                        KIT / "scripts" / "apply-project-files", target,
                    ))
                    self.assertEqual((skill / "SKILL.md").read_text(), "custom Blazor skill")

    def test_defaults_preserve_existing_files_and_skill_directories(self):
        target = self.root / "existing project"
        target.mkdir()
        (target / "AGENTS.md").write_text("custom instructions", encoding="utf-8")
        skill = target / ".claude" / "skills" / "csharp-standards"
        skill.mkdir(parents=True)
        (skill / "SKILL.md").write_text("custom skill", encoding="utf-8")
        references = skill / "references"
        references.mkdir()
        (references / "godot-development.md").write_text("custom reference", encoding="utf-8")
        for _ in range(2):
            self.assert_succeeded(run_bash(
                self.root, 'bash "$(native_path "$1")" "$(native_path "$2")"',
                KIT / "scripts" / "apply-project-files", target,
            ))
        self.assertEqual((target / "AGENTS.md").read_text(), "custom instructions")
        self.assertEqual((skill / "SKILL.md").read_text(), "custom skill")
        self.assertEqual(sorted(path.name for path in skill.iterdir()), ["SKILL.md", "references"])
        self.assertEqual((references / "godot-development.md").read_text(), "custom reference")
        self.assertEqual(
            (target / ".editorconfig").read_bytes(),
            (KIT / "config" / ".editorconfig").read_bytes(),
        )

    def test_wrappers_generate_buildable_personalized_projects(self):
        hive = self.root / "hive"
        self.assert_succeeded(subprocess.run(
            ["dotnet", "new", "install", str(KIT / "templates"),
             "--debug:custom-hive", str(hive)],
            env=ENVIRONMENT, capture_output=True, text=True, timeout=60,
        ))
        command = '''
            export TEMPLATE_TEST_HIVE="$(native_path "$3")"
            dotnet() {
                if [[ "$1" == new ]]; then
                    command dotnet "$@" --debug:custom-hive "$TEMPLATE_TEST_HIVE"
                else
                    command dotnet "$@"
                fi
            }
            export -f dotnet
            bash "$(native_path "$1")" -n "$4" -o "$(native_path "$2")" "${@:5}"
        '''
        for starter in ("console", "library", "mudblazor"):
            for skip_solution in (False, True):
                with self.subTest(starter=starter, skip_solution=skip_solution):
                    name = starter.title() + ("Existing" if skip_solution else "Fresh")
                    flags = ["--skip-solution", "--skip-package-update"] if skip_solution else []
                    result = run_bash(
                        self.root, command,
                        KIT / "scripts" / f"new-ai-ready-{starter}-app"
                        if starter != "library" else KIT / "scripts" / "new-ai-ready-library",
                        self.root, hive, name, *flags,
                    )
                    self.assert_succeeded(result)
                    output = self.root / name
                    for filename in (".editorconfig", "Directory.Build.props", "AGENTS.md", "CLAUDE.md"):
                        self.assertEqual((output / filename).exists(), not skip_solution)
                        if not skip_solution:
                            self.assertEqual(
                                (output / filename).read_bytes(),
                                (KIT / "config" / filename).read_bytes(),
                            )
                    skills = output / ".claude" / "skills"
                    self.assertEqual(skills.exists(), not skip_solution)
                    if not skip_solution:
                        self.assertEqual(
                            sorted(path.name for path in skills.iterdir()),
                            (["blazor-frontend"] if starter == "mudblazor" else [])
                            + ["csharp-standards", "feature-specs"],
                        )
                        for skill in skills.iterdir():
                            source = KIT / "skills" / skill.name
                            for file in source.rglob("*"):
                                if file.is_file() and "__pycache__" not in file.parts and file.suffix not in (".pyc", ".pyo"):
                                    if skill.name == "csharp-standards":
                                        if file.name == "godot-development.md":
                                            self.assertFalse((skill / file.relative_to(source)).exists())
                                            continue
                                        if file.name == "SKILL.md":
                                            self.assertEqual(
                                                (skill / "SKILL.md").read_text(),
                                                "".join(
                                                    line for line in file.read_text().splitlines(keepends=True)
                                                    if not (line.startswith("|") and "godot-development.md" in line)
                                                ),
                                            )
                                            continue
                                    self.assertEqual(
                                        (skill / file.relative_to(source)).read_bytes(),
                                        file.read_bytes(),
                                    )
                    for project in output.glob("*/*.csproj"):
                        references = ET.parse(project).findall(".//PackageReference")
                        analyzer = next(
                            item for item in references if item.get("Include") == "Dward.Analyzers"
                        )
                        self.assertEqual(analyzer.get("PrivateAssets"), "all")
                        self.assertNotIn("*", analyzer.get("Version", ""))
                        self.assertFalse(any("Godot" in item.get("Include", "") for item in references))
                        self.assert_succeeded(subprocess.run(
                            ["dotnet", "build", str(project), "--no-restore", "-v", "quiet"],
                            cwd=output, env=ENVIRONMENT, capture_output=True, text=True, timeout=180,
                        ))
                        if ".UnitTests" in project.stem:
                            self.assert_succeeded(subprocess.run(
                                ["dotnet", "test", str(project), "--no-build",
                                 "--no-restore", "-v", "quiet"],
                                cwd=output, env=ENVIRONMENT, capture_output=True, text=True, timeout=180,
                            ))
                    result = run_bash(
                        self.root, command,
                        KIT / "scripts" / "new-ai-ready-library",
                        self.root, hive, name, "--skip-package-update",
                    )
                    self.assertNotEqual(result.returncode, 0)
                    self.assertIn("Refusing to overwrite", result.stderr)

    def test_package_update_failure_is_not_reported_as_success(self):
        (self.root / "Consumer.csproj").write_text(
            '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup>'
            '<PackageReference Include="Missing.Package" Version="1.0.0" />'
            '</ItemGroup></Project>', encoding="utf-8",
        )
        result = run_bash(
            self.root,
            'dotnet() { return 1; }; export -f dotnet; '
            'bash "$(native_path "$1")" "$(native_path "$2")"',
            KIT / "scripts" / "update-packages", self.root,
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Completed with failures", result.stderr)

    def test_up_to_date_packages_are_restored_and_treated_as_success(self):
        (self.root / "Consumer.csproj").touch()
        result = run_bash(
            self.root, '''
            dotnet() {
                if [[ "$1" == package ]]; then return 2; fi
                echo "Restoring current packages"
            }
            export -f dotnet
            bash "$(native_path "$1")" "$(native_path "$2")"
            ''', KIT / "scripts" / "update-packages", self.root,
        )
        self.assert_succeeded(result)
        self.assertIn("Restoring current packages", result.stdout)

    def test_restore_failure_after_no_package_updates_is_propagated(self):
        (self.root / "Consumer.csproj").touch()
        result = run_bash(
            self.root, '''
            dotnet() {
                if [[ "$1" == package ]]; then return 2; fi
                return 1
            }
            export -f dotnet
            bash "$(native_path "$1")" "$(native_path "$2")"
            ''', KIT / "scripts" / "update-packages", self.root,
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Completed with failures", result.stderr)

    def test_wrapper_propagates_package_update_failure(self):
        result = run_bash(
            self.root, '''
            dotnet() {
                if [[ "$1" != new ]]; then return 1; fi
                while [[ "$1" != -o ]]; do shift; done
                mkdir -p "$2"
                printf '%s' '<Project><ItemGroup><PackageReference Include="Missing.Package" Version="1.0.0" /></ItemGroup></Project>' > "$2/Consumer.csproj"
            }
            export -f dotnet
            bash "$(native_path "$1")" -n Failure -o "$(native_path "$2")"
            ''', KIT / "scripts" / "new-ai-ready-library", self.root,
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Completed with failures", result.stderr)
        self.assertNotIn("is ready at", result.stdout)

    def test_ghinit_local_only_is_repeatable_and_preserves_ignore(self):
        (self.root / ".gitignore").write_text("custom-ignore\n", encoding="utf-8")
        for _ in range(2):
            self.assert_succeeded(run_bash(
                self.root, LOCAL_GIT + 'bash "$(native_path "$1")" --localOnly',
                KIT / "scripts" / "ghinit",
            ))
        self.assertEqual((self.root / ".gitignore").read_text(), "custom-ignore\n")
        result = subprocess.run(
            ["git", "-C", str(self.root), "remote"],
            capture_output=True, text=True, timeout=30,
        )
        self.assert_succeeded(result)
        self.assertEqual(result.stdout.strip(), "")

    def test_godot_bootstrap_and_restructure_preserve_defaults_on_rerun(self):
        engine = shutil.which("godot-mono")
        if not engine:
            self.skipTest("Requires the Godot .NET CLI")
        for bootstrap in (False, True):
            with self.subTest(bootstrap=bootstrap):
                parent = self.root / ("bootstrap" if bootstrap else "restructure")
                parent.mkdir()
                name = "Fixture"
                root = parent / name if bootstrap else parent
                if not bootstrap:
                    (root / "project.godot").write_text(
                        'config_version=5\n[application]\nconfig/name="Fixture"\n'
                        '[dotnet]\nproject/assembly_name="Fixture"\n',
                        encoding="utf-8",
                    )
                    (root / ".editorconfig").write_text("root = true\n", encoding="utf-8")
                args = ["--bootstrap", name] if bootstrap else []
                result = run_bash(
                    parent, LOCAL_GIT
                    + 'export GODOT_PATH="$(native_path "$2")"; '
                    + 'bash "$(native_path "$1")" "${@:3}"',
                    KIT / "scripts" / "godotcsharp", engine, *args,
                )
                self.assert_succeeded(result)
                skills = root / ".claude" / "skills"
                self.assertEqual(
                    sorted(path.name for path in skills.iterdir()),
                    ["aseprite", "csharp-standards", "feature-specs", "godot-engine",
                     "godot-playtesting", "pixel-art-design", "strudel-music"],
                )
                self.assertEqual(list(skills.rglob("__pycache__")), [])
                for filename in ("AGENTS.md", "CLAUDE.md", "Directory.Build.props"):
                    self.assertEqual(
                        (root / filename).read_bytes(),
                        (KIT / "config" / filename).read_bytes(),
                    )
                if not bootstrap:
                    self.assertEqual((root / ".editorconfig").read_text(), "root = true\n")
                else:
                    self.assertEqual(
                        (root / ".editorconfig").read_bytes(),
                        (KIT / "config" / ".editorconfig").read_bytes(),
                    )
                resources = [root / ".editorconfig", root / ".gitattributes",
                             root / name / "icon.svg"]
                for file in resources:
                    file.write_bytes(file.read_bytes() + b"\n")
                before = {file: file.read_bytes() for file in resources}
                projects = sorted(root.glob("*/*.csproj"))
                manifests = {file: file.read_bytes() for file in projects}
                for file in projects:
                    content = file.read_text(encoding="utf-8")
                    if 'Include="Dward.Godot.Analyzers"' in content:
                        file.write_text(
                            content.replace('Include="Dward.Godot.Analyzers"', 'Include="Dward.Godot"'),
                            encoding="utf-8",
                        )
                self.assert_succeeded(run_bash(
                    root, 'bash "$(native_path "$1")"',
                    KIT / "scripts" / "godotcsharp",
                ))
                for file, content in (before | manifests).items():
                    self.assertEqual(file.read_bytes(), content)
                self.assert_succeeded(subprocess.run(
                    ["dotnet", "build", str(root / f"{name}.sln"), "-v", "quiet"],
                    cwd=root, env=ENVIRONMENT, capture_output=True, text=True, timeout=240,
                ))
                for project in projects:
                    if project.parent.name.endswith(".Core"):
                        continue
                    self.assert_succeeded(subprocess.run(
                        ["dotnet", "test", str(project), "--no-build",
                         "--no-restore", "-v", "quiet"],
                        cwd=root, capture_output=True, text=True, timeout=240,
                        env=ENVIRONMENT | {"GODOT_BIN": engine},
                    ))


if __name__ == "__main__":
    unittest.main()
