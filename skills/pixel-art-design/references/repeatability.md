# Repeatable decisions and reusable tooling

Repeatability means consistently briefing, constructing, inspecting, and revising
new artwork. This skill does not contain a catalogue of subject-specific recipes,
fixed geometry, generated sprites, or approved-image benchmarks.
Use the design, animation, terrain, and vehicle references as appropriate;
author each new subject rather than waiting for a matching generator.

## Read or bootstrap external preferences

Active profiles live under the project's `.claude\art-profiles`, outside skills.
The default is `project-style.json`. Read existing choices unchanged; discussion
or inspection alone does not create or modify a profile.

When absent, collect known pixel scale, camera/light, palette, hierarchy, and
delivery requirements from the request and existing assets. Keep uncertainty
provisional and establish one small representative sample before bulk production.

The bundled template holds only process guidance (detail, animation, delivery);
it has no visual-direction defaults. Supply these in the hints, inferred or
asked for as described in the skill's visual brief:

```json
{
  "pixel_scale": { "tile_size": 16, "character_reference": 32 },
  "camera": { "actors": "front_oblique", "ground": "top_down_plane" },
  "lighting": { "direction": "upper_left" },
  "outlines": { "actors": "...", "environment": "..." },
  "palettes": { "character": { "ink": "#RRGGBB" }, "terrain": { "ground": "#RRGGBB" } }
}
```

The values above only show the shape; they are not defaults. Palette groups can
use any names (for example `vehicles` or `ui`). Bootstrapping with any of these
sections missing or empty fails and writes nothing.

```powershell
python '.claude\skills\pixel-art-design\scripts\prototype_art.py' profile --project 'My prototype' --hints 'C:\temp\art-hints.json'
```

The command copies `references\style-profile-template.json`, merges the
JSON hints, and creates a **draft** only when missing. Existing values and custom
fields remain unchanged, even when hints are supplied. Invalid files error rather
than being replaced. Edit established preferences only from explicit user direction.

For a new project or unrelated genre, choose a separate external profile and,
when useful, a project-owned template rather than blindly inheriting another
project's choices:

```powershell
python '.claude\skills\pixel-art-design\scripts\prototype_art.py' --profile '.claude\art-profiles\genre-style.json' --template '.claude\art-profiles\genre-template.json' profile --hints 'C:\temp\genre-hints.json'
```

Templates are reusable starting preferences, not mandatory resolutions, palettes,
or geometry. Adapt provisional choices to the request. Copying the skill does
not copy active profiles. Default paths are resolved beside the installed
`.claude\skills` directory, independent of shell cwd.

## Author and inspect a new subject

Use `aseprite` for setup, drawing operations, and execution. Python 3.13+ and
the sibling Aseprite skill are sufficient for the helpers below; no additional
packages are required. Set `ASEPRITE_PATH` using its execution reference.

Write task-specific Python/Lua in session artifacts or an explicitly chosen
project output directory. Do not add its subject geometry to this skill.
The small `prototype_art.py` CLI manages profiles and validates images only;
it has no asset-generation commands.

`scripts\runtime.py` retains subject-independent helpers and imports this skill's
`scripts\rendering.py` compositing/validation additions. The companion Aseprite
skill remains the program-operation port; these additions do not live there.

| Need | Helper |
|---|---|
| Explicit, protected output directory | `prototype_art.prepare_output(path, overwrite)` |
| Checked Lua and safe sprite compositing | `execute`, `COMPOSITE_SPRITE_LUA` |
| Layered RGBA frames, timing, and tags | `save_master` |
| Native/enlarged exports and timed opaque clips | `export_frame`, `export_tag`, `neutral_clips` |
| Atlases and labelled pose comparisons | `save_sheet`, `contact_sheet` |
| Source layers and task-run provenance | `read_layers`, `record_run`, `write_json` |
| Transparency and exact integer scaling | `alpha_report`, `assert_opaque_render`, `assert_nearest_neighbor` |

Import only what the task needs, adding the actual skill `scripts` directory to
the task's import path. Frame pixel maps, layer names, sizes, timing, tags,
and selected poses come from the new design, not a prescribed character rig.
Use `prepare_output` before writing to reject nonempty destinations unless
explicit overwrite is intended. It never deletes unrelated files.
Keep source art outside its output directory and preserve editable masters.

The compositor renders a sprite onto a fresh transparent image before source-over
blending it onto existing scenery. Direct `Image:drawSprite` onto populated scenery
can erase terrain beneath transparent canvas pixels; do not substitute it.
Preserve source alpha and check background pixels as well as visible art/shadows.
Read `references\compositing.md` for the safe sequence and direct helper imports.

`record_run` writes external profile snapshots, operation identifiers, parameters,
Python/Aseprite versions, and task/helper/input hashes. Pass task code and relevant
dependencies, including any seed. This supports reproducing a particular output;
it does not turn that subject's drawing code into a reusable design requirement.

## Mechanical checks and visual judgment

```powershell
python '.claude\skills\pixel-art-design\scripts\prototype_art.py' validate 'C:\temp\scene-preview.png' --opaque --scale-source 'C:\temp\scene-native.png' --scale 4
python '.claude\skills\pixel-art-design\scripts\prototype_art.py' validate 'C:\temp\new-sprite.png'
python -B -m unittest discover -s '.claude\skills\pixel-art-design\scripts\tests' -p 'test_*.py'
python -B -m unittest discover -s '.claude\skills\aseprite\scripts\tests' -p 'test_*.py'
```

Standalone sprites and unused atlas slots may intentionally be transparent.
Only complete-background previews should be fully opaque. A flattened opaque
scene can conceal erased terrain, so opacity alone does not prove good compositing.

The design skill's rendering integration tests require `ASEPRITE_PATH`; without
it they explicitly skip. They verify background preservation, partial-alpha
blending, complete-scene opacity, and exact scaling. Generic tests cover profile
protection, safe output, and provenance for task code outside the skill. The
companion skill's existing tests validate the port's execution behavior.

Passing checks does not establish appealing art or motion. Inspect native scenes,
enlarged pixels, body-only poses, timed playback, and transitions. Return to the
design/force/terrain gates when those fail. Report technical evidence separately
from visual judgment.

## Keep the skill source-only

Keep subject-specific generators and their tests, generated PNGs/GIFs/atlases,
Aseprite masters, previews, output metadata, active profiles, logs, and caches
outside skill folders. The helpers reject skill-contained generated destinations.
The CLI disables bytecode; use `python -B` for tests and ad-hoc imports.
Saving or using the skill does not stage or commit anything.
