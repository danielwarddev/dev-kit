# Responsiveness, pacing, balance, and visual limits

Monitoring is still ONLY an aid to agent runtime observation. It can measure
properties that influence feel and support design analysis, not independently
prove enjoyment or replace tests, screenshots, video, profiling, or playtesting.

## Useful measurements

| Question | Runtime measurements | Judgment still needed |
|---|---|---|
| Enemy durability | Time-to-kill, hits required, attack downtime | Satisfying challenge versus tedium |
| Player survivability | Time-to-death, damage sources, burst size, warning-to-impact interval | Fairness and intended difficulty |
| Jumping | Height above takeoff, horizontal range, airtime, apex time, input-to-takeoff delay | Responsiveness and natural-feeling motion |
| Movement | Acceleration, stopping distance, reversal delay, speed consistency | Precision versus intentional momentum |
| Feedback | Hit-to-animation/sound/effect request delay, duration, restart frequency | Readability and impact |
| Pacing | Time between meaningful actions, encounter length, recovery/travel downtime | Tension, engagement, and fatigue |
| Camera | Follow delay, positional error, settling time, abrupt changes | Comfort and useful framing |

Measure only what matters to the current design question. Do not add a permanent
analytics system, simulate huge player populations, or record every value.

## Define the measurement

Time-to-kill needs a declared start: encounter entry, first attack, or first damage
can yield different results. State the enemy/player configuration, strategy, misses,
invulnerability, interruptions, and end event. Preserve surviving/timed-out cases;
do not silently exclude them from a successful-kills average.

Time-to-death depends on health, equipment, behavior, healing, enemy count, and
damage pattern. A stationary scripted player does not represent a skilled human.
Label controlled scenarios as such rather than extrapolating to all players.

Jump height is relative to takeoff, not an arbitrary global coordinate. Define
coordinate direction, landing elevation, held/released input, moving platforms,
and whether range means horizontal displacement or traveled path.
Sparse samples can miss the apex; choose adequate capture or report uncertainty.

Distinguish input request, acceptance, simulation effect, and presented feedback.
A logged animation/sound request is not proof that the player saw/heard it.
Physics time alone cannot establish end-to-end input/display latency.
Use presentation evidence or appropriate measurement tools for that claim.

## Evaluate against intent, not invented universal thresholds

Use explicit design goals, user requirements, or an approved baseline. Examples:
basic enemies take two to three successful hits; a jump clears a three-tile gap;
the intended warning allows the specified reaction window.
These examples are not defaults to impose on every game.

If no target exists, describe measurements and tradeoffs as observations/hypotheses.
Ask for a design decision when selecting a target would materially alter gameplay.
Do not label an arbitrary number good/bad or change balance solely to improve a metric.
Long time-to-kill may suit a boss; short survival may be intentional.
Do not optimize every delay, difficulty measure, or downtime toward zero.

Agents may flag likely issues such as accepted inputs with delayed effects,
unintended jump inconsistency, repetitive idle periods, or damage occurring before
its warning. Explain evidence, assumptions, and alternative interpretations.
Warning intervals alone do not prove attacks are avoidable or fair; position,
available actions, readability, and human reaction also matter.

## Comparisons and recommendations

Compare equivalent scenarios and settings before/after a change. Use several runs
when randomness/strategy varies; retain sample count, range, meaningful outliers,
and incomplete cases. Avoid averages that hide rare lethal bursts or latency spikes.
Do not infer causality from correlation or generalize a scripted strategy to players.

Separate the measured finding, design interpretation, and suggested change:

```text
Measured: this scenario required 7 successful hits; target is 2-3.
Interpretation: common-enemy durability exceeds the stated design goal.
Recommendation: review health/damage values, then rerun and visually playtest.
Limit: this does not establish whether the revised combat feels satisfying.
```

Only implement tuning within the user's authorized scope. Preserve tests for
behavioral regressions and do not turn subjective preferences into correctness
assertions unless they become explicit measurable requirements.

## Looks and human assessment

Telemetry can reveal wrong animation selection, restart loops, delayed effects,
camera lag, or visibility-state changes. It cannot assess palette harmony,
silhouette readability, convincing motion, attractive composition, or fun by itself.
Use screenshots for appearance and video/frame sequences for continuity and pacing.

Humans retain the final judgment on taste, control feel, enjoyment, fairness,
and the intended experience. The agent can measure, inspect, compare, and recommend;
report remaining subjective uncertainty rather than declaring that the game
"feels good" from a clean trace.
