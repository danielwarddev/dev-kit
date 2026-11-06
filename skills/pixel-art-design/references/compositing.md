# Compositing sprites onto existing scenery

These preview/compositing safeguards are additions owned by pixel-art-design,
not part of the companion Aseprite MCP-server port. Use that skill for program
operations and checked execution; keep practical art-preview guidance here.

Treat `Image:drawSprite` as a renderer, not a source-over compositor. In the
tested Aseprite workflow it replaces pixels in the sprite's destination rectangle,
including transparent canvas pixels. Drawing directly onto terrain can erase the
background; flattening that result produces black or white rectangles.

Render the frame into a fresh transparent image, then alpha-composite it:

```lua
local frame = Image(sourceSprite.width, sourceSprite.height)
frame:drawSprite(sourceSprite, frameNumber)
destinationImage:drawImage(frame, position, 255, BlendMode.NORMAL)
```

For task scripts, add this skill's actual `scripts` directory to the import path
and import `COMPOSITE_SPRITE_LUA` from `rendering`. Prepend it to a checked Lua
script, or use `runtime.execute`, which does that automatically. It defines
`composite_sprite(destination, source, frameNumber, position)` using the safe
sequence above. The module locates the sibling Aseprite execution tools relative
to its own file, independent of shell cwd.

The same `scripts\rendering.py` module provides `alpha_report`,
`assert_opaque_render`, and `assert_nearest_neighbor` for saved output checks.
Run its engine integration tests under `scripts\tests\test_rendering.py` with
`ASEPRITE_PATH` set. Setup and checked Lua invocation remain documented in the
Aseprite skill's `..\aseprite\references\execution.md`.

Preserve transparency in the source sprite; do not fill its empty canvas with
a background color to conceal a compositing error.

Verify both empty and partially transparent source pixels: empty regions must
leave the existing background unchanged, while translucent shadows blend with it.
A complete-ground scene or background-composited GIF preview should stay fully
opaque. Check the intermediate scene, not just an already-flattened comparison,
which can hide erased terrain behind an opaque rectangle.

Standalone sprites and unused atlas slots may intentionally have alpha.
Inspect native-size backgrounds and integer nearest-neighbor enlargements;
neither correct opacity nor exact scaling proves artistic quality.
