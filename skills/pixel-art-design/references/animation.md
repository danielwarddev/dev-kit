# Convincing pixel-art motion

Use the Aseprite skill for frame operations, timing metadata, tags, and exports.
This reference concerns whether the action reads and has believable weight.

## Plan the action before adding frames

Define the action, viewing direction, gameplay timing constraints, and the
important poses. Block these poses with readable silhouettes first. More frames
cannot repair an unclear attack or a walk without convincing contact.

For a walk, identify contact, compression/down, passing, and lift/up poses as
appropriate to the style. For an attack, establish preparation, the readable
strike/contact, and recovery. Exaggeration should clarify the action without
adding unwanted input delay or contradicting gameplay hit timing.

Maintain perspective across directions. Side, front, and back poses often require
different visible shapes and overlaps, not just shifted eyes on the same body.
Mirroring is appropriate only when equipment, lighting, and directional contracts
permit it.

## Block the force, not isolated body parts

For the key poses, identify the supporting contacts, approximate center of mass,
pelvis and shoulder orientation, head/gaze, and the direction of applied force.
Explain what initiates the action, where momentum goes, what gravity does during
unsupported motion, and how the body absorbs or redirects the remaining energy.
Use a rough whole-body drawing before detailed limb or weapon animation.

| Action phase | Required physical story |
|---|---|
| Load / anticipation | Weight shifts toward a usable support; knees and hips flex or the torso coils in preparation for the intended push. |
| Drive | Contact with the ground or another support supplies force. The pelvis and trunk participate; a wrist does not independently launch the entire attack. |
| Travel / contact | Mass moves along a readable path. Limbs support, steer, reach, or land in agreement with the torso and the action's direction. |
| Follow-through / braking | Momentum continues beyond the main effort. Supporting legs, trunk, and joints absorb or redirect it rather than freezing at the contact pose. |
| Settle | Residual motion diminishes toward balance. Head, equipment, and cloth may lag, but do not drift independently of their attachments. |

This is visual physical plausibility, not a demand for a dynamics simulator.
Stylization can exaggerate poses and timing, but it must keep a coherent cause
and effect. A frame count is chosen for the action, not a fixed three-frame
template. Spend prototype frames on distinct weight/force changes, not filler.

Do not treat layer translation as a complete pose change. Rotation, leaning,
compression, and extension alter the torso's contour, shoulder/hip relationship,
overlaps, and visible planes. Redraw those changes when necessary.

As a gate, hide feet, weapon, and effects temporarily: the pelvis, chest, shoulders,
and head should still visibly participate in the action. They need not identify
the action alone, but they must not remain a rigid cutout while the limbs perform it.

## Grounding and root motion

The critique reference explains a stance-phase comparison. Apply that test to
the current animation; it is not a complete walk loop or a fixed frame budget.

A supporting foot should not visibly slide across the ground during stance.
Measure this in world/ground space, accounting for character and camera movement:

- With root motion, compensate the stance leg as the body advances.
- In an in-place loop played on a moving character, the stance foot normally
  moves backward relative to the sprite origin. Locking it to the same local
  pixel in every frame can cause sliding in the game.
- Release the foot for the swing phase. A requirement to keep every foot fixed
  throughout a walk is incorrect.

Shift weight toward the supporting leg and let the body compress or rise for a
reason. Whole-sprite bobbing alone is not a walk. Inspect a moving runtime view
when available; a looping export on a static background cannot by itself prove
that root speed and foot travel agree.

## A walk is coordinated weight transfer

Use contact, compression, passing, and rise to describe the body's travel between
supports. The pelvis carries the trunk; the trunk and shoulders usually
counter-rotate against the pelvis/legs, while the head moderates that motion to
maintain the intended gaze. Magnitude and timing depend on the character's weight,
equipment, speed, and style, not a mandatory universal sway amount.

At landing, show absorption through ankle, knee, hip, and torso. During stance,
the body travels over the supporting foot. Near toe-off, extension pushes the
body toward the next support; the released foot follows a clear lifted swing arc
and returns under gravity. Do not teleport boots between arbitrary heights or
lengthen and shorten leg rectangles to reach them.

