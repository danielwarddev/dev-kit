# Quality operations

Read only the operation needed. Import this module in a task script as:

```python
from aseprite_ops.tools import quality
```

These are async Python functions, not shell commands. Await each call,
print its result, and stop on errors; see `execution.md`.
Parameters and behavior below are retained from the source implementation.

## ensure_layers_present

```python
async def ensure_layers_present(filename: str, layer_names: List[str], start_frame: int=1, end_frame: int | None=None) -> str:
```

Ensure cels exist for layers across a frame range.

## validate_scene

```python
async def validate_scene(filename: str, required_layers: List[str], start_frame: int=1, end_frame: int | None=None) -> str:
```

Validate presence of layers and cels across a frame range.

Returns JSON with missing layers and missing cels.

## audit_animation

```python
async def audit_animation(filename: str, start_frame: int=1, end_frame: int | None=None, layer_names: List[str] | None=None, overlap_pairs: List[str] | None=None, layer_frame_ranges: List[str] | None=None, report_cels: bool=False, report_bounds: bool=False, max_overlaps: int=200, max_out_of_range: int=200) -> str:
```

Audit animation frames for overlaps and out-of-range layer activity.

overlap_pairs format: ["layerA,layerB", "layerC:layerD"]
layer_frame_ranges format: ["layer:1-8,17-24", "clouds:1-12"]
Returns JSON for AI consumption (summary, overlaps, out_of_range, optional cels).

## animation_sanitize

```python
async def animation_sanitize(filename: str, start_frame: int=1, end_frame: int | None=None, layer_names: List[str] | None=None, layer_order: List[str] | None=None, layer_frame_ranges: List[str] | None=None, ensure_layers: List[str] | None=None, overlap_pairs: List[str] | None=None, report_bounds: bool=False, max_overlaps: int=200, ignore_full_canvas_overlaps: bool=True, out_of_range_action: str='set_opacity_zero', out_of_range_opacity: int=0, report_only: bool=False, include_stats: bool=True) -> str:
```

Normalize animation consistency and optionally apply fixes.

layer_frame_ranges format: ["layer:1-8,17-24", "clouds:1-12"]
out_of_range_action: "set_opacity_zero", "delete_cels", "none"
ignore_full_canvas_overlaps: skip overlap checks when a cel is full canvas
Returns JSON for AI consumption (summary, layer_stats, alerts, overlaps).
