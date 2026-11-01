# Animation operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import animation
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## add_frames

```python
async def add_frames(filename: str, count: int, duration_ms: int | None=None) -> str:
```

Add multiple frames to a sprite and optionally set their duration.

Args:
    filename: Name of the Aseprite file to modify
    count: Number of frames to add
    duration_ms: Optional duration for each new frame in milliseconds

## set_frame_duration_all

```python
async def set_frame_duration_all(filename: str, duration_ms: int) -> str:
```

Set the duration of all frames in milliseconds.

Args:
    filename: Name of the Aseprite file to modify
    duration_ms: Duration in milliseconds

## set_layer_visibility

```python
async def set_layer_visibility(filename: str, layer_name: str, visible: bool=True) -> str:
```

Set layer visibility by name.

Args:
    filename: Name of the Aseprite file to modify
    layer_name: Layer name to target
    visible: Whether the layer should be visible

## set_layer_opacity

```python
async def set_layer_opacity(filename: str, layer_name: str, opacity: int) -> str:
```

Set layer opacity by name (0-255).

Args:
    filename: Name of the Aseprite file to modify
    layer_name: Layer name to target
    opacity: Opacity value 0-255

## get_sprite_info

```python
async def get_sprite_info(filename: str) -> str:
```

Return sprite info as JSON string (size, color mode, frame durations, layers, tags).

Args:
    filename: Name of the Aseprite file to inspect

## duplicate_frame_range

```python
async def duplicate_frame_range(filename: str, start_frame: int, end_frame: int, times: int=1) -> str:
```

Duplicate a frame range and append copies to the end.

Args:
    filename: Name of the Aseprite file to modify
    start_frame: Starting frame index (1-based)
    end_frame: Ending frame index (1-based, inclusive)
    times: Number of times to append the range (default: 1)

## set_cel_position

```python
async def set_cel_position(filename: str, layer_name: str, frame_index: int, x: int, y: int, create_if_missing: bool=False, source_frame_index: int | None=None) -> str:
```

Set a cel's position in a specific layer and frame.

Args:
    filename: Name of the Aseprite file to modify
    layer_name: Layer name to target
    frame_index: Frame index starting at 1
    x: X position in pixels
    y: Y position in pixels
    create_if_missing: Create the cel if it does not exist
    source_frame_index: Optional frame to copy the cel image from

## tween_cel_positions

```python
async def tween_cel_positions(filename: str, layer_name: str, start_frame: int, end_frame: int, start_x: int, start_y: int, end_x: int, end_y: int, create_missing_cels: bool=False, source_frame_index: int | None=None) -> str:
```

Tween cel positions linearly across a frame range.

Args:
    filename: Name of the Aseprite file to modify
    layer_name: Layer name to target
    start_frame: Starting frame index (1-based)
    end_frame: Ending frame index (1-based, inclusive)
    start_x: Starting X position in pixels
    start_y: Starting Y position in pixels
    end_x: Ending X position in pixels
    end_y: Ending Y position in pixels
    create_missing_cels: Create missing cels during tween
    source_frame_index: Optional frame to copy the cel image from

## offset_cel_positions

```python
async def offset_cel_positions(filename: str, layer_name: str, start_frame: int, end_frame: int, dx: int, dy: int) -> str:
```

Offset cel positions by a delta across a frame range.

Args:
    filename: Name of the Aseprite file to modify
    layer_name: Layer name to target
    start_frame: Starting frame index (1-based)
    end_frame: Ending frame index (1-based, inclusive)
    dx: X delta in pixels
    dy: Y delta in pixels

## create_cel

```python
async def create_cel(filename: str, layer_name: str, frame_index: int, x: int=0, y: int=0) -> str:
```

Create an empty cel on a layer/frame.

Args:
    filename: Name of the Aseprite file to modify
    layer_name: Layer name to target
    frame_index: Frame index starting at 1
    x: X position in pixels
    y: Y position in pixels

## clear_cel

```python
async def clear_cel(filename: str, layer_name: str, frame_index: int) -> str:
```

Delete a cel on a layer/frame.

