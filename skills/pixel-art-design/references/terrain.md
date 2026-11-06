# Terrain that reads as a place

Terrain supports the actors and routes, rather than competing with every sprite.
Keep the project's native tile size and pixel density. A new environmental prop
may need a larger transparent canvas at the same pixel scale; do not quietly
stretch the ground or change existing tile/atlas contracts.

## Plan the material and visual hierarchy

Specify the biome, camera convention, light direction, ground palette, navigable
routes, and relevant material contacts. Establish one small representative scene
before generating every biome or a large tile library.

Use compatible color families across terrain and sprites, but reserve the
strongest useful contrast for actors and important objects. Background texture
usually needs quieter value steps and lower accent density than a character.
Test this with an unchanged actor on the terrain; do not recolor or outline the
actor merely to compensate for distracting ground.

Design at three scales:

| Scale | Purpose |
|---|---|
| Large masses | Routes, clearings, banks, forest edges, water bodies, and readable space for actors. |
| Medium clusters | Moss patches, grouped blades/leaves, soil wear, stones, ripples, and canopy lobes that describe the material. |
| Small accents | A few cracks, grass tips, bark marks, glints, or pebbles that clarify an already readable surface. |

Do not jump directly to random single-pixel speckle. More noise can make a flat
tile busier without making it feel like grass, earth, or stone. Use connected
clusters and restrained contrasts; omit accents that are lost at intended size.

## Material construction

- Grass/forest floor: group growth into patches and tufts with quiet soil/moss
  showing between them. Vary cluster shape and placement, not every pixel's color.
- Dirt: show broad wear, compacted areas, and a few directional ruts or stones.
  Avoid an equally spaced checkerboard of tiny pebbles.
- Water: establish depth and the surface plane first. Use coherent ripples or
  reflections; a repeated stack of bright horizontal stripes looks diagrammatic.
- Rock: separate a small number of planes with consistent lighting. Chips and
  cracks belong to the structure, not random speckling.
- Foliage: group leaves into dimensional masses with a readable crown/trunk
  silhouette. Shade the lit tops and overlapping undersides, not concentric
  rings around the center of every tree.

The terrain need not have a black outline around each tile or texture cluster.
Use actual occlusion and material edges where helpful, with treatment consistent
with the set's hierarchy. Never outline the square tile boundary as decoration.

## Transitions describe physical contact

Plan how materials meet before detailing their centers. Paths need readable
edges, bends, junctions, and ends. Shorelines need a land/water relationship,
including applicable bank, exposed soil, shallows, and occlusion.

Use convex and concave corners as well as straight edges. Avoid a pond assembled
from hard blue squares or a path that changes color only inside a rectangular
tile. Shared edge crossings and compatible silhouettes should continue through
adjacent tiles. If using corner masks, document their bit order and the treatment
of ambiguous diagonal cases; do not assume the engine's autotile convention.

Keep shading physically consistent. A bank's illuminated lip and darker side
depend on form and light direction; outlining the entire shore with the same
bright rim is another form of pillow shading. Water can have a quieter shallow
band without a fluorescent outline.

Do not silently invent unsupported junctions. Either supply the needed material
combinations or compose the prototype with a deliberate separator and disclose
what the sample does not include.

## Repetition and variation

Construct variants from the same base material, lighting, scale, and compatible
edge structure. Do not make one tile significantly brighter, noisier, or more
contrasty than its neighbors unless that variation has a scene-level purpose.

Inspect both a repeated single-tile patch and a mixed-variant patch over several
tiles. Look for obvious grids, texture-free border gutters, repeated bright
diagonals, and recognizable motifs recurring every tile. Wrap selected clusters
across edges or use compatible edge profiles; do not leave a conspicuously blank
perimeter around every tile just to make color checks pass.

Validate connections, not a universal rule that all adjacent edge pixels must
have identical RGB values. Legitimate texture and shading can change at an edge.
Material topology and intended cluster continuation should agree, and the actual
patch should not expose an unintended seam.

## Props, contact, and scale

Trees should have a supported crown, attached branches, a readable trunk, and a
base/root contact. Rock and shrub masses should sit on the same ground plane.
Use cast/contact shadows consistent with the light and terrain, not dark stickers
that dominate each object.

Compare environmental dimensions with the actor at the same pixel density.
Deliberate stylization is valid, but a tree should not accidentally look like a
small bush because its canopy and trunk were compressed into an arbitrary canvas.
Use larger source dimensions when appropriate for a standalone sample, preserving
the ground anchor and clearly identifying any integration change.

Compose and depth-sort props from their intended ground positions. Check whether
large canopies hide the actor, routes, or interaction points. Keep the prototype's
main route readable; density and decoration are not substitutes for composition.

## Buildings and constructed decoration

Block the footprint, front/side walls, roof or broken upper structure, and entrance
before adding grain, masonry, or ornament. Keep roof pitch, wall axes, windows,
and the ground anchor in the same camera convention. A building needs readable
planes and supported structure, not just a decorated rectangle.

Compare doors and clearances with the actor at native pixel density. Let larger
props use larger canvases rather than shrinking them to the character's dimensions.
Plan routes to the entrance, not through an opaque wall; depth-sort from ground
positions and inspect whether foreground structures hide the actors.

Wood/hide/thatch should describe construction and wear with grouped marks following
the surface. Stone courses, recessed openings, eaves, and exposed broken edges
should explain depth; do not outline every brick at equal contrast. A ruined
variant changes its silhouette and structure, not merely its color.

## Review and deliver

Inspect native/intended-size scenes and nearest-neighbor enlargements. Review
material patches and transition corners before the complete scene, then compare
the scene against the chosen direction and baseline at equal scale.

Name the biggest defect and make a targeted correction: for example, a repeated
bright grass motif, a rectangular shore, an unsupported tree crown, or a path
whose contrast overwhelms the actor. Reinspect the revised export rather than
assuming a larger palette or more texture improved it.

Verify dimensions, atlas layout, mask/neighbor conventions, transparency, and
anchors separately. State the prototype's material and integration limits.
Successful seam or metadata checks do not prove a convincing environment.

Check empty space inside actor/prop rectangles as well as the visible artwork.
Transparent sprite pixels must preserve the terrain or earlier props underneath;
partial-alpha shadows must blend with them. A complete-ground preview should be
fully opaque, but an opaque comparison can still conceal erased terrain behind
black/white boxes. Inspect the intermediate scene and background preservation,
not only opaque actor pixels. Keep source sprites transparent; use the Aseprite
operations with this skill's `references\compositing.md` safeguards rather than
filling their canvases with a solid color.
