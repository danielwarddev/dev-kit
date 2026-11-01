# Portable evidence contract

Use structured text such as UTF-8 JSON Lines when introducing a recorder.
One complete JSON object per line supports streaming and selective querying.
Adapt existing structured output rather than imposing this format unnecessarily.
Plain targeted logs can suffice for a small investigation.

## Common envelope

For a new structured trace, standardize the envelope, not every game's payload:

| Field | Meaning |
|---|---|
| `schema_version` | Explicit format version; start at 1 |
| `run_id` | Unique capture identity; never mix different runs |
| `seq` | Monotonically increasing record order within the run |
| `kind` | `run_start`, `event`, `sample`, `check`, or `run_end` |
| `tick` | Agreed run-relative simulation/physics tick, or null when inapplicable |
| `sim_time_s` | Simulation time under the declared convention, or null |
| `entity_id` | Stable run-local entity identity, or null for run-level records |
| `name` | Specific event/sample/check name |
| `data` | Small JSON payload containing task-relevant evidence |

Do not invent tick/time values for events outside simulation. Record their time
domain/phase explicitly in `data`; `seq` orders observations, not engine causality.
If multiple producers record concurrently, define ordering rather than assuming it.

Example observation, not a universal combat API:

```json
{"schema_version":1,"run_id":"combat-001","seq":42,"kind":"event","tick":81,"sim_time_s":1.35,"entity_id":"enemy:3","name":"combat.damage_applied","data":{"attack_id":"attack:7","source_id":"player:1","health_before":10,"damage":3,"health_after":7,"phase":"after_damage"}}
```

Entity IDs must distinguish spawned instances and remain stable during the run.
Names/node paths alone may repeat or change; retain a descriptive label/path as
optional metadata, not as the only identity. Record spawn/despawn when relevant.
Attack/action IDs allow duplicate effects to be distinguished from new actions.

## Run metadata and completeness

`run_start` should identify the scenario, relevant starting state, seed(s), engine
version, code revision when available, tick rate, time scale, observation phase,
sampling rates, trace limits, and rendering/headless mode. Include only settings
that affect the interpretation. A seed is not a determinism guarantee.

Declare whether tick/time is run-relative or engine-relative, when it starts, and
whether time advances during pause. Prefer accumulated simulation delta for
simulation elapsed time; tick/rate conversion only holds under its stated settings.
Record rate/time-scale/pause changes when they matter.
Rendered frames and wall time are separate optional fields, not substitutes.

`run_end` should report termination reason, observed range, record/drop counts,
limit hits, output errors, and check statuses. A missing end record, invalid line,
dropped relevant range, or incomplete scenario must be identified by the reader.
Do not silently skip malformed JSON or summarize a partial trace as a pass.

## Three evidence types

**Events:** record inputs, state transitions with reasons, collisions, damage,
spawn/despawn, animation transitions, and other discrete outcomes relevant to the
question. Distinguish requests from accepted actions and committed effects.

**Samples:** record small state snapshots such as position, velocity, health,
target ID/distance, AI state, or camera position. Declare units and coordinate
space (local/global/screen; pixels or world units), and whether values are requested
or actual. Use explicit JSON arrays/objects for vectors, not ambiguous strings.
Encode unsupported values deliberately; flag invalid/non-finite measurements
instead of silently replacing them with zero/null.

**Checks:** record the expectation, measurement/window, tolerance, evidence basis,
and result. Status is `pass`, `fail`, or `inconclusive`. These are scenario
observations, not claims that automated tests passed.
Incomplete coverage must not yield a pass.

Gameplay event names and payloads stay project-specific. Use readable names such
as `player.landed`, `path.failed`, or `menu.focus_changed`; avoid importing a
platformer's terminology into strategy or UI-heavy games.