## copy_cel

```python
async def copy_cel(filename: str, layer_name: str, source_frame: int, target_frame: int, replace: bool=True) -> str:
```

Copy a cel from one frame to another.

## copy_frame

```python
async def copy_frame(filename: str, source_frame: int, target_frame: int | None=None, overwrite: bool=True) -> str:
```

Copy all cels from a source frame to a target frame (or append).

## propagate_frame_to_range

```python
async def propagate_frame_to_range(filename: str, source_frame: int, start_frame: int, end_frame: int, overwrite: bool=True) -> str:
```

Copy all layers from a source frame to a range of frames.

Args:
    filename: Name of the Aseprite file to modify
    source_frame: Frame index to copy from (1-based)
    start_frame: Start frame index (1-based)
    end_frame: End frame index (1-based, inclusive)
    overwrite: Whether to overwrite existing cels (default: True)

## set_tag

```python
async def set_tag(filename: str, name: str, from_frame: int, to_frame: int, direction: str='forward') -> str:
```

Create or update an animation tag on the sprite.

direction: forward | reverse | pingpong | pingpong_reverse

## set_onion_skin

```python
async def set_onion_skin(filename: str, enabled: bool=True, before: int=2, after: int=2, opacity: int=128) -> str:
```

Configure onion skin settings for Aseprite.

## propagate_cels

```python
async def propagate_cels(filename: str, layer_names: list[str], source_frame: int, start_frame: int, end_frame: int, replace: bool=True) -> str:
```

Copy cels from a source frame to a range of frames for specific layers.

Args:
    filename: Name of the Aseprite file to modify
    layer_names: List of layer names to copy
    source_frame: Frame index to copy from (1-based)
    start_frame: Start frame index (1-based)
    end_frame: End frame index (1-based, inclusive)
    replace: Whether to overwrite existing cels (default: True)

## tween_cel_positions_eased

```python
async def tween_cel_positions_eased(filename: str, layer_name: str, start_frame: int, end_frame: int, start_x: int, start_y: int, end_x: int, end_y: int, easing: str='smoothstep', create_missing_cels: bool=False, source_frame_index: int | None=None) -> str:
```

Tween cel positions with easing across a frame range.

## oscillate_cel_positions

```python
async def oscillate_cel_positions(filename: str, layer_name: str, start_frame: int, end_frame: int, amplitude_x: int=0, amplitude_y: int=0, cycles: float=1.0, phase_deg: float=0.0, create_missing_cels: bool=False, source_frame_index: int | None=None) -> str:
```

Oscillate cel positions across a frame range using a sine wave.

## tween_cel_opacity_eased

```python
async def tween_cel_opacity_eased(filename: str, layer_name: str, start_frame: int, end_frame: int, start_opacity: int, end_opacity: int, easing: str='smoothstep', create_missing_cels: bool=False, source_frame_index: int | None=None) -> str:
```

Tween cel opacity with easing across a frame range.

## tween_cel_scale_eased

```python
async def tween_cel_scale_eased(filename: str, layer_name: str, start_frame: int, end_frame: int, start_scale: float, end_scale: float, easing: str='smoothstep', anchor: str='center', replace: bool=True, create_missing_cels: bool=True, source_frame_index: int | None=None) -> str:
```

Tween cel scale with easing across a frame range.

## delete_frame

```python
async def delete_frame(filename: str, frame_index: int) -> str:
```

Delete a frame by index.

Args:
    filename: Aseprite file to modify
    frame_index: Frame index starting at 1

## delete_tag

```python
async def delete_tag(filename: str, name: str) -> str:
```

Delete an animation tag by name.

Args:
    filename: Aseprite file to modify
    name: Tag name to delete

## set_cel_opacity

```python
async def set_cel_opacity(filename: str, layer_name: str, frame_index: int, opacity: int) -> str:
```

Set the opacity of a single cel (0-255).

Args:
    filename: Aseprite file to modify
    layer_name: Layer containing the cel
    frame_index: Frame index starting at 1
    opacity: Opacity 0 (transparent) to 255 (opaque)
