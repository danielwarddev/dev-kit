# Installs or updates the dev kit on your local machine. Can also be reran to repair existing setup.
#
# 1. Check required tools, .NET SDK 10+, NuGet configuration, and Bash profiles.
# 2. Builds and packs the analyzers then installs them into a local
#    NuGet feed (defaults to ~/.nuget/local-feed/Dward).
# 3. Add or repair the enabled DwardLocal NuGet source and Dward.* source mapping,
#    preserving unrelated configuration.
# 4. Deploy a snapshot to CustomScripts/dward-dev-kit and register its templates.
# 5. Add the deployed scripts to Windows user PATH and maintain a nonduplicating
#    PATH block in .bashrc and the applicable Bash login profile.
# 6. Report missing optional Godot, GitHub CLI, and Aseprite tools without failing.

import argparse
import os
from pathlib import Path
import shlex
import shutil
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET
import zipfile


KIT = Path(__file__).resolve().parent
SOURCE = "DwardLocal"
START = "# >>> dev-kit scripts >>>"
END = "# <<< dev-kit scripts <<<"


def run(*arguments, capture=False):
    result = subprocess.run(
        list(map(str, arguments)), cwd=KIT, check=True,
        capture_output=capture, text=True,
    )
    return result.stdout if capture else None


def read_config(path):
    if path.exists():
        parser = ET.XMLParser(target=ET.TreeBuilder(insert_comments=True))
        tree = ET.parse(path, parser=parser)
        if tree.getroot().tag != "configuration":
            raise ValueError(f"Expected a NuGet configuration file: {path}")
        return tree
    root = ET.Element("configuration")
    sources = ET.SubElement(root, "packageSources")
    ET.SubElement(sources, "add", {
        "key": "nuget.org", "value": "https://api.nuget.org/v3/index.json",
    })
    return ET.ElementTree(root)


