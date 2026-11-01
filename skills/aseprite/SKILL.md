---
name: aseprite
description: 'Use when creating, editing, inspecting, or exporting Aseprite pixel art, sprites, sprite sheets, palettes, tilemaps, or animations. Uses bundled operation scripts and Aseprite batch mode instead of MCP. Includes drawing, layers, animation, effects, text, visual feedback, and arbitrary Lua.'
---

# Aseprite

Use the bundled scripts, not the Aseprite MCP server. There is no custom CLI:
write a short Python task script importing only the modules you need.
The scripts retain the original server's operations and generated Lua.

## Start here

1. Read `references\execution.md` for setup, invocation, errors, and paths.
2. Read only the relevant topic below; use heading searches/ranges for large files.
3. Execute a bounded batch of edits, export a preview, inspect the image, and iterate.
   Never claim visual quality from successful commands alone.

Do not read all references or source modules. Signatures and parameter descriptions
are in the references; read implementation only to debug a specific operation.
Resolve relative skill paths from this skill's base directory, not the task cwd.

| Need | Reference |
|---|---|
| New sprite, groups, layers, initial frames | `references\canvas.md` |
| Pixels, shapes, fills, gradients | `references\drawing.md` |
| Layer order, duplication, blending, merging | `references\layers.md` |
| Frame/cel copying, movement, tweening, tags, metadata | `references\animation.md` |
| Palettes, ramps, quantization, color modes | `references\palette.md` |
| Outlines, color replacement, dithering, HSL | `references\fx.md` |
| Native filters, convolution, brightness/contrast | `references\native_fx.md` |
| Moving, copying, erasing regions | `references\selection.md` |
| Flipping, rotating, scaling, cropping | `references\transform.md` |
| Bitmap/TrueType text | `references\text.md`; font setup in `references\fonts.md` |
| PNG/GIF/sheets, import, sprite copies | `references\export.md` |
| Individual/composited pixel reads | `references\pixel_read.md` |
| Onion-skin renders, frame diffs, color statistics | `references\analysis.md` |
| Animation validation and sanitization | `references\quality.md` |
| Slices, pivots, nine-patch regions | `references\slices.md` |
| Tilesets and tilemap layers | `references\tilemap.md` |
| Copying layers between sprites | `references\scene.md` |
| Raw Lua | `references\script.md`; checked execution in `references\execution.md` |
| Art/animation workflow, local previews | `references\workflow.md` |

Prefer explicit layer/frame-targeted operations (`*_at`), not separate active-state
setters: each operation launches a fresh Aseprite process.
Operations generally save immediately; copy valuable inputs before destructive edits.
Treat raw Lua and Python as unrestricted host code.

Bundled source is MIT-licensed; see `LICENSE` and `references\provenance.md`.
