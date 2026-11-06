import asyncio
import os
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from rendering import (
    COMPOSITE_SPRITE_LUA,
    alpha_report,
    assert_nearest_neighbor,
    assert_opaque_render,
)
from aseprite_ops.core.commands import AsepriteCommand, lua_escape
from aseprite_ops.tools import export


@unittest.skipUnless(os.environ.get("ASEPRITE_PATH"), "Set ASEPRITE_PATH for integration tests")
class RenderingTests(unittest.TestCase):
    def create_images(self, directory):
        source = Path(directory) / "sprite.png"
        scene = Path(directory) / "scene.png"
        script = COMPOSITE_SPRITE_LUA + f"""
local source = Sprite(4,4)
local image = Image(4,4)
image:drawPixel(1,1,app.pixelColor.rgba(210,50,40,255))
image:drawPixel(2,1,app.pixelColor.rgba(20,30,40,128))
source:newCel(source.layers[1],1,image,Point(0,0))
image:saveAs("{lua_escape(str(source))}")
local destination = Image(6,6)
for y=0,5 do for x=0,5 do
  destination:drawPixel(x,y,app.pixelColor.rgba(21+x*11,10+y*17,50,255))
end end
local before = Image(destination)
composite_sprite(destination,source,1,Point(1,1))
for y=0,3 do for x=0,3 do
  if app.pixelColor.rgbaA(image:getPixel(x,y))==0 then
    assert(destination:getPixel(x+1,y+1)==before:getPixel(x+1,y+1),'Background erased')
  end
end end
assert(destination:getPixel(2,2)==image:getPixel(1,1),'Opaque pixel changed')
local shadow = destination:getPixel(3,2)
assert(app.pixelColor.rgbaA(shadow)==255,'Partial alpha erased background')
for _,channel in ipairs({{app.pixelColor.rgbaR,app.pixelColor.rgbaG,app.pixelColor.rgbaB}}) do
  local expected = (channel(image:getPixel(2,1))*128+channel(before:getPixel(3,2))*127)/255
  assert(math.abs(channel(shadow)-expected)<=1,'Shadow blend incorrect')
end
destination:saveAs("{lua_escape(str(scene))}")
print('COMPOSITING_PASSED')
"""
        success, output = AsepriteCommand.execute_lua_script_checked(script)
        self.assertTrue(success, output)
        self.assertIn("COMPOSITING_PASSED", output)
        return source, scene

    def test_transparent_canvas_and_partial_shadows_preserve_terrain(self):
        with tempfile.TemporaryDirectory(prefix="aseprite-compositing-") as directory:
            source, scene = self.create_images(directory)
            self.assertEqual(
                alpha_report(source)["frames"],
                [{"frame": 1, "transparent": 14, "partial": 1, "opaque": 1}],
            )
            self.assertEqual(assert_opaque_render(scene)["frames"][0]["opaque"], 36)
            with self.assertRaises(ValueError):
                assert_opaque_render(source)

    def test_scale_validation_catches_changed_pixels(self):
        with tempfile.TemporaryDirectory(prefix="aseprite-scale-") as directory:
            source, _ = self.create_images(directory)
            enlarged = Path(directory) / "enlarged.png"
            result = asyncio.run(export.export_frame(str(source), 1, str(enlarged), scale=2))
            self.assertTrue(result.startswith("Frame 1 exported"), result)
            assert_nearest_neighbor(source, enlarged, 2)
            success, output = AsepriteCommand.execute_lua_script_checked(f"""
local image = Image(app.activeSprite.width,app.activeSprite.height)
image:drawSprite(app.activeSprite,1)
image:drawPixel(2,2,app.pixelColor.rgba(1,2,3,255))
image:saveAs("{lua_escape(str(enlarged))}")
print('CHANGED')
""", str(enlarged))
            self.assertTrue(success, output)
            with self.assertRaises(ValueError):
                assert_nearest_neighbor(source, enlarged, 2)


if __name__ == "__main__":
    unittest.main()