def write_config(tree, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    tree.write(path, encoding="utf-8", xml_declaration=True)


def configure_feed(config, feed):
    tree = read_config(config)
    if not config.exists():
        write_config(tree, config)
    sources = tree.getroot().find("packageSources")
    existing = None if sources is None else next(
        (item for item in sources.findall("add") if item.get("key") == SOURCE), None,
    )
    if existing is None:
        run("dotnet", "nuget", "add", "source", feed, "--name", SOURCE, "--configfile", config)
    elif existing.get("value") != str(feed):
        run("dotnet", "nuget", "update", "source", SOURCE, "--source", feed, "--configfile", config)
    tree = read_config(config)
    disabled = tree.getroot().find("disabledPackageSources")
    if disabled is not None and any(
        item.get("key") == SOURCE and item.get("value", "").lower() == "true"
        for item in disabled.findall("add")
    ):
        run("dotnet", "nuget", "enable", "source", SOURCE, "--configfile", config)
        tree = read_config(config)
    root = tree.getroot()
    mappings = root.find("packageSourceMapping")
    changed = False
    if mappings is None:
        mappings = ET.SubElement(root, "packageSourceMapping")
        for source in root.findall("./packageSources/add"):
            if source.get("key") != SOURCE:
                mapping = ET.SubElement(mappings, "packageSource", {"key": source.get("key")})
                ET.SubElement(mapping, "package", {"pattern": "*"})
        changed = True
    mapping = next(
        (item for item in mappings.findall("packageSource") if item.get("key") == SOURCE), None,
    )
    if mapping is None:
        mapping = ET.SubElement(mappings, "packageSource", {"key": SOURCE})
        changed = True
    if not any(item.get("pattern") == "Dward.*" for item in mapping.findall("package")):
        ET.SubElement(mapping, "package", {"pattern": "Dward.*"})
        changed = True
    if changed:
        write_config(tree, config)
    print(f"Local package source ready: {SOURCE} -> {feed}", flush=True)


def same_package(first, second):
    def contents(archive):
        return {
            "core-properties" if name.endswith(".psmdcp") else name: archive.read(name)
            for name in archive.namelist() if name != "_rels/.rels"
        }

    with zipfile.ZipFile(first) as left, zipfile.ZipFile(second) as right:
        return contents(left) == contents(right)


def pack_analyzers(config, feed):
    with tempfile.TemporaryDirectory(prefix="dev-kit packages ") as directory:
        stage = Path(directory)
        for project in ("Dward.Analyzers", "Dward.Godot.Analyzers"):
            run(
                "dotnet", "pack", KIT / "analyzers" / project / f"{project}.csproj",
                "--configuration", "Release", "--output", stage,
                f"-p:RestoreConfigFile={config}",
            )
        packages = sorted(stage.glob("*.nupkg"))
        if len(packages) != 2:
            raise ValueError("Expected both analyzer packages; local feed was not changed.")
        for package in packages:
            destination = feed / package.name
            if destination.exists() and not same_package(package, destination):
                raise ValueError(
                    f"{destination.name} already exists with different contents. "
                    "Increment the analyzer package version before installing changed packages."
                )
        feed.mkdir(parents=True, exist_ok=True)
        for package in packages:
            destination = feed / package.name
            if destination.exists():
                print(f"Already installed: {package.name}", flush=True)
            else:
                shutil.copy2(package, destination)
                print(f"Installed: {package.name}", flush=True)


def bash_path(path):
    if os.name == "nt":
        return run("cygpath", "-u", path, capture=True).strip()
    return str(path)


def profile_contents(path, scripts):
    text = path.read_text(encoding="utf-8") if path.exists() else ""
    if text.count(START) != text.count(END) or text.count(START) > 1:
        raise ValueError(f"Malformed dev-kit PATH block in {path}; repair it before rerunning.")
    if START in text:
        start = text.index(START)
        end = text.index(END)
        if end < start:
            raise ValueError(f"Malformed dev-kit PATH block in {path}.")
        text = text[:start] + text[end + len(END):].lstrip("\r\n")
    block = (
        f"{START}\n"
        f"_dev_kit_scripts={shlex.quote(scripts)}\n"
        'case ":$PATH:" in\n'
        '    *":$_dev_kit_scripts:"*) ;;\n'
        '    *) export PATH="$_dev_kit_scripts:$PATH" ;;\n'
        "esac\n"
        "unset _dev_kit_scripts\n"
        f"{END}\n"
    )
    return text.rstrip("\r\n") + ("\n\n" if text.strip() else "") + block


def default_profiles(home):
    login_profile = next(
        (home / name for name in (".bash_profile", ".bash_login", ".profile")
         if (home / name).exists()),
        home / ".bash_profile",
    )
    return [home / ".bashrc", login_profile]


def validate_deployment(destination):
    if destination.is_symlink() or getattr(destination, "is_junction", lambda: False)():
        raise ValueError(f"Deployment must not be a link: {destination}")
    if destination == KIT or destination in KIT.parents or KIT in destination.parents:
        raise ValueError("Deployment must be outside the dev-kit checkout.")
    if destination.exists() and not (destination / ".dev-kit-deployment").is_file():
        raise ValueError(f"Refusing to replace an unmanaged directory: {destination}")


def deploy_files(destination):
    validate_deployment(destination)
    destination.parent.mkdir(parents=True, exist_ok=True)
    ignore = shutil.ignore_patterns(
        "__pycache__", "*.pyc", "*.pyo", "bin", "obj", ".git", ".vs", ".idea",
        "settings.local.json", "*.user",
    )
    with tempfile.TemporaryDirectory(prefix=".dward-dev-kit-stage-", dir=destination.parent) as directory:
        stage = Path(directory) / "snapshot"
        stage.mkdir()
        for source in (KIT / "scripts").iterdir():
            if source.name == "tests" or source.name == "__pycache__":
                continue
            target = stage / source.name
            if source.is_dir():
                shutil.copytree(source, target, ignore=ignore)
            elif not source.suffix:
                shutil.copy2(source, target)
                target.chmod(target.stat().st_mode | 0o111)
        for name in ("config", "skills", "templates"):
            shutil.copytree(
                KIT / name, stage / name,
                ignore=lambda path, names: set(ignore(path, names)) | (
                    {"tests"} if Path(path) == KIT / "templates" else set()
                ),
            )
        shutil.copy2(KIT / "LICENSE", stage / "LICENSE")
        (stage / ".dev-kit-deployment").write_text("dward-dev-kit\n", encoding="utf-8")
        previous = Path(directory) / "previous"
        if destination.exists():
            move_deployment(destination, previous)
        try:
            move_deployment(stage, destination)
        except OSError:
            if previous.exists():
                move_deployment(previous, destination)
            raise
    print(f"Deployed snapshot: {destination}", flush=True)


def move_deployment(source, destination):
    for attempt in range(6):
        try:
            source.rename(destination)
            return
        except PermissionError:
            if attempt == 5:
                raise
            time.sleep(1)


def windows_path_entries(current, destination, previous):
    def normalized(value):
        return os.path.normcase(os.path.normpath(value.rstrip("\\/")))

    remove = {normalized(str(destination)), normalized(str(previous))}
    entries = [value for value in current.split(";") if value and normalized(value) not in remove]
    return ";".join([*entries, str(destination)])


def configure_windows_path(destination):
    import winreg

    with winreg.CreateKey(winreg.HKEY_CURRENT_USER, "Environment") as key:
        try:
            current, value_type = winreg.QueryValueEx(key, "Path")
        except FileNotFoundError:
            current, value_type = "", winreg.REG_EXPAND_SZ
        updated = windows_path_entries(current, destination, KIT / "scripts")
        if updated != current:
            winreg.SetValueEx(key, "Path", 0, value_type, updated)
    import ctypes
    from ctypes import wintypes

    result = ctypes.c_size_t()
    notify = ctypes.windll.user32.SendMessageTimeoutW
    notify.argtypes = [
        wintypes.HWND, wintypes.UINT, ctypes.c_size_t, wintypes.LPCWSTR,
        wintypes.UINT, wintypes.UINT, ctypes.POINTER(ctypes.c_size_t),
    ]
    notify.restype = ctypes.c_ssize_t
    notify(
        0xFFFF, 0x001A, 0, "Environment", 0x0002, 5000, ctypes.byref(result),
    )
    print(f"Windows user PATH ready: {destination}", flush=True)


def install(args):
    # 1. Check prerequisites and validate existing configuration before changes.
    missing = [command for command in ("dotnet", "bash", "git", "tar", "find")
               if not shutil.which(command)]
    if os.name == "nt" and not shutil.which("cygpath"):
        missing.append("cygpath (run from Git Bash)")
    if missing:
        raise ValueError("Missing prerequisites: " + ", ".join(missing) + ". Install them and rerun.")
    sdk = run("dotnet", "--version", capture=True).strip()
    if int(sdk.split(".")[0]) < 10:
        raise ValueError(f".NET SDK 10 or later is required; the active SDK is {sdk}.")
    config = args.nuget_config.expanduser().resolve()
    feed = args.feed.expanduser().resolve()
    destination = args.custom_scripts.expanduser().resolve() / "dward-dev-kit"
    validate_deployment(destination)
    profiles = (
        [path.expanduser().resolve() for path in args.profile]
        if args.profile else default_profiles(Path.home())
    )
    read_config(config)
    scripts = bash_path(destination)
    for path in profiles:
        profile_contents(path, scripts)
    for path in (KIT / "templates", KIT / "analyzers", KIT / "config", KIT / "skills"):
        if not path.is_dir():
            raise ValueError(f"Missing kit directory: {path}")
    if not config.exists():
        write_config(read_config(config), config)
    # 2. Build and install analyzer packages into the local feed.
    pack_analyzers(config, feed)
    # 3. Add or repair the NuGet source and package-source mapping.
    configure_feed(config, feed)
    # 4. Deploy scripts and supporting files, then register the copied templates.
    deploy_files(destination)
    installed = run("dotnet", "new", "uninstall", capture=True)
    previous_templates = KIT / "templates"
    if str(previous_templates) in installed:
        run("dotnet", "new", "uninstall", previous_templates)
    run("dotnet", "new", "install", destination / "templates", "--force")
    # 5. Add the deployment to Windows user PATH and Bash profiles without duplicates.
    if os.name == "nt" and not args.no_user_path:
        configure_windows_path(destination)
    for path in profiles:
        contents = profile_contents(path, scripts)
        if not path.exists() or path.read_text(encoding="utf-8") != contents:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(contents, encoding="utf-8", newline="\n")
        print(f"Bash PATH ready: {path}", flush=True)
    # 6. Report missing optional tools without failing installation.
    for command, purpose in (
        ("godot-mono", "Godot setup/import and engine tests"),
        ("gh", "ghinit GitHub repository creation"),
        ("aseprite", "pixel-art operations"),
    ):
        if not shutil.which(command):
            print(f"Optional prerequisite not found: {command} ({purpose})", flush=True)
    print("Installation complete. Open a new Bash terminal to use the starter commands.", flush=True)


def main():
    parser = argparse.ArgumentParser(description="Install or update this checkout's developer kit.")
    default_config = (
        Path(os.environ.get("APPDATA", Path.home() / "AppData" / "Roaming")) / "NuGet" / "NuGet.Config"
        if os.name == "nt" else Path.home() / ".nuget" / "NuGet" / "NuGet.Config"
    )
    parser.add_argument("--nuget-config", type=Path, default=default_config)
    parser.add_argument("--feed", type=Path, default=Path.home() / ".nuget" / "local-feed" / "Dward")
    parser.add_argument(
        "--custom-scripts", type=Path,
        default=Path.home() / "OneDrive" / "CustomScripts" if os.name == "nt" else Path.home() / "CustomScripts",
        help="Parent directory for the deployed dward-dev-kit snapshot.",
    )
    parser.add_argument("--no-user-path", action="store_true",
                        help="Skip Windows user PATH changes (Bash profiles are still updated).")
    parser.add_argument("--profile", type=Path, action="append",
                        help="Bash profile to update (repeatable); defaults to .bashrc and .bash_profile.")
    try:
        install(parser.parse_args())
    except (ValueError, OSError, ET.ParseError, zipfile.BadZipFile, subprocess.CalledProcessError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
