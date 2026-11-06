# Visual critique and correction patterns

Inspect actual task-specific previews outside the skill with the image-view tool.
An image filename or successful export is not evidence of visual review.
The skill stores correction patterns, not generated artwork or an approved-image
gallery. These patterns isolate defects, not a finished-art style.

## Static failure/correction patterns

| Area | Defect and visible consequence | Targeted correction | What not to infer |
|---|---|---|---|
| Silhouette | An upright weapon touches the arm/body contour, losing its identity in the solid silhouette. | Move the blade outward and leave a clear gap above the grip; preserve the intentional hand attachment. | Every limb and accessory must be detached, or every character needs this pose. |
| Helmet volume | Nested central highlights create pillow shading and an inflated badge-like surface. | Group the bright light-facing plane, middle plane, and darker turn under the selected light direction. | Every helmet should share these proportions or have exactly this ramp. |
| Cloth highlights | Scattered bright marks fragment the cloak and imply sparkling surface noise. | Replace scattered accents with a few connected folds following the cloak's form. | Isolated pixels are always wrong, or cloth can never have a sharp highlight. |

Apply the correction to the current subject and inspect a before/after at equal
native/display scale. Do not copy a fixed silhouette or pixel-coordinate recipe.

## Stance-phase grounding comparison

During stance, compare a ground landmark with the supporting foot while the body
advances. If the foot advances with the torso while remaining in contact, it
slides. Correct the connected hip/knee/ankle pose so the supporting foot stays
with its ground contact as the body travels. Release it for the swing phase.
Inspect timed playback as well: a static contact diagram cannot establish good
walking, and the whole-body force story remains a separate gate.

Judge real animation in ground/world space, not by requiring a constant
sprite-local foot coordinate. See `references\animation.md` for root-motion
and in-place-loop distinctions.

## Review the current asset

Start with the visual brief and selected reference. Inspect:

1. Native/intended display size: subject, gameplay role, equipment, and focal
   features are readable without relying on an enlarged preview.
2. Silhouette and major values: important forms separate and the pose is
   recognizable; use solid/grayscale views when a specific ambiguity warrants them.
3. Enlarged pixels: contours, clusters, overlaps, lighting, and material cues
   look intentional, without accidental tangencies, noise, or fuzzy halos.
4. Context: representative light/dark or textured terrain and neighboring assets
   reveal weak contrast, inconsistent scale, perspective, or outline weight.
5. Motion when applicable: pelvis, trunk, shoulders, and head participate in
   coordinated load, drive, travel, absorption, and settling. Inspect with weapon
   trails/effects hidden, then check timed playback, loop boundaries, and available
   runtime grounding evidence. Slide-free feet alone do not satisfy this gate.

Do not declare a named defect solely because a heuristic fires. Some touching
forms, lone pixels, hard highlights, or irregular edges are deliberate.

## Turn observations into revisions

Use a short, evidence-based diagnosis:

> The bow merges into the torso at intended size, so the archer reads like the
> unarmed character. Shift the bow outward, open a gap above the grip, and
> recheck the silhouette against the forest background.

Prioritize high-impact construction defects before polish:

| Priority | Question | Typical correction |
|---|---|---|
| Identity and pose | Is this the intended subject/role, and is the action legible? | Change silhouette, proportions, equipment scale, or stance. |
| Separation and form | Do masses overlap clearly and feel dimensional? | Regroup values, clarify front/back order, or move shading onto real planes. |
| Consistency and context | Does it belong to the set and read in the game? | Align perspective, scale, lighting, outlines, or terrain contrast. |
| Whole-body action | Do force, weight transfer, gravity, and leftover momentum explain the poses? | Reblock supports, pelvis/trunk rotation, connected limb paths, braking, and settling before adding effects. |
| Pixel polish | Do residual marks interfere with otherwise sound construction? | Clean contour steps, stray marks, and fragmented highlights. |

Choose the largest one or two defects, make a bounded correction, and inspect a
new export. Compare before/after at the same scale and background. Verify that
the defect improved and that the correction did not erase identity or introduce
a different failure. Avoid endless undirected polishing or expanding the scope.

## Completion and limits

The applicable gates in `SKILL.md` must be satisfied for the intended use.
A prototype can have simplified detail and motion, but still needs clear identity,
coherent form, asset-family consistency, and understandable contact.

Technical checks may verify dimensions, palette constraints, transparency,
cel coverage, tags, durations, or bounds. They can also flag suspicious isolated
pixels or excess colors for review. They cannot prove appealing art, good anatomy,
convincing materials, readable action, or consistent style.

Report visual evidence separately from technical checks. When actual-scale,
background, reference, or runtime inspection is unavailable, say which judgment
remains unverified. Do not replace missing evidence with a numerical beauty score.
