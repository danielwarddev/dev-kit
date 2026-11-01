"""Shared, bounded runtime for screenshots and playtesting scenarios."""

import json
import math
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import uuid

from operations import executable, project_directory


def validate_scenario(scenario):
    if not isinstance(scenario, dict) or set(scenario) - {"scene", "steps", "startup_frames"}:
        raise ValueError("Scenario requires scene and steps; optional startup_frames.")
    scene = scenario.get("scene", "")
    relative = scene.removeprefix("res://") if isinstance(scene, str) else ""
    if not isinstance(scene, str) or not scene.startswith("res://") or not relative.endswith(".tscn") or any(
        part in ("", ".", "..") for part in relative.split("/")
    ) or "\\" in relative or ":" in relative:
        raise ValueError("scene must be a res:// path to a .tscn inside the project.")
    frames = scenario.get("startup_frames", 2)
    if type(frames) is not int or not 1 <= frames <= 3600:
        raise ValueError("startup_frames must be an integer from 1 to 3600.")
    steps = scenario.get("steps")
    if not isinstance(steps, list) or not 1 <= len(steps) <= 100:
        raise ValueError("steps must contain 1 to 100 operations.")
    fields = {
        "wait": {"op", "frames"},
        "key": {"op", "action", "frames"},
        "action": {"op", "action", "frames"},
        "click": {"op", "node"},
        "assert": {"op", "node", "property", "equals"},
        "wait_until": {"op", "node", "property", "equals", "frames"},
        "sample": {"op", "node", "property"},
        "capture": {"op", "name"},
    }
    names = set()
    for step in steps:
        if not isinstance(step, dict) or step.get("op") not in fields:
            raise ValueError("Unknown scenario operation.")
        op = step["op"]
        if set(step) != fields[op]:
            raise ValueError(f"{op} requires exactly these fields: {sorted(fields[op])}")
        if "frames" in step and (type(step["frames"]) is not int or not 1 <= step["frames"] <= 3600):
            raise ValueError("frames must be an integer from 1 to 3600.")
        for field in ("action", "node", "property"):
            if field in step and (not isinstance(step[field], str) or not step[field]):
                raise ValueError(f"{field} must be a nonempty string.")
        if "node" in step and (step["node"].startswith("/") or ".." in step["node"].split("/")):
            raise ValueError("node must be relative to the running scene, without parent traversal.")
        if op == "capture":
            name = step["name"]
            if not isinstance(name, str) or not re.fullmatch(r"[A-Za-z0-9_-]+", name) or name in names:
                raise ValueError("Capture names must be unique letters, digits, underscores, or hyphens.")
            names.add(name)
    json.dumps(scenario, allow_nan=False)


def isolated_settings(text, user_name):
    """Override save settings in the disposable copy, including feature overrides."""
    lines = text.splitlines()
    output = []
    application = False
    found = False
    for line in lines:
        section = re.match(r"^\s*\[([^\]]+)\]\s*(?:;.*)?$", line)
        if section:
            application = section[1] == "application"
            if application:
                found = True
                output.extend([
                    line, 'config/use_custom_user_dir=true',
                    f'config/custom_user_dir_name="{user_name}"',
                ])
                continue
        if application and re.match(r"\s*config/(use_custom_user_dir|custom_user_dir_name)(?:\.[^=]+)?\s*=", line):
            continue
        output.append(line)
    if not found:
        output.extend([
            "[application]", "config/use_custom_user_dir=true",
            f'config/custom_user_dir_name="{user_name}"',
        ])
    return "\n".join(output) + "\n"


def mark_failed(result_path, message):
    if result_path.is_file():
        result = json.loads(result_path.read_text(encoding="utf-8"))
        result["complete"] = False
        result["error"] = message
        result_path.write_text(json.dumps(result, indent=2), encoding="utf-8")


