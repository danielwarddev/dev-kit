"""Capture a fresh, rendered scene using the shared bounded runtime."""

import argparse
from pathlib import Path

from runtime import fail_cli, print_result, run_scenario


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True, type=Path)
    parser.add_argument("--scene", required=True, help="res:// path to a .tscn")
    parser.add_argument("--output", required=True, type=Path, help="Fresh artifact directory outside the project")
    parser.add_argument("--godot", help="Godot executable; defaults to GODOT_PATH or godot")
    parser.add_argument("--frames", type=int, default=2, help="Startup physics boundaries, not a readiness guarantee")
    parser.add_argument("--timeout", type=float, default=60)
    args = parser.parse_args()
    try:
        result = run_scenario(
            args.project, {"scene": args.scene, "startup_frames": args.frames,
                           "steps": [{"op": "capture", "name": "scene"}]},
            args.output, godot_path=args.godot, timeout=args.timeout,
        )
    except (ValueError, OSError, RuntimeError) as error:
        fail_cli(error)
    print_result(result)


if __name__ == "__main__":
    main()
