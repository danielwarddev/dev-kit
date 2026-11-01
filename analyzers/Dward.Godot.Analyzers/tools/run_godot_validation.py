"""Run Godot scene and resource validation. Python standard library only."""

# This script:
# - Collects scenes and literal resource paths/UIDs from project files
# - Launches Godot to run godot_validation.gd using that list
# - Fails on that Godot script returning a non-success code, timing out, or not printing the success message
# - Reports how long this took in the console at the end

import argparse
import json
import re
import shutil
import subprocess
import tempfile
import time
from pathlib import Path

IGNORED_DIRECTORIES = {".git", ".godot", "bin", "obj", ".idea", "__pycache__"}
TOKEN = re.compile(
    r'//[^\n]*|/\*[\s\S]*?\*/|\#[^\n]*|@"(?:[^"]|"")*"|'
    r'"""[\s\S]*?"""|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|'
    r'[A-Za-z_]\w*|[^\s]'
)


# Source scanning: exclude comments and decode literal resource paths, not computed paths.
def tokens(text, suffix):
    result = []
    for match in TOKEN.finditer(text):
        value = match.group()
        if value.startswith(("//", "/*")) or (suffix == ".gd" and value.startswith("#")):
            continue
        result.append((value, text.count("\n", 0, match.start()) + 1))
    return result


def literal(value):
    if value.startswith('@"'):
        return value[2:-1].replace('""', '"')
    if value.startswith('"""'):
        return None
    if value.startswith(('"', "'")):
        escapes = {
            "0": "\0", "a": "\a", "b": "\b", "f": "\f", "n": "\n",
            "r": "\r", "t": "\t", "v": "\v", "\\": "\\", "'": "'", '"': '"',
        }

        def decode(match):
            escaped = match[1]
            if escaped.startswith(("u", "U", "x")):
                return chr(int(escaped[1:], 16))
            if escaped not in escapes:
                raise ValueError(f"Unsupported string escape: \\{escaped}")
            return escapes[escaped]

        return re.sub(r"\\(u[0-9a-fA-F]{4}|U[0-9a-fA-F]{8}|x[0-9a-fA-F]{1,4}|.)", decode, value[1:-1])
    return None


# Contract inventory: collect scenes, literal res:// and uid:// references, and
# ext_resource UID/path pairs so Godot can detect missing resources and stale UIDs.
def make_request(project):
    request = {"scenes": [], "resources": []}
    for path in sorted(project.rglob("*")):
        if not path.is_file() or any(part.lower() in IGNORED_DIRECTORIES for part in path.relative_to(project).parts):
            continue
        if path.suffix not in {".cs", ".gd", ".tscn", ".tres", ".godot"}:
            continue
        text = path.read_text(encoding="utf-8-sig")
        resource_path = "res://" + path.relative_to(project).as_posix()
        if path.suffix == ".tscn":
            request["scenes"].append(resource_path)
        if path.suffix in {".cs", ".gd"}:
            source_tokens = tokens(text, path.suffix)
            strings = [
                (literal(value), line) for index, (value, line) in enumerate(source_tokens)
                if index == 0 or source_tokens[index - 1][0] != "$"
            ]
        else:
            strings = [
                (json.loads(match.group()), text.count("\n", 0, match.start()) + 1)
                for match in re.finditer(r'"(?:\\.|[^"\\])*"', text)
                if not text[text.rfind("\n", 0, match.start()) + 1:match.start()].lstrip().startswith(";")
            ]
        for value, line in strings:
            if value and value.startswith(("res://", "uid://")):
                request["resources"].append({"path": value, "source": resource_path, "line": line})
        for match in re.finditer(r"^\[ext_resource\s+([^\n]+)\]$", text, re.MULTILINE):
            attributes = dict(re.findall(r'(\w+)=("(?:\\.|[^"\\])*")', match[1]))
            if "uid" in attributes and "path" in attributes:
                request["resources"].append({
                    "path": json.loads(attributes["path"]), "uid": json.loads(attributes["uid"]),
                    "source": resource_path, "line": text.count("\n", 0, match.start()) + 1,
                })
    return request


# Engine validation: send the inventory to the companion GDScript in a bounded run.
# Fail on a nonzero exit, stderr (including warnings), or missing success confirmation.
def run_engine(project, godot, request, timeout=120):
    started = time.perf_counter()
    executable = shutil.which(str(godot))
    if executable is None:
        raise RuntimeError(
            f"Godot executable not found: {godot}. Set GodotExecutable or GODOT_PATH "
            f"to a .NET-enabled Godot console executable. ({time.perf_counter() - started:.2f}s)"
        )
    with tempfile.TemporaryDirectory(prefix="godot-validation-") as directory:
        request_path = Path(directory) / "request.json"
        request_path.write_text(json.dumps(request), encoding="utf-8")
        try:
            result = subprocess.run(
                [executable, "--headless", "--path", str(project),
                 "--script", str(Path(__file__).parent / "godot_validation.gd"), "--", str(request_path)],
                capture_output=True, text=True, timeout=timeout,
            )
        except subprocess.TimeoutExpired as error:
            raise RuntimeError(
                f"Godot validation timed out ({time.perf_counter() - started:.2f}s). "
                "Check scene/script constructors and _init for blocking work or loops; "
                "the validator instantiates scenes without running _Ready/_Process."
            ) from error
    duration = time.perf_counter() - started
    output = result.stdout + result.stderr
    if result.returncode or result.stderr.strip() or "GODOT_VALIDATION_OK" not in result.stdout:
        hint = (
            "Check the Godot messages above for broken scripts, scene syntax, missing dependencies, "
            "or missing imports. Build C# and import assets before validating."
        )
        if "GODOT_VALIDATION_OK" not in result.stdout:
            hint += (
                " No GODOT_VALIDATION_OK completion marker was received; a zero exit code alone "
                "does not prove validation finished. Check that godot_validation.gd ran to completion."
            )
        raise RuntimeError(f"Godot validation failed ({duration:.2f}s):\n{output.strip()}\n{hint}")
    return f"{result.stdout.strip()} ({duration:.2f}s)"


# Command-line entry: run Godot validation for the supplied project and executable.
# Build/import first; this script repairs nothing and does not inspect Git.
def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True, help="Actual directory containing project.godot")
    parser.add_argument("--godot", type=Path, required=True, help=".NET-enabled Godot console executable")
    args = parser.parse_args()
    project = args.project.resolve()
    if not (project / "project.godot").is_file():
        parser.error("--project must contain project.godot")
    request = make_request(project)
    print(run_engine(project, args.godot, request))


if __name__ == "__main__":
    main()
