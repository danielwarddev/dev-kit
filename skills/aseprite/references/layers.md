# Layers operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import layers
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## delete_layer

```python
async def delete_layer(filename: str, layer_name: str) -> str:
```

Delete a layer by name.

Args:
    filename: Aseprite file to modify
    layer_name: Name of the layer to delete

## rename_layer

```python
async def rename_layer(filename: str, layer_name: str, new_name: str) -> str:
```

Rename a layer.

Args:
    filename: Aseprite file to modify
    layer_name: Current layer name
    new_name: New layer name

## duplicate_layer

```python
async def duplicate_layer(filename: str, layer_name: str, new_name: str='', group: str='') -> str:
```

Duplicate a layer with all its cels across every frame.

The copy inherits the source's opacity and blend mode. By default it is
placed directly above the source layer; pass `group` to place it inside a
group instead.

Args:
    filename: Aseprite file to modify
    layer_name: Layer to duplicate, by name or "group/subgroup/layer" path
    new_name: Name for the copy (default: "<layer_name> copy")
    group: Optional group to place the copy inside, by name or
        "group/subgroup" path (default: directly above the source)

## reorder_layer

```python
async def reorder_layer(filename: str, layer_name: str, position: int) -> str:
```

Move a layer to a new position in the layer stack.

Args:
    filename: Aseprite file to modify
    layer_name: Layer to move
    position: Target stack position, 1-based where 1 is the bottom layer

## set_layer_blend_mode

```python
async def set_layer_blend_mode(filename: str, layer_name: str, mode: str) -> str:
```

Set a layer's blend mode.

Args:
    filename: Aseprite file to modify
    layer_name: Layer to modify
    mode: One of: normal, darken, multiply, color_burn, lighten, screen,
        color_dodge, addition, overlay, soft_light, hard_light,
        difference, exclusion, subtract, divide, hue, saturation,
        color, luminosity

## merge_layer_down

```python
async def merge_layer_down(filename: str, layer_name: str) -> str:
```

Merge a layer into the layer directly below it.

Args:
    filename: Aseprite file to modify
    layer_name: Layer to merge down (must not be the bottom layer)

## flatten_sprite

```python
async def flatten_sprite(filename: str) -> str:
```

Flatten all layers into a single layer.

Args:
    filename: Aseprite file to modify
