# Pixel Read operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import pixel_read
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## get_pixel_color

```python
async def get_pixel_color(filename: str, x: int, y: int, layer_name: str='', frame_index: int=1) -> str:
```

Read the RGBA color of a single pixel.

Args:
    filename: Aseprite file to read
    x: X coordinate
    y: Y coordinate
    layer_name: Layer to read from (uses active layer when empty)
    frame_index: Frame index starting at 1

## get_pixels_rect

```python
async def get_pixels_rect(filename: str, x: int, y: int, width: int, height: int, layer_name: str='', frame_index: int=1) -> str:
```

Read all pixel colors in a rectangular region.

Args:
    filename: Aseprite file to read
    x: Top-left x coordinate
    y: Top-left y coordinate
    width: Width of the region
    height: Height of the region
    layer_name: Layer to read from (uses active layer when empty)
    frame_index: Frame index starting at 1

Returns:
    JSON array of {x, y, hex, r, g, b, a} objects

## get_composite_pixel

```python
async def get_composite_pixel(filename: str, x: int, y: int, frame_index: int=1) -> str:
```

Read the RGBA colour VISIBLE at a pixel (flattened composite of all layers).

Unlike get_pixel_color (which reads a single cel), this composites every
visible layer — "what the player actually sees" — by flattening a throwaway
clone. Essential for value/CVD QA on grouped/multi-layer scenes.

Args:
    filename: Aseprite file to read
    x: X coordinate (sprite-global)
    y: Y coordinate (sprite-global)
    frame_index: Frame index starting at 1

## get_composite_rect

```python
async def get_composite_rect(filename: str, x: int, y: int, width: int, height: int, frame_index: int=1) -> str:
```

Read VISIBLE RGBA over a rectangle (flattened composite of all layers).

The rectangular counterpart of get_composite_pixel — reads the composited
pixels every visible layer produces, not a single cel.

Args:
    filename: Aseprite file to read
    x: Top-left x (sprite-global)
    y: Top-left y (sprite-global)
    width: Region width
    height: Region height
    frame_index: Frame index starting at 1

Returns:
    JSON array of {x, y, hex, r, g, b, a} objects