Keep segment lengths and attachment points coherent, allowing projected
foreshortening and intentional stylization. Resolve the hip, knee, ankle, heel,
and toe relationship before shading. A planted foot is necessary, not sufficient:
it can still accompany an implausible knee bend, floating pelvis, or rigid chest.
Dynamic balance can carry the center of mass beyond a support briefly; account
for momentum and the next contact rather than demanding static balance in every pose.

Arms and held equipment counterbalance or remain braced for a reason. A heavy
shield should not flap with the same motion as an empty hand. Small delayed cloth
motion should follow the body's acceleration, not an unrelated oscillation.

## A sword swing starts below the wrist

Choose a strike direction and a physically usable stance. Load the rear/supporting
leg and coil or draw back as appropriate. Show the drive through legs and pelvis,
then trunk, shoulder, elbow, and hand. The exact overlap varies by weapon and
technique, but the hand and tip must result from connected body poses rather than
independently chosen coordinates.

Carry the character's mass into or around the strike. Redraw torso rotation and
shoulder/hip separation; moving an unchanged torso one pixel while rotating a sword
is not a convincing attack. Keep the grip attached and the weapon rigid within
the intended perspective.

After the fast effort, show where the leftover angular/linear momentum goes.
The weapon and upper body continue into follow-through; the stance and joints
brake it, then recover and settle. A body that instantly snaps to neutral while
the sword continues moving breaks the force story. Slowing motion needs a visible
reason, such as contact, muscular braking, or a deliberate stop.

Use longer preparation/settling holds and tighter accelerating strike spacing
when appropriate. Evaluate the action with trails and impact effects disabled
first. An arc effect can reinforce an already readable swing, not rescue a
stationary-body weapon sweep.

## Arcs, volume, and equipment

Follow readable motion arcs for hands, feet, heads, and weapon tips. Check
attachments: a sword belongs to a hand, a shield follows its arm, and a cloak
hangs from a consistent attachment. Avoid moving accessories independently of
the anatomy that should carry them.

Preserve the volume of head, torso, and equipment between frames, allowing
intentional squash, stretch, or perspective changes. Do not let limbs grow and
shrink accidentally because their pixel rectangles were resized.

Use secondary motion only after the primary action works. Hair, fabric, and
equipment can lag and settle, but should not distract from the action or create
unnecessary work for a prototype.

## Timing and pixel stability

Inspect playback at intended timing, not only a sheet of equally spaced frames.
Vary holds and spacing when needed to communicate weight and intention.
Honor the game's action duration and feedback requirements; visual anticipation
must not silently change combat responsiveness.

Check the last-to-first transition of loops as carefully as adjacent frames.
Remove accidental jumps in position, size, lighting, or palette. Track useful
clusters across frames to avoid unintended glitter and crawling outlines.
Real shape changes and moving highlights can be intentional; do not freeze all
details merely to minimize frame differences.

For water, fire, foliage, and other environmental motion, preserve the base
structure and animate coherent masses or flow rather than independent random
pixels. Motion amplitude and contrast should suit the asset's visual priority.

## Verification

Inspect a sheet for pose/volume consistency and timed playback for the action.
First assess whole-body weight, drive, absorption, and settling without effects.
Look for foot teleporting, rubber limbs, a fixed trunk, or disconnected hand/tip
motion. If those fail, return to the poses instead of adding frames or polish.
Use onion-skin views or frame differences for a specific suspected defect, not
as proof of quality. Verify tags, durations, anchors, and frame layout separately
with the existing tooling.

Separate the visual judgment from the mechanical evidence. Correct durations,
unique frames, rigid attachments, and world-locked stance coordinates can all
pass while the motion remains awkward. Actual playback inspection is required,
and user feedback about feel takes precedence over those technical passes.

State whether grounding was checked in a runtime view or only in an illustrative
export. If runtime access is unavailable, report the root-speed matching limit
instead of claiming the walk is proven slide-free.
