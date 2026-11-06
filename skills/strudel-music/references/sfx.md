# Sound effects and ambience

Infer the event, intended feeling, sound character, duration, and playback role
from the request/game. Choose an audible attack, useful body, and controlled
tail; a short UI click does not need musical layers or a SoundFont library.
Keep feedback distinct without making routine actions harsh or overpowering.
Use related variations for repeated footsteps, attacks, and impacts.

## Custom effects with Strudel

Use the existing `render.mjs` pipeline for original shaped synthesis, layered
noise/pitch effects, or explicitly registered licensed recordings. Select
envelopes, pitch movement, filtering and space for the event. Avoid long muddy
reverb on rapid feedback. Recorded realism requires suitable recordings, not
calling a sine wave a realistic sound.

Choose timing directly: `bpm`, `beatsPerCycle`, and `cycles` determine the active
duration, and fractional cycles are supported. Effects normally use `loop: false`
with enough `tailSeconds` to finish their decay. For example, 120 BPM, one beat
per cycle and 0.5 cycles gives 0.25 seconds of active sound before its tail.
Save the source/config, render, and use the existing technical checks.
Continuous ambience needs a natural loop and preceding effect history.

Design each effect for its event rather than reusing a stock sound. Repeated
events such as footsteps, attacks, and impacts can share one source with small
pitch, timing, or filter variations rendered as separate files. Never point the
output at existing game assets unless regeneration/replacement was requested.
Do not loop one-shot effects. Do not wire game playback without being asked.

## Validation

Use the renderer's `.validation.json` for invalid samples, clipping, unintended
silence, duration, and boundary jumps. Also check that one-shot tails decay
fully and loops have the intended flag and seam. Preserve intended rests and
genre-specific attacks; metrics cannot establish subjective feel. No external
audio upload or repeated user audition workflow is required.
