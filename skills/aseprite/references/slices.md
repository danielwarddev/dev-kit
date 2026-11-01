# Slices operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import slices
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## create_slice

```python
async def create_slice(filename: str, name: str, x: int, y: int, width: int, height: int) -> str:
```

Create a named slice (a rectangular region usable by game engines).

Args:
    filename: Aseprite file to modify
    name: Slice name
    x: Left edge of the slice
    y: Top edge of the slice
    width: Slice width
    height: Slice height

## set_slice_center

```python
async def set_slice_center(filename: str, name: str, x: int, y: int, width: int, height: int) -> str:
```

Set a slice's 9-patch center rectangle (relative to the slice origin).

The center defines the stretchable region for 9-patch scaling in
game engines.

Args:
    filename: Aseprite file to modify
    name: Slice name
    x: Center-rect left edge, relative to the slice
    y: Center-rect top edge, relative to the slice
    width: Center-rect width
    height: Center-rect height

## set_slice_pivot

```python
async def set_slice_pivot(filename: str, name: str, x: int, y: int) -> str:
```

Set a slice's pivot point (relative to the slice origin).

Args:
    filename: Aseprite file to modify
    name: Slice name
    x: Pivot x, relative to the slice
    y: Pivot y, relative to the slice

## list_slices

```python
async def list_slices(filename: str) -> str:
```

List all slices with their bounds, 9-patch centers, and pivots.

Returns:
    JSON array of {name, x, y, width, height, center?, pivot?}.

## delete_slice

```python
async def delete_slice(filename: str, name: str) -> str:
```

Delete a slice by name.

Args:
    filename: Aseprite file to modify
    name: Slice name to delete
