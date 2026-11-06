# Sprite construction and art direction

## Reference analysis

Choose a small number of references with comparable perspective, subject scale,
and intended presentation. A detailed 128-pixel character is not direct evidence
for what works on a 16-pixel character. View the references at actual gameplay
scale as well as enlarged.

Explain the useful decisions in each reference:

- Which silhouette feature makes the subject recognizable?
- How are head, torso, limbs, and equipment proportioned?
- Where are the largest value masses and strongest contrast?
- Which contour changes, overlaps, and shaded planes imply volume?
- Which few marks communicate material, and what details are omitted?

Prefer an approved project sample as the consistency reference. If none exists,
establish a representative sample rather than inventing a different visual style
for every asset. The critique reference describes individual defects and targeted
corrections; these descriptions are not finished-art benchmarks.

## Perspective, proportions, and silhouette

Choose a camera convention first: side view, overhead, or an oblique view with
visible top and front surfaces. Keep the amount of visible top surface consistent
across heads, shoulders, containers, buildings, and terrain.

Build the subject from its important masses and pose. A character's head-to-body
ratio, shoulder width, stance, and equipment size should be intentional. Small
sprites may need larger heads, hands, or weapons for identity and readability.
Do not impose realistic anatomy where the chosen style requires exaggeration,
but keep joints, attachments, balance, and overlaps believable within that style.

Test the solid silhouette. Look for a characteristic shape, a stable stance, and
useful gaps between the torso, arms, legs, and equipment. A bow or staff that
merges with the torso may need to move, enlarge, or change pose rather than gain
more internal detail. Distinct gameplay roles should differ in silhouette or
large shape language, not depend entirely on tiny accessories or recolors.

Avoid accidental tangencies: contours that just touch can make separate forms
look attached. Use a clear overlap with an understandable front/back order or a
clear gap. Deliberate asymmetry can give a pose life; mirror only what should
actually be symmetric.

## Value, palette, and focal hierarchy

Block the major light, middle, and dark regions before texture. Check grayscale
when two differently colored regions still seem to merge. Similar hue is not
the problem if value separates them; different hue is not a solution if value
does not.

Keep the strongest useful contrast around defining features such as the face,
weapon, or interaction point. Quiet broad clothing and terrain masses give those
features room to read. Do not give every edge, pebble, or seam equal contrast.
Assess saturation and value against actual backgrounds, not only a neutral canvas.

Use a compact palette appropriate to the style, without a universal color limit.
Reuse compatible ramps across related assets. A ramp should have useful value
steps; small hue and saturation shifts can keep shadows and highlights lively
when consistent with the lighting. Do not automatically add white to every
highlight or black to every shadow.

An outline is a style choice. A full outline, selective outline, colored outline,
or no outline can each work. Apply the choice consistently and keep its effective
thickness appropriate to native resolution. Do not use an automatic outline to
hide weak construction.

## Form, light, and contact

Choose a light direction and shade the actual surfaces. A rounded helmet should
turn gradually through a small number of designed clusters; a box has distinct
planes; a fabric fold has a directional ridge and recess. A light patch belongs
where the surface faces the light, not automatically at the center.

Avoid pillow shading: nested contours with bright centers and dark borders
regardless of the object's form or light source. Also avoid beveling every
component identically. Neither more ramp colors nor a smooth gradient fixes the
underlying construction.

Use overlap shadows sparingly to establish where a head sits above a collar, a
shield covers an arm, or a lid projects over a container. Darken the actual
occlusion, not every seam. A ground shadow should support the selected perspective
and contact, not become a dark sticker that dominates the sprite.

Feet, wheels, trunks, and building bases should meet the intended ground plane.
Check the sprite's actual game anchor; transparent padding and a visual shadow
do not guarantee correct placement.

## Pixel clusters and contour craft

Think in connected shapes of pixels, each describing a surface, edge, shadow, or
accent. Refine the silhouette's curves and diagonal stair-steps intentionally.
Regular slopes usually benefit from a coherent step rhythm; organic contours
can vary when that variation describes the form. Do not flatten every organic
edge into mathematically uniform stairs.

Remove accidental bumps, doubled edges, narrow holes, and highlight fragments
that interrupt a surface. A lone pixel may be exactly right for an eye, glint,
or leaf tip; remove meaningless isolated marks, not all isolated pixels.

Use crisp pixel placement at native resolution. Integer nearest-neighbor scaling
is for presentation and inspection, not a way to add working resolution. Do not
blur or downsample a large painting and assume the result is intentional pixel art.

Manual anti-aliasing and dithering are optional, style-dependent techniques.
Anti-aliasing should clarify a contour without introducing a fuzzy halo on other
backgrounds. Dithering should solve a deliberate transition or texture problem;
at small sizes it often produces noise. Neither technique is a mandatory polish
step, and indiscriminate gradients are not a replacement for designed clusters.

## Material shorthand

Use only the cues the resolution can support:

| Material | Useful cues | Frequent failure |
|---|---|---|
| Metal | Sharp selective highlights, dark/light adjacency, planar or curved reflections | Giving the entire object an equally bright rim |
| Cloth | Broad quiet masses, a few folds tied to tension and gravity | Sparkly highlights or evenly spaced decorative stripes |
| Leather/wood | Restrained surface variation, seams or grain following construction | Random speckling that overwhelms shape |
| Stone | Readable planes, sparse cracks and chips related to form | Outlining every tiny fragment equally |
| Fur/foliage | Grouped tufts or leaf masses, selective contour breaks | Drawing each hair or leaf as unrelated noise |
| Glass/liquid | Consistent rim, reflection, fill level, or controlled transparency | Low-opacity clutter that disappears against terrain |

Keep material distinctions visible at intended size. If removing the texture
makes the sprite unreadable, fix its major masses before restoring texture.

## Asset families, terrain, and props

Compare related assets on a contact sheet at the same gameplay scale. Check
perspective, body/prop scale, outline weight, palette, lighting, and contrast.
Preserve consistent size for comparable objects unless variation is intentional.

Characters and interactable props generally need stronger focal separation than
background texture, but follow the game's actual visual hierarchy. Terrain
should support gameplay readability rather than compete with every character.

Preview tiles in repeated patches, including relevant corners, borders, and
transitions. Check seams and unwanted repeated lines or motifs across several
tiles, not just one isolated tile. Construct variants from the same underlying
material and lighting; random noise is not a convincing variation system.

Props should have a characteristic silhouette, visible construction, and a
believable base/contact plane. A chest needs readable lid/body separation before
wood grain; a shrine needs its major structure before ornamental marks.

For terrain construction, material transitions, repeat checks, and environmental
scale/contact, read `references\terrain.md`.
