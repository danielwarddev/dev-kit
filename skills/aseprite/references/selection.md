# Selection operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import selection
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## move_region

```python
async def move_region(filename: str, layer_name: str, frame_index: int, x: int, y: int, width: int, height: int, dest_x: int, dest_y: int) -> str:
```

Cut a rectangular region of pixels and paste it at a new position.

Coordinates are sprite-global. The source area is left transparent.
Pixels moved outside the canvas are discarded. Fully transparent
source pixels do not overwrite destination pixels.

Args:
    filename: Aseprite file to modify
    layer_name: Layer to operate on
    frame_index: Frame index starting at 1
    x: Left edge of the source region
    y: Top edge of the source region
    width: Region width
    height: Region height
    dest_x: New left edge for the region
    dest_y: New top edge for the region

## copy_region

```python
async def copy_region(filename: str, layer_name: str, frame_index: int, x: int, y: int, width: int, height: int, dest_x: int, dest_y: int, target_layer_name: str='', target_frame_index: int=0) -> str:
```

Copy a rectangular region of pixels to another position, layer, or frame.

Coordinates are sprite-global. Fully transparent source pixels do not
overwrite destination pixels. The destination cel is created when missing.

Args:
    filename: Aseprite file to modify
    layer_name: Source layer
    frame_index: Source frame index starting at 1
    x: Left edge of the source region
    y: Top edge of the source region
    width: Region width
    height: Region height
    dest_x: Left edge of the destination
    dest_y: Top edge of the destination
    target_layer_name: Destination layer (default: same as source)
    target_frame_index: Destination frame (default: same as source)

## erase_region

```python
async def erase_region(filename: str, layer_name: str, frame_index: int, x: int, y: int, width: int, height: int) -> str:
```

Erase (make transparent) a rectangular region of pixels.

Coordinates are sprite-global.

Args:
    filename: Aseprite file to modify
    layer_name: Layer to operate on
    frame_index: Frame index starting at 1
    x: Left edge of the region
    y: Top edge of the region
    width: Region width
    height: Region height

## erase_color

```python
async def erase_color(filename: str, layer_name: str, frame_index: int, color: str, tolerance: int=0) -> str:
```

Make all pixels of a given color transparent (like a magic eraser).

Args:
    filename: Aseprite file to modify
    layer_name: Layer to operate on
    frame_index: Frame index starting at 1
    color: Hex color to erase, e.g. "#FF00FF"
    tolerance: Per-channel tolerance 0-255 (default 0 = exact match)
