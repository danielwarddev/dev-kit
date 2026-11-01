# Fx operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import fx
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## outline_cel

```python
async def outline_cel(filename: str, layer_name: str, frame_index: int, color: str='#000000', include_diagonals: bool=False) -> str:
```

Add a 1px outline around all opaque pixels of a cel.

Transparent pixels adjacent to opaque pixels are filled with the
outline color. Great for making sprites read clearly against any
background.

Args:
    filename: Aseprite file to modify
    layer_name: Layer to outline
    frame_index: Frame index starting at 1
    color: Outline hex color (default black)
    include_diagonals: Also outline diagonal neighbors for a thicker,
        rounded outline (default False)

## replace_color

```python
async def replace_color(filename: str, layer_name: str, frame_index: int, from_color: str, to_color: str, tolerance: int=0) -> str:
```

Replace one color with another in a cel, preserving alpha.

Args:
    filename: Aseprite file to modify
    layer_name: Layer to operate on
    frame_index: Frame index starting at 1
    from_color: Hex color to replace, e.g. "#FF0000"
    to_color: Replacement hex color
    tolerance: Per-channel tolerance 0-255 (default 0 = exact match)

## adjust_hsl

```python
async def adjust_hsl(filename: str, layer_name: str, frame_index: int, hue_shift: float=0, saturation_shift: float=0, lightness_shift: float=0) -> str:
```

Shift hue, saturation, and lightness of all opaque pixels in a cel.

Useful for creating palette-swapped variants and shading: e.g. darken
a duplicated layer for shadows or hue-shift toward blue for night
scenes.

Args:
    filename: Aseprite file to modify
    layer_name: Layer to adjust
    frame_index: Frame index starting at 1
    hue_shift: Degrees to rotate hue, -360 to 360
    saturation_shift: Saturation delta, -100 to 100
    lightness_shift: Lightness delta, -100 to 100

## apply_dither_gradient

```python
async def apply_dither_gradient(filename: str, layer_name: str, frame_index: int, x: int, y: int, width: int, height: int, color_start: str, color_end: str, horizontal: bool=False, create_if_missing: bool=True) -> str:
```

Fill a rectangle with a two-color gradient using Bayer 4x4 ordered dithering.

This is the classic pixel-art way to blend two colors without
introducing new intermediate colors. The gradient runs from
color_start (top/left) to color_end (bottom/right).

Args:
    filename: Aseprite file to modify
    layer_name: Layer to draw on
    frame_index: Frame index starting at 1
    x: Left edge of the rectangle
    y: Top edge of the rectangle
    width: Rectangle width
    height: Rectangle height
    color_start: Hex color at the start of the gradient
    color_end: Hex color at the end of the gradient
    horizontal: Run the gradient left-to-right instead of top-to-bottom
    create_if_missing: Create the cel if it does not exist

## apply_dither_pattern

```python
async def apply_dither_pattern(filename: str, layer_name: str, frame_index: int, x: int, y: int, width: int, height: int, color_a: str, color_b: str, density: float=0.5, create_if_missing: bool=True) -> str:
```

Fill a rectangle with a uniform Bayer-dithered mix of two colors.

density controls the ratio: 0.0 = all color_a, 0.5 = checkerboard,
1.0 = all color_b. Useful for textures (stone, grass) and flat
mid-tones between two palette colors.

Args:
    filename: Aseprite file to modify
    layer_name: Layer to draw on
    frame_index: Frame index starting at 1
    x: Left edge of the rectangle
    y: Top edge of the rectangle
    width: Rectangle width
    height: Rectangle height
    color_a: Base hex color
    color_b: Mixed-in hex color
    density: Fraction of color_b, 0.0-1.0 (default 0.5)
    create_if_missing: Create the cel if it does not exist
