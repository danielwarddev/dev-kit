# Scene operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import scene
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## copy_layers_between_sprites

```python
async def copy_layers_between_sprites(source_filename: str, target_filename: str, layer_names: List[str], replace: bool=True, create_missing_frames: bool=True) -> str:
```

Copy layers by name from a source sprite to a target sprite.

Args:
    source_filename: Source .aseprite file
    target_filename: Target .aseprite file
    layer_names: List of layer names to copy
    replace: Overwrite existing cels in target layers
    create_missing_frames: Add frames to target if needed
