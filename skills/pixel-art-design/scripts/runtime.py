import hashlib
import json
from pathlib import Path
import sys
import tempfile

from style_profile import SKILL_ROOT, outside_skill

sys.path.insert(0, str(SKILL_ROOT.parent / "aseprite" / "scripts"))

from aseprite_ops.core.commands import AsepriteCommand, lua_escape
from rendering import (
    COMPOSITE_SPRITE_LUA,
    alpha_report,
    assert_nearest_neighbor,
    assert_opaque_render,
)
from aseprite_ops.tools import animation, canvas, export, pixel_read


FONT = {
    "A": ("010", "101", "111", "101", "101"), "B": ("110", "101", "110", "101", "110"),
    "C": ("011", "100", "100", "100", "011"), "D": ("110", "101", "101", "101", "110"),
    "E": ("111", "100", "110", "100", "111"), "F": ("111", "100", "110", "100", "100"),
    "G": ("011", "100", "101", "101", "011"), "H": ("101", "101", "111", "101", "101"),
    "I": ("111", "010", "010", "010", "111"), "J": ("001", "001", "001", "101", "010"),
    "K": ("101", "101", "110", "101", "101"), "L": ("100", "100", "100", "100", "111"),
    "M": ("101", "111", "111", "101", "101"), "N": ("101", "111", "111", "111", "101"),
    "O": ("010", "101", "101", "101", "010"), "P": ("110", "101", "110", "100", "100"),
    "Q": ("010", "101", "101", "111", "011"), "R": ("110", "101", "110", "101", "101"),
    "S": ("011", "100", "010", "001", "110"), "T": ("111", "010", "010", "010", "010"),
    "U": ("101", "101", "101", "101", "111"), "V": ("101", "101", "101", "101", "010"),
    "W": ("101", "101", "111", "111", "101"), "X": ("101", "101", "010", "101", "101"),
    "Y": ("101", "101", "010", "010", "010"), "Z": ("111", "001", "010", "100", "111"),
}


def text_lua(x: int, y: int, text: str, target: str = "board") -> str:
    lines = []
    for index, character in enumerate(text):
        if character == " ":
            continue
        for dy, row in enumerate(FONT[character]):
            for dx, bit in enumerate(row):
                if bit == "1":
                    lines.append(
                        f"{target}:drawPixel({x+index*4+dx},{y+dy},"
                        "app.pixelColor.rgba(201,214,219,255))"
                    )
    return "\n".join(lines) + "\n"


def quote(value) -> str:
    return '"' + lua_escape(str(value)) + '"'


def rgba(value) -> tuple:
    if not isinstance(value, str):
        return tuple(value)
    channels = tuple(bytes.fromhex(value.removeprefix("#")))
    return channels + (255,) if len(channels) == 3 else channels


def data_lua(pixels: dict) -> str:
    return "{" + ",".join(
        "{" + ",".join(map(str, (x, y, *color))) + "}"
        for (x, y), color in sorted(pixels.items())
    ) + "}"


def image_lua(pixels: dict, width: int, height: int, name: str = "image") -> str:
    return (
        f"local {name}=Image({width},{height})\n"
        f"for _,p in ipairs({data_lua(pixels)}) do "
        f"{name}:drawPixel(p[1],p[2],app.pixelColor.rgba(p[3],p[4],p[5],p[6])) end\n"
    )


def execute(script: str, source: Path | None = None, marker: str = "SAVED") -> None:
    success, output = AsepriteCommand.execute_lua_script_checked(
        COMPOSITE_SPRITE_LUA + script, str(source) if source else None
    )
    if not success or marker not in output.splitlines():
        raise RuntimeError(output)


async def export_frame(source: Path, target: Path, scale: int = 1) -> None:
    target = outside_skill(target)
    result = await export.export_frame(str(source), 1, str(target), scale=scale)
    if not result.startswith("Frame 1 exported") or not target.is_file():
        raise RuntimeError(result)
    assert_nearest_neighbor(source, target, scale)


