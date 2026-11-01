# Transform operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import transform
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## flip_layer

```python
async def flip_layer(filename: str, layer_name: str, frame_index: int, direction: str='horizontal') -> str:
```

Flip a layer's image horizontally or vertically.

Args:
    filename: Aseprite file to modify
    layer_name: Layer name to flip
    frame_index: Frame index starting at 1
    direction: "horizontal" or "vertical"

## rotate_layer

```python
async def rotate_layer(filename: str, layer_name: str, frame_index: int, angle: int=90) -> str:
```

Rotate a layer's image 90, 180, or 270 degrees clockwise.

Args:
    filename: Aseprite file to modify
    layer_name: Layer name to rotate
    frame_index: Frame index starting at 1
    angle: Rotation angle: 90, 180, or 270 (clockwise)

## resize_canvas

```python
async def resize_canvas(filename: str, width: int, height: int) -> str:
```

Scale the canvas and all its content to new dimensions.

Args:
    filename: Aseprite file to modify
    width: New canvas width in pixels
    height: New canvas height in pixels

## crop_canvas

```python
async def crop_canvas(filename: str, x: int, y: int, width: int, height: int) -> str:
```

Crop the canvas to the given rectangle, discarding content outside it.

Args:
    filename: Aseprite file to modify
    x: Left edge of the crop area
    y: Top edge of the crop area
    width: Width of the crop area
    height: Height of the crop area
