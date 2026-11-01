# Analysis operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import analysis
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## render_onion_skin

```python
async def render_onion_skin(filename: str, frame_index: int, output_filename: str, before: int=1, after: int=1, scale: int=4, ghost_opacity: int=100) -> str:
```

Render a frame with neighboring frames as translucent onion-skin ghosts.

Produces a PNG of the given frame composited over ghosted copies of
the surrounding frames — the batch-mode equivalent of Aseprite's
onion skinning. Essential for checking motion continuity while
animating: export it, open the PNG, and verify the in-between
positions line up.

Args:
    filename: Aseprite file to read
    frame_index: Frame to render, starting at 1
    output_filename: Output PNG path
    before: Number of previous frames to ghost (default 1)
    after: Number of following frames to ghost (default 1)
    scale: Integer nearest-neighbor scale factor (default 4)
    ghost_opacity: Opacity of ghost frames, 0-255 (default 100)

## compare_frames

```python
async def compare_frames(filename: str, frame_a: int, frame_b: int) -> str:
```

Compare two frames and report how much they differ.

Flattens the sprite (non-destructively) and diffs the two frames
pixel by pixel. Use while animating to confirm a frame actually
changed, or that it did not change too much.

Args:
    filename: Aseprite file to read
    frame_a: First frame index, starting at 1
    frame_b: Second frame index, starting at 1

Returns:
    JSON with changed pixel count, total pixels, percent changed,
    and the bounding box of the changed region.

## get_color_stats

```python
async def get_color_stats(filename: str, frame_index: int=1, top: int=16) -> str:
```

Report the colors used in a frame and how often each appears.

Flattens the sprite (non-destructively) and histograms the frame.
Use to check palette discipline: too many near-duplicate colors is
a common pixel-art mistake.

Args:
    filename: Aseprite file to read
    frame_index: Frame to analyze, starting at 1 (default 1)
    top: How many of the most-used colors to list (default 16)

Returns:
    JSON with unique color count, opaque pixel count, and the top
    colors with usage counts.
