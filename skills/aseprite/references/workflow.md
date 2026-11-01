# Art, animation, and feedback

## Drawing

Decide canvas size, target silhouette, palette, layers, and output formats first.
For pixel art, work at native resolution; use nearest-neighbor enlarged previews
for inspection, not enlarged working canvases.

Block the silhouette, inspect it, then add major color regions, shading, and detail.
Use named layers and explicit `*_at` calls. Batch pixel data into a single call
instead of one Aseprite launch per pixel. Keep large coordinate arrays in task
files, not context. Use ramps, palette presets, quantization, and dithering where
appropriate; do not default to smooth gradients for palette-constrained art.

Export frame previews at 8-10x (`export.export_frame`) and inspect actual images.
Check both native-size readability and enlarged pixel placement. Inspect composited
pixels when multiple layers or groups affect the visible result.

## Animation

Block key poses first. Copy frames/cels to preserve structure, then redraw only
what changes. Propagate static layers across the animation; put secondary movement
on separate layers. Use position/opacity/scale tweens for suitable motion rather
than redrawing everything. Do not assume tweening alone makes convincing character
motion: evaluate silhouette, arcs, contact, anticipation, and timing.

For environments, construct the base once, copy it for variants, keep sky/ground
static, and animate clouds, water, birds, or foliage independently.

Set frame durations and tags explicitly. Render onion-skin previews or frame diffs
when needed. Export a GIF and a sheet for final timing and frame-order inspection.
Check metadata for exact frame counts, durations, and tag bounds.

Use `quality.validate_scene`/`audit_animation` when layer/cel coverage matters.
Sanitization can change cels and opacity: run `animation_sanitize(report_only=True)`
first, then apply only intended fixes. Overlapping cels are not automatically
wrong; full-canvas backgrounds and deliberate overlaps are common.

## Local HTTP previews

Direct image inspection is normally enough. For browser inspection/animation,
serve only the output directory using Python's existing foreground HTTP server:

```powershell
python -m http.server 8000 --bind 127.0.0.1 --directory 'C:\path\to\preview-output'
```

Run this in an attached background shell session if the agent runtime supports it.
Verify responsiveness with `Invoke-WebRequest http://127.0.0.1:8000/`.
Stop that specific session when finished. Do not detach processes, expose the
workspace on all interfaces, or kill processes by name.

The MCP's `start_preview_server`/`stop_preview_server` functions were deliberately
not bundled: they detach processes and use port-keyed temporary PID files.
The standard server provides the same viewing workflow without that lifecycle code.
