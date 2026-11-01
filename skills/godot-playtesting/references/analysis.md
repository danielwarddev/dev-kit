# Context-efficient analysis and visual evidence

Store traces in files. Read a compact run summary first; query only the relevant
entity, event name, and tick range when investigating. Never paste hundreds of
frames, full pixel data, or an entire scene tree into model context.

Use existing shell/text tools for simple inspection. Write a small task-local
parser only when filtering/aggregation needs it. A persistent analysis CLI or
dependency is not required. Stream large JSONL files rather than loading all rows.
Parse and validate structured fields; do not use incidental string matches as
proof of an exact count or threshold.

## Summary contents

Include scenario/settings identity, capture completeness, outcome/termination,
failed/inconclusive checks, significant event counts, first relevant transitions,
and task-specific ranges/timing. Keep summaries bounded and point to raw evidence.
Include actual observations, not only a green status produced by the game.

For each finding, retain run ID, entity IDs, tick/sequence window, expected outcome,
observed outcome, tolerance, and any evidence gaps.
Use outcome labels such as "observed check passed", not "all tests passed".

## Reading a temporal failure

Inspect the causal chain:

1. Was the input/request issued?
2. Was it accepted, rejected, or blocked, and why?
3. Did the appropriate state transition occur?
4. Did movement/hit/other effect occur?
5. Was the result committed once and presented appropriately?

Position without input/collision context rarely explains a movement bug.
Damage totals without attack IDs may hide duplicate hits.
Correlated events do not prove causality; use reasons/IDs and implementation
inspection when the sequence is ambiguous.

## Sampling, aggregation, and anomaly windows

Record important discrete events regardless of state-sampling interval.
Use lower-rate routine samples where sufficient. Aggregate minima/maxima, counts,
displacement, durations, and threshold violations where they answer the question.
Do not let an average hide a one-tick spike or duplicate event.

For intermittent failures, a bounded high-detail ring buffer can preserve ticks
before an anomaly plus a bounded post-trigger window. Only add this complexity
when needed. Report overwritten history, dropped records, and the captured range.
Inspect full-detail windows instead of expanding the whole run's capture rate.

Sparse samples cannot establish that no brief overlap/hit/state occurred between
samples. Select events or a higher-resolution window for that claim.

## Screenshots, video, and human judgment

For responsiveness, pacing, or balance questions, read `feel-and-balance.md`.
Compare measurements against stated design goals and separate observed facts from
subjective interpretations. Never invent universal "good" thresholds.

Tie screenshots to named checkpoints or trace windows: scene entry, attack active,
collision, death, menu open, or a suspicious transition. Record the associated
run/tick and capture phase where available. If timing is approximate, say so;
do not claim a screenshot depicts an exact physics tick without synchronization.

Inspect images for visibility, layering, animation selection, camera framing,
UI/focus, and visual artifacts. Use video/frame sequences for continuity, pacing,
and transitions when available. Numerical state cannot prove that a sprite was
visible, its animation looked convincing, or the game felt good.

Human playtesting remains important for control feel, readability, fun, balance,
and emergent behavior. Profiling remains necessary for performance conclusions.
Tests remain necessary for repeatable regression coverage.

Report the scope of evidence honestly: what the trace establishes, what visual
evidence establishes, which tests ran, and what remains unverified. Follow the
project's reporting conventions; do not inflate an observation into total correctness.
