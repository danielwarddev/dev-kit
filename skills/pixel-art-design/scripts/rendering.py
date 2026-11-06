import json
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "aseprite" / "scripts"))

from aseprite_ops.core.commands import AsepriteCommand, lua_escape


COMPOSITE_SPRITE_LUA = """
local function composite_sprite(destination, source, frameNumber, position)
  local frame = Image(source.width, source.height)
  frame:drawSprite(source, frameNumber)
  destination:drawImage(frame, position or Point(0,0), 255, BlendMode.NORMAL)
  return frame
end
"""


def _run(script: str, source: Path) -> str:
    success, output = AsepriteCommand.execute_lua_script_checked(script, str(source))
    if not success:
        raise RuntimeError(output)
    return output


def alpha_report(filename: str | Path) -> dict:
    source = Path(filename).resolve(strict=True)
    output = _run("""
local sprite = app.activeSprite
local frames = {}
for index=1,#sprite.frames do
  local image = Image(sprite.width,sprite.height)
  image:drawSprite(sprite,index)
  local transparent,partial,opaque = 0,0,0
  for y=0,image.height-1 do for x=0,image.width-1 do
    local alpha = app.pixelColor.rgbaA(image:getPixel(x,y))
    if alpha==0 then transparent=transparent+1
    elseif alpha==255 then opaque=opaque+1
    else partial=partial+1 end
  end end
  frames[#frames+1] = string.format(
    '{"frame":%d,"transparent":%d,"partial":%d,"opaque":%d}',
    index,transparent,partial,opaque)
end
print('ALPHA_REPORT:'..string.format(
  '{"width":%d,"height":%d,"frames":[%s]}',
  sprite.width,sprite.height,table.concat(frames,',')))
""", source)
    for line in output.splitlines():
        if line.startswith("ALPHA_REPORT:"):
            return json.loads(line.removeprefix("ALPHA_REPORT:"))
    raise RuntimeError(f"Missing alpha report: {output}")


def assert_opaque_render(filename: str | Path) -> dict:
    report = alpha_report(filename)
    for frame in report["frames"]:
        if frame["transparent"] or frame["partial"]:
            raise ValueError(
                f"{filename}, frame {frame['frame']}: background render contains "
                f"{frame['transparent']} transparent and {frame['partial']} partial-alpha pixels."
            )
    return report


def assert_nearest_neighbor(
    native: str | Path, enlarged: str | Path, scale: int
) -> None:
    if not isinstance(scale, int) or scale < 1:
        raise ValueError("Scale must be a positive integer.")
    source = Path(native).resolve(strict=True)
    target = Path(enlarged).resolve(strict=True)
    output = _run(f"""
local originalSprite = app.activeSprite
local enlargedSprite = app.open("{lua_escape(str(target))}")
local original = Image(originalSprite.width,originalSprite.height)
original:drawSprite(originalSprite,1)
local enlarged = Image(enlargedSprite.width,enlargedSprite.height)
enlarged:drawSprite(enlargedSprite,1)
if enlarged.width~=original.width*{scale} or enlarged.height~=original.height*{scale} then
  print('MISMATCH:dimensions'); return
end
for y=0,enlarged.height-1 do for x=0,enlarged.width-1 do
  local a = original:getPixel(math.floor(x/{scale}),math.floor(y/{scale}))
  local b = enlarged:getPixel(x,y)
  local bothInvisible = app.pixelColor.rgbaA(a)==0 and app.pixelColor.rgbaA(b)==0
  if not bothInvisible and a~=b then
    print('MISMATCH:pixel '..x..','..y); return
  end
end end
print('NEAREST_MATCH')
""", source)
    mismatch = next((line for line in output.splitlines() if line.startswith("MISMATCH:")), None)
    if mismatch:
        raise ValueError(f"{enlarged} is not a {scale}x nearest-neighbor render: {mismatch}")
    if "NEAREST_MATCH" not in output.splitlines():
        raise RuntimeError(f"Missing scale validation result: {output}")