async def export_tag(source: Path, tag: str, target: Path, scale: int = 6) -> None:
    target = outside_skill(target)
    result = await export.export_tag(str(source), tag, str(target), scale=scale)
    if not result.startswith(f"Tag '{tag}' exported") or not target.is_file():
        raise RuntimeError(result)
    assert_opaque_render(target)


async def save_master(
    path: Path, frames: list, width: int, height: int,
    durations: list, tags: list, overwrite: bool = False,
) -> Path:
    path = outside_skill(path)
    if path.exists() and not overwrite:
        raise FileExistsError(path)
    result = await canvas.create_canvas(width, height, str(path))
    if not result.startswith("Canvas created successfully"):
        raise RuntimeError(result)
    names = list(frames[0])
    script = "local sprite=app.activeSprite\nlocal layers={}\n"
    for index, name in enumerate(names, 1):
        target = "sprite.layers[1]" if index == 1 else "sprite:newLayer()"
        script += f"layers[{index}]={target}; layers[{index}].name={quote(name)}\n"
    data = "{" + ",".join(
        "{" + ",".join(data_lua(frame.get(name, {})) for name in names) + "}"
        for frame in frames
    ) + "}"
    script += f"local data={data}\nlocal durations={{{','.join(map(str, durations))}}}\n"
    script += f"""
for index,frame in ipairs(data) do
  if index>1 then sprite:newEmptyFrame(index) end
  sprite.frames[index].duration=durations[index]/1000
  for layerIndex,pixels in ipairs(frame) do
    local image=Image({width},{height})
    for _,p in ipairs(pixels) do
      image:drawPixel(p[1],p[2],app.pixelColor.rgba(p[3],p[4],p[5],p[6]))
    end
    sprite:newCel(layers[layerIndex],index,image,Point(0,0))
  end
end
"""
    for name, first, last in tags:
        script += (
            f"do local tag=sprite:newTag({first},{last}); "
            f"tag.name={quote(name)} end\n"
        )
    script += "sprite:saveAs(sprite.filename)\nprint('SAVED')\n"
    execute(script, path)
    info = json.loads(await animation.get_sprite_info(str(path)))
    if (info["width"], info["height"], info["frames"]) != (width, height, len(frames)):
        raise RuntimeError("Saved master dimensions/frame count differ.")
    if info["durations_ms"] != durations:
        raise RuntimeError("Saved frame durations differ.")
    return path


async def save_sheet(path: Path, frames: list, width: int, height: int, columns: int) -> None:
    path = outside_skill(path)
    rows = (len(frames) + columns - 1) // columns
    script = f"local sheet=Image({width*columns},{height*rows})\n"
    for index, frame in enumerate(frames):
        script += f"do local composed=Image({width},{height})\n"
        for pixels in frame.values():
            script += "do\n" + image_lua(pixels, width, height)
            script += "composed:drawImage(image,Point(0,0),255,BlendMode.NORMAL)\nend\n"
        script += (
            f"sheet:drawImage(composed,Point({index%columns*width},"
            f"{index//columns*height}),255,BlendMode.NORMAL)\nend\n"
        )
    script += f"sheet:saveAs({quote(path)})\nprint('SAVED')\n"
    execute(script)


async def contact_sheet(
    path: Path, frames: list, labels: list, width: int, height: int,
    selected: list, scale: int,
) -> None:
    path = outside_skill(path)
    columns = 4
    rows = (len(selected) + columns - 1) // columns
    board_width, board_height = columns * (width + 4) + 8, rows * (height + 14) + 8
    script = f"""
local board=Image({board_width},{board_height})
for y=0,{board_height-1} do for x=0,{board_width-1} do
  board:drawPixel(x,y,app.pixelColor.rgba(44,53,68,255))
end end
"""
    for position, index in enumerate(selected):
        x, y = 4 + position % columns * (width + 4), 4 + position // columns * (height + 14)
        script += text_lua(x, y, labels[index])
        for pixels in frames[index].values():
            script += "do\n" + image_lua(pixels, width, height)
            script += f"board:drawImage(image,Point({x},{y+9}),255,BlendMode.NORMAL)\nend\n"
    with tempfile.TemporaryDirectory(prefix="pixel-art-sheet-") as directory:
        native = Path(directory) / "native.png"
        execute(script + f"board:saveAs({quote(native)})\nprint('SAVED')\n")
        await export_frame(native, path, scale)
        assert_opaque_render(path)


