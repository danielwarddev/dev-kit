# Text operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import text
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## list_text_fonts

```python
async def list_text_fonts() -> str:
```

List fonts that draw_text can use.

Covers bitmap sprite-sheet fonts and .ttf/.otf files installed under
~/.aseprite-mcp/fonts, plus TrueType fonts found in the system font
directories. Bitmap fonts are the right choice for pixel art: their
glyphs are already pixels and `size` scales them by whole numbers.

## measure_text

```python
async def measure_text(text: str, font: str, size: int=1, letter_spacing: int=0, bold: int=0, antialias: bool=False) -> str:
```

Measure text without drawing it.

Use this to size a panel or centre a label in one shot instead of
guessing and re-rendering.

Args:
    text: The string to measure
    font: Font name from list_text_fonts, or a path to a .ttf/.otf or
        bitmap font directory
    size: Pixel size for TrueType fonts; integer scale factor for bitmap
        fonts (default 1)
    letter_spacing: Extra pixels between glyphs (default 0)
    bold: Faux-bold passes; each grows strokes by 1px (default 0)
    antialias: Keep greyscale edges instead of hard pixels (default False)

Returns:
    width, height, advance_width, and the ink extents above/below the
    baseline.

## draw_text

```python
async def draw_text(filename: str, text: str, x: int, y: int, font: str, size: int=1, color: str='#FFFFFF', layer_name: Optional[str]=None, frame_index: int=1, anchor: str='topleft', letter_spacing: int=0, bold: int=0, outline_color: Optional[str]=None, outline_width: int=1, outline_diagonal: bool=True, shadow_color: Optional[str]=None, shadow_dx: int=1, shadow_dy: int=1, antialias: bool=False, create_if_missing: bool=True) -> str:
```

Draw text onto a sprite.

Glyphs are rasterised outside Aseprite and composited in, because the
Lua API has no text drawing of its own.

Args:
    filename: Aseprite file to modify
    text: String to draw
    x: X coordinate of the anchor point
    y: Y coordinate of the anchor point
    font: Font name from list_text_fonts, or a path to a .ttf/.otf or a
        bitmap font directory
    size: Pixel size for TrueType fonts; integer scale factor for bitmap
        fonts (default 1)
    color: Text colour, #RRGGBB or #RRGGBBAA (default white)
    layer_name: Layer to draw on; the active/first layer if omitted
    frame_index: Frame index starting at 1 (default 1)
    anchor: Which point of the text (x, y) refers to - one of topleft,
        top, topright, left, center, right, bottomleft, bottom,
        bottomright, baselineleft, baseline, baselineright
        (default topleft)
    letter_spacing: Extra pixels between glyphs (default 0)
    bold: Faux-bold passes; each grows strokes by 1px (default 0)
    outline_color: If set, draw a 1px outline in this colour
    outline_width: Outline thickness in pixels (default 1)
    outline_diagonal: Include diagonal neighbours in the outline, giving a
        rounded corner; False gives a boxier 4-way outline (default True)
    shadow_color: If set, draw a drop shadow in this colour
    shadow_dx: Shadow X offset (default 1)
    shadow_dy: Shadow Y offset (default 1)
    antialias: Keep greyscale edges instead of hard pixels (default False)
    create_if_missing: Create the layer/cel if absent (default True)
