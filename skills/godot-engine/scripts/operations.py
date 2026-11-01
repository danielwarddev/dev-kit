"""Direct Python helpers for the bundled Godot script; no MCP or custom CLI."""

import json
import os
from pathlib import Path
import subprocess


_REQUIRED = {
    "create_scene": ("scene_path",),
    "add_node": ("scene_path", "node_type", "node_name"),
    "load_sprite": ("scene_path", "node_path", "texture_path"),
    "export_mesh_library": ("scene_path", "output_path"),
    "save_scene": ("scene_path",),
    "get_uid": ("file_path",),
    "resave_resources": (),
}
_SUCCESS = {
    "create_scene": "Scene created",
    "add_node": "added successfully",
    "load_sprite": "Sprite loaded successfully",
    "export_mesh_library": "MeshLibrary exported",
    "save_scene": "Scene saved successfully",
    "resave_resources": "Resave operation complete",
}


def executable(godot_path=None):
    return godot_path or os.environ.get("GODOT_PATH", "godot")


def project_directory(project_path):
    project = Path(project_path).resolve(strict=True)
    if not (project / "project.godot").is_file():
        raise ValueError(f"Not a Godot project: {project}")
    return project


def run_engine(arguments, *, godot_path=None, timeout=120):
    """Run a bounded engine command and surface exit, script, and resource errors."""
    result = subprocess.run(
        [executable(godot_path), *map(str, arguments)],
        capture_output=True, text=True, encoding="utf-8", timeout=timeout,
    )
    output = result.stdout + "\n" + result.stderr
    if result.returncode or result.stderr.strip() or any(
        line.lstrip().startswith(("ERROR:", "SCRIPT ERROR:", "[ERROR]"))
        for line in result.stdout.splitlines()
    ):
        raise RuntimeError(f"Godot command failed (exit {result.returncode}):\n{output.strip()}")
    return result.stdout


def operate(project_path, operation, parameters, *, godot_path=None, timeout=120):
    """Invoke an upstream operation with snake_case parameters."""
    project = project_directory(project_path)
    if operation not in _REQUIRED:
        raise ValueError(f"Unknown operation: {operation}")
    if not isinstance(parameters, dict):
        raise TypeError("Parameters must be a dictionary")
    for key in _REQUIRED[operation]:
        if key not in parameters or not isinstance(parameters[key], str):
            raise ValueError(f"Required string parameter: {key}")
        if not parameters[key] and key != "node_path":
            raise ValueError(f"Parameter cannot be empty: {key}")
    for key in ("scene_path", "texture_path", "output_path", "new_path", "file_path", "project_path"):
        if key in parameters:
            value = parameters[key]
            if not isinstance(value, str):
                raise ValueError(f"Resource path must be a string: {key}")
            relative = value.removeprefix("res://").replace("\\", "/")
            if "\\" in value or ".." in relative.split("/") or ":" in relative or relative.startswith("/"):
                raise ValueError(f"Use a project-relative or res:// resource path: {key}")
    output = run_engine(
        ["--headless", "--path", project, "--script",
         Path(__file__).with_name("godot_operations.gd"), operation,
         json.dumps(parameters, ensure_ascii=False, allow_nan=False)],
        godot_path=godot_path, timeout=timeout,
    )
    if operation == "get_uid":
        for line in output.splitlines():
            if line.startswith("{"):
                data = json.loads(line)
                if "exists" not in data:
                    raise RuntimeError(f"UID result missing exists field:\n{output}")
                return data
        raise RuntimeError(f"Godot did not return UID data:\n{output}")
    if _SUCCESS[operation] not in output:
        raise RuntimeError(f"Godot did not confirm {operation}:\n{output}")
    return output


def list_projects(directory, recursive=False):
    """Find project roots without traversing symlink or generated directories."""
    root = Path(directory).resolve(strict=True)
    found = []
    for current, directories, files in os.walk(root, followlinks=False):
        directories[:] = [
            name for name in directories
            if not name.startswith(".") and name not in ("bin", "obj", "node_modules")
            and not (Path(current) / name).is_symlink()
        ] if recursive else []
        if "project.godot" in files:
            found.append(str(Path(current)))
        if not recursive:
            found.extend(
                str(child) for child in sorted(root.iterdir())
                if child.is_dir() and not child.is_symlink()
                and not child.name.startswith(".") and (child / "project.godot").is_file()
            )
    return sorted(set(found))


def project_info(project_path, *, godot_path=None):
    """Return project identity and actual file counts, not name-based guesses."""
    project = project_directory(project_path)
    counts = {"scenes": 0, "scripts": 0, "assets": 0, "other": 0}
    for current, directories, files in os.walk(project, followlinks=False):
        directories[:] = [
            name for name in directories
            if not name.startswith(".") and name not in ("bin", "obj", "node_modules")
            and not (Path(current) / name).is_symlink()
        ]
        for name in files:
            extension = Path(name).suffix.lower()
            category = (
                "scenes" if extension == ".tscn" else
                "scripts" if extension in (".gd", ".cs", ".gdshader", ".shader") else
                "assets" if extension in (".png", ".jpg", ".webp", ".svg", ".wav", ".ogg", ".mp3", ".glb", ".gltf", ".tres", ".res") else
                "other"
            )
            counts[category] += 1
    return {
        "path": str(project),
        "godot_version": run_engine(["--version"], godot_path=godot_path).strip(),
        "counts": counts,
        "project_settings_file": str(project / "project.godot"),
    }
