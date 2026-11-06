import asyncio
import hashlib
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from prototype_art import prepare_output
from runtime import AsepriteCommand, export_frame, record_run, save_master, write_json
from style_profile import SKILL_ROOT


class ToolTests(unittest.TestCase):
    def test_nonempty_output_is_protected_and_overwrite_does_not_delete_unrelated_files(self):
        with tempfile.TemporaryDirectory(prefix="art-output-") as directory:
            path = Path(directory)
            marker = path / "keep.txt"
            marker.write_text("user data")
            with self.assertRaises(FileExistsError):
                prepare_output(path, False)
            prepare_output(path, True)
            self.assertEqual(marker.read_text(), "user data")

    def test_generated_art_cannot_be_written_inside_either_skill(self):
        for root in (SKILL_ROOT, SKILL_ROOT.parent / "aseprite"):
            target = root / "generated-output-test"
            with self.assertRaises(ValueError):
                prepare_output(target, True)
            self.assertFalse(target.exists())

    def test_save_and_export_helpers_reject_skill_destinations_before_engine_execution(self):
        with patch.object(AsepriteCommand, "execute_lua_script_checked") as execute:
            for root in (SKILL_ROOT, SKILL_ROOT.parent / "aseprite"):
                with self.assertRaises(ValueError):
                    asyncio.run(save_master(root / "test.aseprite", [], 1, 1, [], []))
                with self.assertRaises(ValueError):
                    asyncio.run(export_frame(Path("source.aseprite"), root / "test.png"))
                with self.assertRaises(ValueError):
                    write_json(root / "test.json", {})
            execute.assert_not_called()

    def test_run_metadata_supports_task_code_outside_the_skill(self):
        with tempfile.TemporaryDirectory(prefix="art-run-") as directory:
            folder = Path(directory)
            code = folder / "new-subject.py"
            code.write_text("original task code\n")
            dependency = folder / "geometry.py"
            dependency.write_text("task-specific geometry\n")
            source = folder / "input.dat"
            source.write_bytes(b"input data")
            with patch.object(AsepriteCommand, "run_command", return_value=(True, "Aseprite test")):
                record_run(folder, "new subject", {"status": "draft"}, code, {"seed": 42},
                           inputs=(source,), dependencies=(dependency,))
            metadata = json.loads((folder / "run.json").read_text())
            self.assertEqual(metadata["operation"], "new subject")
            self.assertEqual(metadata["parameters"], {"seed": 42})
            for path in (code, dependency):
                self.assertEqual(metadata["code_sha256"][str(path.resolve())],
                                 hashlib.sha256(path.read_bytes()).hexdigest())
            rendering = SKILL_ROOT / "scripts" / "rendering.py"
            self.assertEqual(metadata["code_sha256"][str(rendering.relative_to(SKILL_ROOT))],
                             hashlib.sha256(rendering.read_bytes()).hexdigest())
            self.assertEqual(metadata["inputs"][0]["sha256"], hashlib.sha256(b"input data").hexdigest())


if __name__ == "__main__":
    unittest.main()
