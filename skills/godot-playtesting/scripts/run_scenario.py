"""Run a JSON input/observation scenario through godot-engine's shared runtime."""

import argparse
import json
from pathlib import Path
import sys

engine_scripts = Path(__file__).resolve().parents[2] / "godot-engine" / "scripts"
if not (engine_scripts / "runtime.py").is_file():
    raise SystemExit("Missing sibling godot-engine runtime; install both skills together.")
sys.path.insert(0, str(engine_scripts))
from runtime import fail_cli, print_result, run_scenario


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True, type=Path)
    parser.add_argument("--scenario", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--godot")
    parser.add_argument("--timeout", type=float, default=60)
    parser.add_argument("--headless", action="store_true", help="State-only evidence; cannot capture images")
    args = parser.parse_args()
    try:
        scenario = json.loads(args.scenario.read_text(encoding="utf-8-sig"))
        result = run_scenario(args.project, scenario, args.output, godot_path=args.godot,
                              timeout=args.timeout, headless=args.headless)
    except (ValueError, OSError, RuntimeError) as error:
        fail_cli(error)
    print_result(result)


if __name__ == "__main__":
    main()
