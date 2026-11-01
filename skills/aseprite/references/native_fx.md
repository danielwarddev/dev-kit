# Native Fx operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import native_fx
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## outline_native

```python
async def outline_native(filename: str, layer_name: str='', frame_index: int=1, color: str='#000000', place: str='outside', matrix: str='circle') -> str:
```

Native Aseprite Outline around opaque pixels (app.command.Outline).

Higher quality than the 1px hand-rolled outline_cel: inside/outside
placement + a circle/square brush. Works best on a full-canvas cel.

Args:
    filename: Aseprite file to modify
    layer_name: layer to outline (empty = active layer)
    frame_index: 1-based frame
    color: outline colour as #RRGGBB
    place: "outside" or "inside"
    matrix: "circle" or "square"

## adjust_hsl_native

```python
async def adjust_hsl_native(filename: str, layer_name: str='', frame_index: int=1, hue: int=0, saturation: int=0, lightness: int=0, x: int=0, y: int=0, width: int=0, height: int=0) -> str:
```

Native Hue/Saturation/Lightness filter (engine-quality vs adjust_hsl).

Args:
    filename: Aseprite file to modify
    layer_name: layer to adjust (empty = active layer)
    frame_index: 1-based frame
    hue: -180..180 (degrees)
    saturation: -100..100
    lightness: -100..100
    x, y, width, height: optional region (width>0 & height>0 to scope)

## adjust_brightness_contrast

```python
async def adjust_brightness_contrast(filename: str, layer_name: str='', frame_index: int=1, brightness: int=0, contrast: int=0, x: int=0, y: int=0, width: int=0, height: int=0) -> str:
```

Native Brightness/Contrast filter (app.command.BrightnessContrast).

Args:
    filename: Aseprite file to modify
    layer_name: layer to adjust (empty = active layer)
    frame_index: 1-based frame
    brightness: -100..100
    contrast: -100..100
    x, y, width, height: optional region (width>0 & height>0 to scope)

## invert_colors

```python
async def invert_colors(filename: str, layer_name: str='', frame_index: int=1, x: int=0, y: int=0, width: int=0, height: int=0) -> str:
```

Native colour inversion (app.command.InvertColor).

Args:
    filename: Aseprite file to modify
    layer_name: layer to invert (empty = active layer)
    frame_index: 1-based frame
    x, y, width, height: optional region (width>0 & height>0 to scope)

## apply_convolution

```python
async def apply_convolution(filename: str, matrix: str, layer_name: str='', frame_index: int=1, x: int=0, y: int=0, width: int=0, height: int=0) -> str:
```

Native convolution filter (blur / sharpen / edge / emboss …).

Args:
    filename: Aseprite file to modify
    matrix: a built-in matrix name (see list_convolution_matrices),
        e.g. "blur-3x3", "sharpen-3x3", "edges-find", "misc-emboss"
    layer_name: layer to filter (empty = active layer)
    frame_index: 1-based frame
    x, y, width, height: optional region (width>0 & height>0 to scope)

## list_convolution_matrices

```python
async def list_convolution_matrices() -> str:
```

List the built-in convolution-matrix names usable with apply_convolution.

Returns:
    JSON array of matrix resource names.

## extract_palette

```python
async def extract_palette(filename: str, max_colors: int=16, with_alpha: bool=False) -> str:
```

Build an OPTIMAL palette from the sprite via native ColorQuantization.

True palette extraction (vs the nearest-snap quantize_to_palette): writes
the resulting palette to the sprite and returns it. NOTE: mutates the
sprite's palette. Sprite must be in RGB mode.

Args:
    filename: Aseprite file to modify
    max_colors: palette size cap, 1..256 (fewer if the art has fewer)
    with_alpha: include alpha when quantizing

Returns:
    JSON {colors: [#RRGGBB, ...], count}
