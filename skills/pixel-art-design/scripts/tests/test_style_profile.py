import json
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from style_profile import (
    PROFILE_PATH, SKILL_ROOT, TEMPLATE_PATH, load_or_create,
)


DIRECTION = {
    "pixel_scale": {"tile_size": 16, "character_reference": 24},
    "camera": {"actors": "side_view"},
    "lighting": {"direction": "upper_right"},
    "outlines": {"actors": "Selective dark outlines."},
    "palettes": {"character": {"blue": "#102030"}},
}


class StyleProfileTests(unittest.TestCase):
    def test_missing_profile_is_bootstrapped_as_a_draft(self):
        with tempfile.TemporaryDirectory(prefix="art-profile-") as directory:
            path = Path(directory) / "project-style.json"
            profile = load_or_create(path, {**DIRECTION, "project": "Prototype", "status": "approved"})
            self.assertEqual(profile["status"], "draft")
            self.assertEqual(profile["project"], "Prototype")
            for key, value in DIRECTION.items():
                self.assertEqual(profile[key], value)
            self.assertEqual(json.loads(path.read_text()), profile)

    def test_bootstrap_without_inferred_direction_fails_without_writing(self):
        with tempfile.TemporaryDirectory(prefix="art-profile-") as directory:
            path = Path(directory) / "project-style.json"
            partial = {key: value for key, value in DIRECTION.items() if key != "lighting"}
            with self.assertRaisesRegex(ValueError, "missing lighting. There are no defaults"):
                load_or_create(path, partial)
            self.assertFalse(path.exists())

    def test_existing_choices_and_custom_fields_are_not_overwritten(self):
        with tempfile.TemporaryDirectory(prefix="art-profile-") as directory:
            path = Path(directory) / "project-style.json"
            original = load_or_create(path, {
                **DIRECTION,
                "project": "Original",
                "custom_notes": ["keep this"],
            })
            before = path.read_bytes()
            self.assertEqual(load_or_create(path, {"project": "Different"}), original)
            self.assertEqual(path.read_bytes(), before)

    def test_invalid_existing_profile_is_not_silently_replaced(self):
        with tempfile.TemporaryDirectory(prefix="art-profile-") as directory:
            path = Path(directory) / "project-style.json"
            path.write_text('{"version": 99}', encoding="utf-8")
            before = path.read_bytes()
            with self.assertRaises(ValueError):
                load_or_create(path)
            self.assertEqual(path.read_bytes(), before)

    def test_profile_state_is_external_and_cannot_be_created_inside_skill(self):
        self.assertFalse(PROFILE_PATH.is_relative_to(SKILL_ROOT))
        target = SKILL_ROOT / "generated-profile-test.json"
        with self.assertRaises(ValueError):
            load_or_create(target)
        self.assertFalse(target.exists())

    def test_new_genre_can_bootstrap_from_its_own_template(self):
        with tempfile.TemporaryDirectory(prefix="genre-profile-") as directory:
            template = json.loads(TEMPLATE_PATH.read_text(encoding="utf-8"))
            template["status"] = "approved"
            template["camera"] = {"actors": "top_down"}
            template_path = Path(directory) / "genre-template.json"
            template_path.write_text(json.dumps(template), encoding="utf-8")
            path = Path(directory) / "genre-style.json"
            hints = {key: value for key, value in DIRECTION.items() if key != "camera"}
            profile = load_or_create(path, hints, template_path=template_path)
            self.assertEqual(profile["camera"]["actors"], "top_down")
            self.assertEqual(profile["status"], "draft")
            self.assertIsNone(profile["project"])


if __name__ == "__main__":
    unittest.main()
