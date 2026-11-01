# Palette operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import palette
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## get_palette

```python
async def get_palette(filename: str) -> str:
```

Get the active sprite palette as a JSON array of hex colors.

## set_palette

```python
async def set_palette(filename: str, colors: List[str]) -> str:
```

Set the active sprite palette using a list of hex colors.

## remap_colors_in_cel_range

```python
async def remap_colors_in_cel_range(filename: str, layer_name: str, start_frame: int, end_frame: int, mappings: List[dict], create_missing_cels: bool=False, source_frame_index: int | None=None) -> str:
```

Remap colors in a layer across a frame range using explicit mappings.

Args:
    filename (str): The path to the Aseprite file.
    layer_name (str): The name of the layer to remap colors in.
    start_frame (int): The start frame index to remap colors in.
    end_frame (int): The end frame index to remap colors in.
    mappings (List[dict]): The color mappings to use.
    create_missing_cels (bool, optional): Whether to create missing cels. Defaults to False.
    source_frame_index (int | None, optional): The source frame index to use for remapping. Defaults to None.

## list_palette_presets

```python
async def list_palette_presets() -> str:
```

List the built-in retro palette presets with their colors.

Returns:
    JSON object mapping preset name to its list of hex colors.

## apply_palette_preset

```python
async def apply_palette_preset(filename: str, preset: str) -> str:
```

Set the sprite palette to a built-in retro preset.

This only sets the palette; existing pixels keep their colors.
Use quantize_to_palette afterwards to snap pixels to the new palette.

Args:
    filename: Aseprite file to modify
    preset: One of: gameboy, monochrome, grayscale_4, cga, pico8, c64,
        dawnbringer16, dawnbringer32

## generate_color_ramp

```python
async def generate_color_ramp(base_color: str, steps: int=5, hue_shift_degrees: float=20, lightness_range: float=0.5) -> str:
```

Generate a shading ramp (dark to light) from a base color.

Produces the standard pixel-art shading technique of hue-shifting:
shadows lean cooler (hue shifted one way), highlights lean warmer.
Use the returned colors for shading instead of plain darker/lighter
versions of the same hue.

Args:
    base_color: Hex color the ramp is built around, e.g. "#D04648"
    steps: Number of colors in the ramp, 2-16 (default 5)
    hue_shift_degrees: Total hue rotation across the ramp (default 20)
    lightness_range: Total lightness span across the ramp, 0-1 (default 0.5)

Returns:
    JSON array of hex colors ordered darkest to lightest.

## quantize_to_palette

```python
async def quantize_to_palette(filename: str, layer_name: str='', start_frame: int=1, end_frame: int=0) -> str:
```

Snap every pixel to the nearest color in the sprite's palette.

Walks the chosen cels and replaces each opaque pixel with the
closest palette color (RGB distance). Run after apply_palette_preset
or set_palette to make existing art conform to the palette.

Args:
    filename: Aseprite file to modify
    layer_name: Layer to quantize (empty = all top-level layers)
    start_frame: First frame to process (default 1)
    end_frame: Last frame to process (default 0 = last frame)

## set_color_mode

```python
async def set_color_mode(filename: str, mode: str) -> str:
```

Convert the sprite's color mode.

Args:
    filename: Aseprite file to modify
    mode: "rgb", "grayscale", or "indexed"