async def neutral_clips(master: Path, folder: Path, tags: list) -> None:
    info = json.loads(await animation.get_sprite_info(str(master)))
    width, height = info["width"], info["height"]
    with tempfile.TemporaryDirectory(prefix="pixel-art-preview-") as directory:
        preview = Path(directory) / "preview.aseprite"
        result = await canvas.create_canvas(width, height, str(preview))
        if not result.startswith("Canvas created successfully"):
            raise RuntimeError(result)
        script = f"""
local preview=app.activeSprite
local source=app.open({quote(master)})
for index=1,#source.frames do
  if index>1 then preview:newEmptyFrame(index) end
  preview.frames[index].duration=source.frames[index].duration
  local image=Image({width},{height})
  for y=0,{height-1} do for x=0,{width-1} do
    image:drawPixel(x,y,app.pixelColor.rgba(44,53,68,255))
  end end
  composite_sprite(image,source,index,Point(0,0))
  preview:newCel(preview.layers[1],index,image,Point(0,0))
end
"""
        for name, first, last in tags:
            script += f"do local tag=preview:newTag({first},{last}); tag.name={quote(name)} end\n"
        script += "preview:saveAs(preview.filename)\nprint('SAVED')\n"
        execute(script, preview)
        for name, first, last in tags:
            target = folder / f"{name}.gif"
            await export_tag(preview, name, target)
            gif = json.loads(await animation.get_sprite_info(str(target)))
            if gif["durations_ms"] != info["durations_ms"][first-1:last]:
                raise RuntimeError("GIF durations changed; use multiples of 10ms for exact GIF timing.")


async def read_layers(path: Path, names: list, width: int, height: int) -> dict:
    info = json.loads(await animation.get_sprite_info(str(path)))
    if (info["width"], info["height"]) != (width, height):
        raise ValueError(f"Expected a {width}x{height} source.")
    result = {}
    for name in names:
        rows = json.loads(await pixel_read.get_pixels_rect(
            str(path), 0, 0, width, height, layer_name=name
        ))
        result[name] = {
            (pixel["x"], pixel["y"]): tuple(pixel[channel] for channel in ("r", "g", "b", "a"))
            for pixel in rows if pixel["a"]
        }
        if not result[name]:
            raise ValueError(f"Source layer {name!r} is missing or empty.")
    return result


def record_run(
    folder: Path, operation: str, profile: dict, code: Path, parameters: dict,
    inputs=(), dependencies=(),
) -> None:
    success, version = AsepriteCommand.run_command(["--version"])
    if not success:
        raise RuntimeError(version)
    write_json(folder / "style-used.json", profile)
    code_paths = {}
    for path in (code, Path(__file__), SKILL_ROOT / "scripts" / "style_profile.py", *dependencies):
        path = Path(path).resolve()
        name = str(path.relative_to(SKILL_ROOT)) if path.is_relative_to(SKILL_ROOT) else str(path)
        code_paths[name] = path
    code_paths[str(Path("scripts") / "rendering.py")] = SKILL_ROOT / "scripts" / "rendering.py"
    write_json(folder / "run.json", {
        "operation": operation,
        "python": sys.version.split()[0],
        "aseprite": version.strip(),
        "code_sha256": {
            name: hashlib.sha256(path.read_bytes()).hexdigest()
            for name, path in code_paths.items()
        },
        "parameters": parameters,
        "inputs": [
            {"name": Path(path).name, "sha256": hashlib.sha256(Path(path).read_bytes()).hexdigest()}
            for path in inputs if path is not None
        ],
        "compositing": "fresh frame render followed by source-over alpha blending",
    })


def write_json(path: Path, value) -> None:
    outside_skill(path).write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")
