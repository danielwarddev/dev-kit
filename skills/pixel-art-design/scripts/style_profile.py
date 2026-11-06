import copy
import json
from pathlib import Path
import re


SKILL_ROOT = Path(__file__).resolve().parents[1]
PROFILE_ROOT = SKILL_ROOT.parent.parent / "art-profiles"
PROFILE_PATH = PROFILE_ROOT / "project-style.json"
TEMPLATE_PATH = SKILL_ROOT / "references" / "style-profile-template.json"


def outside_skill(path: Path) -> Path:
    resolved = Path(path).resolve()
    for root in (SKILL_ROOT, SKILL_ROOT.parent / "aseprite"):
        if resolved.is_relative_to(root.resolve()):
            raise ValueError("Runtime profiles and generated outputs must stay outside skill folders.")
    return resolved


def _merge(target: dict, hints: dict) -> None:
    for key, value in hints.items():
        if isinstance(value, dict) and isinstance(target.get(key), dict):
            _merge(target[key], value)
        else:
            target[key] = copy.deepcopy(value)


DIRECTION_KEYS = ("pixel_scale", "camera", "lighting", "outlines", "palettes")


def _validate(profile: dict) -> None:
    if not isinstance(profile, dict) or profile.get("version") != 1:
        raise ValueError("Style profile must use version 1; the existing file was not changed.")
    if profile.get("status") not in ("draft", "approved"):
        raise ValueError("Style profile status must be draft or approved.")
    missing = [key for key in DIRECTION_KEYS
               if not isinstance(profile.get(key), dict) or not profile[key]]
    if missing:
        raise ValueError(
            f"Style profile is missing {', '.join(missing)}. There are no defaults: infer them "
            "from the request, existing assets, or references and tell the user, or ask."
        )
    for group, colors in profile["palettes"].items():
        if not isinstance(colors, dict) or not colors:
            raise ValueError(f"The {group} palette must name at least one color.")
        if any(not isinstance(value, str) or not re.fullmatch(r"#[0-9a-fA-F]{6}", value)
               for value in colors.values()):
            raise ValueError(f"The {group} palette must contain #RRGGBB colors.")


def read_profile(path: Path = PROFILE_PATH) -> dict:
    profile = json.loads(Path(path).read_text(encoding="utf-8"))
    _validate(profile)
    return profile


def load_or_create(
    path: Path = PROFILE_PATH, hints: dict | None = None,
    template_path: Path = TEMPLATE_PATH,
) -> dict:
    path = outside_skill(path)
    if path.exists():
        return read_profile(path)
    profile = json.loads(Path(template_path).read_text(encoding="utf-8"))
    if hints:
        _merge(profile, hints)
    profile["status"] = "draft"
    _validate(profile)
    encoded = json.dumps(profile, indent=2) + "\n"
    path.parent.mkdir(parents=True, exist_ok=True)
    try:
        with path.open("x", encoding="utf-8", newline="\n") as destination:
            destination.write(encoded)
    except FileExistsError:
        return read_profile(path)
    return profile
