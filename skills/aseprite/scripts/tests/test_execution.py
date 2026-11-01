import asyncio
import json
import os
from pathlib import Path
import sys
import struct
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from aseprite_ops.core.commands import AsepriteCommand
from aseprite_ops.tools import animation, canvas, drawing, export, pixel_read


@unittest.skipUnless(os.environ.get("ASEPRITE_PATH"), "Set ASEPRITE_PATH for integration tests")
class ExecutionTests(unittest.TestCase):
    def test_lua_runtime_and_syntax_errors_are_failures(self):
        for script in ('error("intentional failure")', "local = broken"):
            with self.subTest(script=script):
                success, output = AsepriteCommand.execute_lua_script(script)
                self.assertFalse(success, output)
                self.assertTrue(output.strip())

    def test_unicode_and_lua_delimiters_round_trip(self):
        success, output = AsepriteCommand.execute_lua_script(
            'print("caf\\195\\169 ]=] ]==]")'
        )
        self.assertTrue(success, output)
        self.assertEqual(output.strip(), "caf\u00e9 ]=] ]==]")

    def test_documented_workflow_without_mcp(self):
        with tempfile.TemporaryDirectory(prefix="aseprite-skill-") as directory:
            sprite = str(Path(directory) / "example.aseprite")
            preview = str(Path(directory) / "preview.png")

            async def workflow():
                self.assertTrue((await canvas.create_canvas(32, 32, sprite)).startswith("Canvas created successfully"))
                self.assertTrue((await canvas.add_layer(sprite, "body")).startswith("Layer 'body' added"))
                self.assertTrue((await drawing.draw_rectangle_at(
                    sprite, "body", 1, 8, 8, 16, 16, "#D04648", True
                )).startswith("Rectangle drawn"))
                pixels = json.loads(await pixel_read.get_pixels_rect(sprite, 8, 8, 1, 1, "body", 1))
                self.assertEqual(pixels[0]["hex"].lower(), "#d04648")
                self.assertEqual(pixels[0]["a"], 255)
                info = json.loads(await animation.get_sprite_info(sprite))
                self.assertEqual((info["width"], info["height"]), (32, 32))
                result = await export.export_frame(sprite, 1, preview, scale=8)
                self.assertTrue(result.startswith("Frame 1 exported"), result)

            asyncio.run(workflow())
            self.assertTrue(Path(preview).is_file())
            with open(preview, "rb") as image:
                header = image.read(24)
            self.assertEqual(header[:8], b"\x89PNG\r\n\x1a\n")
            self.assertEqual(struct.unpack(">II", header[16:24]), (256, 256))
            self.assertNotIn("mcp", sys.modules)


if __name__ == "__main__":
    unittest.main()