def run_scenario(project_path, scenario, output_path, *, godot_path=None, timeout=60, headless=False):
    validate_scenario(scenario)
    project = project_directory(project_path)
    output = Path(output_path).resolve()
    if output == project or project in output.parents:
        raise ValueError("Keep recipe output outside the source project.")
    if not math.isfinite(timeout) or timeout <= 0 or timeout > 600:
        raise ValueError("timeout must be greater than zero and at most 600 seconds.")
    if headless and any(step["op"] in ("capture", "click") for step in scenario["steps"]):
        raise ValueError("Screenshots and GUI clicks require a rendered run, not headless mode.")
    if output.exists():
        raise FileExistsError(f"Use a fresh output directory: {output}")
    output.mkdir(parents=True)
    run_id = uuid.uuid4().hex
    user_name = f"godot-recipe-{run_id}"
    result_path = output / "result.json"
    if sys.platform == "win32":
        user_data = Path(os.environ["APPDATA"]) / user_name if os.environ.get("APPDATA") else None
    elif sys.platform == "darwin":
        user_data = Path.home() / "Library" / "Application Support" / user_name
    else:
        user_data = Path(os.environ.get("XDG_DATA_HOME", str(Path.home() / ".local" / "share"))) / user_name
    try:
        with tempfile.TemporaryDirectory(prefix="godot-recipe-project-") as temporary:
            snapshot = Path(temporary) / "project"
            # Preserve imported assets and built C# assemblies; never edit source/editor state.
            shutil.copytree(project, snapshot, ignore=shutil.ignore_patterns(
                ".git", ".idea", "node_modules", "__pycache__",
            ))
            settings = snapshot / "project.godot"
            settings.write_text(isolated_settings(settings.read_text(encoding="utf-8-sig"), user_name), encoding="utf-8")
            request = dict(scenario, run_id=run_id, output=str(output), user_name=user_name)
            request_path = output / "request.json"
            request_path.write_text(json.dumps(request, allow_nan=False), encoding="utf-8")
            arguments = [
                executable(godot_path), "--path", str(snapshot), "--script",
                str(Path(__file__).with_name("runtime.gd")), "--", str(request_path),
            ]
            if headless:
                arguments.insert(1, "--headless")
            try:
                process = subprocess.run(arguments, cwd=snapshot, capture_output=True, text=True, encoding="utf-8", timeout=timeout)
            except subprocess.TimeoutExpired as error:
                for name, stream in (("stdout.log", error.stdout), ("stderr.log", error.stderr)):
                    text = stream.decode("utf-8", errors="replace") if isinstance(stream, bytes) else stream or ""
                    (output / name).write_text(text, encoding="utf-8")
                mark_failed(result_path, f"Process timed out after {timeout}s.")
                raise RuntimeError(f"Recipe timed out after {timeout}s; partial evidence is in {output}.") from error
            (output / "stdout.log").write_text(process.stdout, encoding="utf-8")
            (output / "stderr.log").write_text(process.stderr, encoding="utf-8")
            result = json.loads(result_path.read_text(encoding="utf-8")) if result_path.is_file() else None
            errors = any(line.lstrip().startswith(("ERROR:", "SCRIPT ERROR:", "[ERROR]")) for line in process.stdout.splitlines())
            if process.returncode or process.stderr.strip() or errors:
                if result and result.get("complete") is True:
                    mark_failed(result_path, "Engine exit/output checks failed; inspect stdout.log and stderr.log.")
                raise RuntimeError(f"Godot recipe failed; evidence in {output}:\n{process.stdout}\n{process.stderr}")
            if not result or result.get("run_id") != run_id or result.get("complete") is not True or "GODOT_RECIPE_OK" not in process.stdout:
                mark_failed(result_path, "Engine did not confirm recipe completion.")
                raise RuntimeError(f"Godot did not confirm recipe completion; inspect {output}.")
            for step in scenario["steps"]:
                if step["op"] == "capture":
                    image = output / f'{step["name"]}.png'
                    if not image.is_file() or image.stat().st_size == 0:
                        mark_failed(result_path, f"Capture missing or empty: {image}")
                        raise RuntimeError(f"Capture missing or empty: {image}")
            return result
    finally:
        if result_path.is_file():
            result = json.loads(result_path.read_text(encoding="utf-8"))
            if result.get("run_id") == run_id and result.get("user_data"):
                user_data = Path(result["user_data"]).resolve()
        # Only remove the unique save directory confirmed by this run's engine.
        if user_data and user_data.name == user_name and user_data.is_dir() and not user_data.is_symlink():
            shutil.rmtree(user_data)


def print_result(result):
    print(json.dumps(result, indent=2))


def fail_cli(error):
    print(f"Recipe failed: {error}", file=sys.stderr)
    raise SystemExit(1)
