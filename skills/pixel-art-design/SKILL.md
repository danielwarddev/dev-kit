---
name: pixel-art-design
description: 'Use when designing, creating, improving, or visually reviewing pixel-art sprites, characters, props, terrain tiles, palettes, or animations. Teaches silhouette, proportions, form, lighting, pixel clusters, materials, consistency, and staged visual critique. Pair with aseprite for drawing and export operations. Not for tool-only conversions, sprite metadata, or game-code integration.'
---

# Pixel-art design

Aim for convincing, intentionally drawn game art, not photographic realism or
maximum detail. At small resolutions, useful exaggeration and selective omission
often communicate more than literal proportions or extra shading.

This skill owns visual decisions, judgment, and practical preview safeguards.
Use `aseprite` for file operations, drawing, animation setup, and exports; do not
duplicate its ported operations here. Compositing and validation helpers belong
in this skill, not in the companion Aseprite port.
Respect the user's scope: discussion or critique does not authorize asset edits.

## Start here

1. When creating or improving assets, read the appropriate profile under the
   project's `.claude\art-profiles\` directory, outside the skill. The default is
   `project-style.json`; independent work in another genre can use its own
   profile file. If absent, bootstrap a draft using the request and known project
   constraints as described in `references\repeatability.md`. Reuse existing
   choices; never silently replace the profile. Discussion/inspection alone does
   not create or change it.
2. Read `references\design.md` before creating or substantially redrawing art.
3. Read `references\critique.md` and apply its failure/correction checklist.
   Read `references\animation.md` as well when making or changing motion.
   For terrain, tile sets, or environmental props, also read `references\terrain.md`.
   For vehicles or driving scenes, also read `references\vehicles.md`.
   When placing sprites over backgrounds, read `references\compositing.md`.
4. Inspect relevant existing assets and their intended backgrounds. Preserve
   technical contracts such as frame size, atlas layout, transparency, anchors,
   and animation tags unless changing them is explicitly in scope.

Resolve all reference and tooling paths from this skill's own directory.
Read actual images, not just filenames, drawing scripts, or color statistics.

## Establish a small visual brief

Keep project-specific prototype preferences in `.claude\art-profiles`, not in
the reusable skill or a project-level design document. Before drawing, state asset-specific decisions in
the task response or session artifacts:

- Subject, gameplay role, and one or two defining visual features.
- Native resolution, intended on-screen size, camera perspective, and ground anchor.
- Proportions, outline treatment, lighting direction, palette/value groups, and
  detail density consistent with the intended asset family.
- Reference images, what principles to borrow, and representative backgrounds
  against which the sprite must remain readable.

Use existing project direction when it satisfies the request. Existing placeholder
quality is not a quality ceiling.

There are no default visual-direction choices. Pixel scale (tile size and
character size), camera perspective, light direction, outline treatment, and
palette each come from, in order:

1. The active profile, when it already records the choice.
2. Inference from the request (named colors, mood, setting, time of day, era,
   genre conventions, target hardware or resolution), existing project assets, or
   supplied references. Tell the user each choice you inferred and what it was
   based on, so they can correct it; inferred choices stay provisional.
3. Otherwise, ask the user before drawing, batching the open choices into one
   question. Do not fall back to a generic default or carry choices over from
   another project.

The profile starts as a draft, not an automatically approved direction. Use its
provisional choices for the first small sample; update established preferences
from explicit user direction, not incidental variation in a new asset. Copying
the skill does not copy project preferences. For a new project or unrelated genre,
establish a separate draft profile rather than blindly inheriting the old one.
When prior artwork is excluded, use general principles/tooling only; do not load
old art or archived generators as visual references.
Use local, original, or appropriately licensed references and record their origin.
Study principles without copying a recognizable protected character or composition.

## Draw in stages, with gates

| Stage | Required judgment before proceeding |
|---|---|
| Silhouette | Subject and defining feature read at intended size; equipment and limbs do not merge accidentally. For a new design, compare two or three inexpensive shape/pose options before committing. |
| Major values | Light, middle, and dark masses separate the important forms without relying only on hue. These are a planning aid, not a mandatory three-color limit. |
| Color and form | Proportions and perspective agree; lighting follows the surfaces; important overlaps and contact points are understandable. |
| Selective detail | Highlights, texture, and accents describe form/material or gameplay identity. Remove marks that only add noise. |
| Context and motion | Sprite reads against representative terrain and beside neighboring assets. Animated poses show coordinated weight transfer, force, and settling through the whole body, not independent moving limbs on a rigid torso. Check timing and grounding as separate requirements. |

Use simple shapes to block ideas, not as an obligation to retain rectangular
torsos, elliptical heads, concentric shading, or identical recolored poses.
Refine the contour and interior pixel clusters to describe the actual subject.
Do not compensate for a failed silhouette or value gate by adding texture.

For a new asset family, establish one representative sample for each relevant
category, such as character, prop, and terrain, before bulk production. Inspect
them together and use the user's review if requested; do not multiply an unproven
visual recipe across the entire set.

## Inspect, diagnose, revise

Inspect both native/intended-size views and integer nearest-neighbor enlargements.
Use the first for readability and the second for deliberate pixel placement.
Check silhouette and grayscale views when they help resolve a specific question.
Compare with the selected references at equivalent subject/display scale.

Name the largest visible defect, its consequence, and a targeted correction.
Prioritize silhouette, proportions, value separation, and form before pixel polish.
Make a bounded revision, export again, and verify that the defect improved without
breaking another gate. Do not merely describe a fix or repeat "looks good."

Stop when the intended use and applicable gates are satisfied. If a gate remains
unmet, state the limitation rather than claiming finished quality. Prototype scope
can omit ornamental detail and secondary motion, not basic readability or grounding.
No palette count, successful command, or automated audit proves artistic quality.
Likewise, correct frame timing and slide-free feet do not prove convincing motion.
Reject an awkward whole-body action before polishing its pixels or adding effects.

Preserve editable masters and existing assets. Report what changed, what was
visually inspected, and any material limitation; separate technical validation
from the visual judgment.

## Reusable tooling and new subjects

Read `references\repeatability.md` for external profiles, shared rendering/export
helpers, and subject-independent checks. The skill contains no subject-specific
generators, fixed pixel patterns, or example sprites.

For a new subject, establish its brief, block original silhouettes/poses, draw
with the Aseprite operations, and inspect/revise through the gates above.
No pre-existing recipe is required. Keep task-specific Python/Lua in session
artifacts or the user's chosen project output, not in the reusable skill.
Repeatability means applying the same decision and review process to new art,
not regenerating a previous sprite or forcing a new subject into an old rig.

Do not create an approved-image gallery or promote generated examples to
approved benchmarks unless requested.

## Keep the skill source-only

Store generated PNGs/GIFs/atlases/Aseprite masters, previews, run metadata, and
temporary checks outside skill folders. Active style profiles live under
`.claude\art-profiles`; reusable templates remain in `references`.
Do not bundle generated illustrations or cached bytecode with the skill.
The shared helpers reject skill-contained output/profile paths; the CLI suppresses bytecode.
Use `python -B` for tests or ad-hoc imports. No commit or staging operation
is part of this workflow.
