import sys

sys.dont_write_bytecode = True

import argparse
import asyncio
import json
from pathlib import Path

from style_profile import PROFILE_PATH, TEMPLATE_PATH, load_or_create, outside_skill


def prepare_output(path: Path, overwrite: bool) -> Path:
    path = outside_skill(path)
    if path.exists() and not path.is_dir():
        raise ValueError(f"{path} is not a directory.")
    if path.exists() and any(path.iterdir()) and not overwrite:
        raise FileExistsError(f"{path} is not empty. Choose a new output or explicitly use --overwrite.")
    path.mkdir(parents=True, exist_ok=True)
    return path


def parser():
    root = argparse.ArgumentParser(description="Pixel-art profile management and generic image checks.")
    root.add_argument("--profile", type=Path)
    root.add_argument("--template", type=Path)
    commands = root.add_subparsers(dest="command", required=True)
    profile = commands.add_parser("profile", help="Read the external project profile or bootstrap a draft.")
    profile.add_argument("--hints", type=Path)
    profile.add_argument("--project")
    validate = commands.add_parser("validate", help="Check alpha and optional integer scaling.")
    validate.add_argument("image", type=Path)
    validate.add_argument("--opaque", action="store_true")
    validate.add_argument("--scale-source", type=Path)
    validate.add_argument("--scale", type=int)
    return root


async def main(args):
    if args.command == "profile":
        hints = json.loads(args.hints.read_text()) if args.hints else {}
        if not isinstance(hints, dict):
            raise ValueError("Profile hints must be a JSON object.")
        if args.project:
            hints["project"] = args.project
        print(json.dumps(load_or_create(
            args.profile or PROFILE_PATH, hints, args.template or TEMPLATE_PATH,
        ), indent=2))
        return
    from runtime import alpha_report, assert_nearest_neighbor, assert_opaque_render
    if bool(args.scale_source) != bool(args.scale):
        raise ValueError("--scale-source and --scale must be supplied together.")
    report = assert_opaque_render(args.image) if args.opaque else alpha_report(args.image)
    if args.scale_source:
        assert_nearest_neighbor(args.scale_source, args.image, args.scale)
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    arguments = parser().parse_args()
    try:
        asyncio.run(main(arguments))
    except (ValueError, FileNotFoundError, FileExistsError, RuntimeError) as error:
        raise SystemExit(str(error))
