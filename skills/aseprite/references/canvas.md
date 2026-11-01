# Canvas operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import canvas
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## create_canvas

```python
async def create_canvas(width: int, height: int, filename: str='canvas.aseprite') -> str:
```

Create a new Aseprite canvas with specified dimensions.

Args:
    width: Width of the canvas in pixels
    height: Height of the canvas in pixels
    filename: Name of the output file (default: canvas.aseprite)

## add_layer

```python
async def add_layer(filename: str, layer_name: str, group: str='') -> str:
```

Add a new layer to the Aseprite file.

Args:
    filename: Name of the Aseprite file to modify
    layer_name: Name of the new layer
    group: Optional group to place the new layer inside, by name or
        "group/subgroup" path (default: top level)

## add_group

```python
async def add_group(filename: str, group_name: str, parent_group: str='') -> str:
```

Add a new, empty group layer.

Combine with add_layer(group=...) / duplicate_layer(group=...) to build a
grouped layer structure.

Args:
    filename: Name of the Aseprite file to modify
    group_name: Name of the new group
    parent_group: Optional existing group to nest the new group inside, by
        name or "group/subgroup" path (default: top level)

## add_frame

```python
async def add_frame(filename: str) -> str:
```

Add a new frame to the Aseprite file.

Args:
    filename: Name of the Aseprite file to modify

## set_frame

```python
async def set_frame(filename: str, frame_index: int) -> str:
```

Set the active frame by index (1-based).

Args:
    filename: Name of the Aseprite file to modify
    frame_index: Frame index starting at 1

## set_frame_duration

```python
async def set_frame_duration(filename: str, frame_index: int, duration_ms: int) -> str:
```

Set the duration of a frame in milliseconds.

Args:
    filename: Name of the Aseprite file to modify
    frame_index: Frame index starting at 1
    duration_ms: Duration in milliseconds

## set_layer

```python
async def set_layer(filename: str, layer_name: str, create_if_missing: bool=False) -> str:
```

Set the active layer by name.

Args:
    filename: Name of the Aseprite file to modify
    layer_name: Layer name to activate
    create_if_missing: Create layer if it does not exist
