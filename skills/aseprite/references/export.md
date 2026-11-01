# Export operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import export
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## export_sprite

```python
async def export_sprite(filename: str, output_filename: str, format: str='png') -> str:
```

Export the Aseprite file to another format.

Args:
    filename: Name of the Aseprite file to export
    output_filename: Name of the output file
    format: Output format (default: "png", can be "png", "gif", "jpg", etc.)

## copy_sprite

```python
async def copy_sprite(filename: str, output_filename: str, overwrite: bool=False) -> str:
```

Copy a sprite to a new Aseprite file.

Args:
    filename: Name of the Aseprite file to copy
    output_filename: Name of the output .aseprite file
    overwrite: Whether to overwrite if output exists

## export_frame

```python
async def export_frame(filename: str, frame_index: int, output_filename: str, scale: int=1) -> str:
```

Export a single frame as a PNG, optionally scaled up.

Use this for visual feedback while drawing: export at scale 8-10 and
open the PNG to inspect the result, then keep iterating.

Args:
    filename: Aseprite file to export
    frame_index: Frame index starting at 1
    output_filename: Output PNG path
    scale: Integer nearest-neighbor scale factor (default 1)

## export_spritesheet

```python
async def export_spritesheet(filename: str, output_filename: str, sheet_type: str='horizontal', data_filename: str='', scale: int=1, padding: int=0, tag_name: str='', data_format: str='json-array', list_tags: bool=False) -> str:
```

Export frames as a sprite sheet, optionally with a JSON data file.

Args:
    filename: Aseprite file to export
    output_filename: Output sheet image path (PNG)
    sheet_type: Layout: "horizontal", "vertical", "rows", "columns", or "packed"
    data_filename: Optional path for a JSON metadata file
    scale: Integer scale factor applied before packing (default 1)
    padding: Padding in pixels between frames (default 0)
    tag_name: Only include frames of this animation tag (default: all frames)
    data_format: JSON format for the data file: "json-array" (default) or "json-hash"
    list_tags: Include animation tag metadata in the JSON data file (default: False)

## export_layers

```python
async def export_layers(filename: str, output_directory: str, include_hidden: bool=False) -> str:
```

Export each layer as its own PNG file named <layer>.png.

Args:
    filename: Aseprite file to export
    output_directory: Directory for the per-layer PNGs (created if missing)
    include_hidden: Also export hidden layers (default False)

## export_tag

```python
async def export_tag(filename: str, tag_name: str, output_filename: str, scale: int=1) -> str:
```

Export the frames of an animation tag as a GIF or PNG sequence.

Args:
    filename: Aseprite file to export
    tag_name: Animation tag to export
    output_filename: Output path; .gif gives an animation, .png a sequence
    scale: Integer scale factor (default 1)

## import_image_as_layer

```python
async def import_image_as_layer(filename: str, image_path: str, layer_name: str, frame_index: int=1, x: int=0, y: int=0) -> str:
```

Import an image file (PNG, etc.) into a layer of the sprite.

Useful for bringing in reference images or composing pre-made parts.
The layer is created if it does not exist. Works best when the sprite
is in RGB color mode.

Args:
    filename: Aseprite file to modify
    image_path: Image file to import
    layer_name: Layer to place the image on
    frame_index: Frame index starting at 1 (default 1)
    x: X position for the image's top-left corner (default 0)
    y: Y position for the image's top-left corner (default 0)
