---
name: godot-playtesting
description: 'Specialized Godot runtime playtesting support. Use for slow/unreliable scenario access, development-only setup controls, runtime trace/state analysis, timing/feel/balance measurements, or temporal evidence that existing tests and bounded normal inspection cannot establish. Reuse existing controls and traces. Do not load for ordinary coding, scene edits, launches, screenshots, or tests when existing evidence/access suffices. Supplements tests and visual/human checks.'
---

# Godot Playtesting

**This is not a general Godot coding or debugging skill.** It helps reach/repeat
runtime scenarios and observe/interpret behavior over time, including runtime bugs
and feel/balance questions. A gameplay feature or bug fix alone does not trigger it;
specialized observation or scenario-access support does. Behavior spanning frames
alone is not a trigger when existing tests establish the relevant sequence and a
bounded normal run supplies the remaining runtime/visual evidence.
Trace/state analysis and timing/feel/balance measurements are triggers even when
existing tooling already supplies the data. Slow or unreliable access to the
required state is also a trigger, even when an existing shortcut can solve it.
Use project coding standards for implementation and `godot-engine` for engine tools.

Controls establish scenarios; monitoring exists ONLY to help the agent see what
happens at runtime. Neither replaces automated tests, screenshots/video, profiling,
or human playtesting. Forced victory does not verify winning; set gold does not
verify earning it. Metrics support design goals, not proof of visual quality or fun.

## Decision and workflow

1. Identify the behavior, evidence needed, starting state, and real path under test.
2. Inspect existing tests, logs, controls, debug tools, and scenario/input helpers.
   If access/evidence suffices, add nothing.
3. Read only applicable references below. Add the smallest missing project-specific
   setup or observation mechanism; no universal recorder/console/addon is required.
4. Run a bounded isolated scenario. Verify setup committed, record interventions,
   and distinguish bypasses from naturally observed behavior.
5. Read a summary first, then relevant entities/events/tick ranges. Inspect associated
   screenshots/video; run applicable tests after changes and repeat the scenario.
6. Reset overrides/input, protect real saves, and remove temporary hooks or retain
   explicitly justified development-only ones. State evidence gaps honestly.

For C# Godot work, `csharp-standards`' `godot-development.md` contains the initial evidence assessment.
Assess once; revisit only for changed scope, failed checks, or new evidence gaps.
This skill provides portable guidance and opt-in recipes, not an installed runtime framework.

| Need | Read |
|---|---|
| Execute bounded input, assertions, samples, or screenshot checkpoints | `references\recipes.md` |
| Development shortcuts, discovery, activation, save isolation | `references\design.md` |
| Shortcut verification and explicit bypass boundaries | `references\verification.md` |
| Whether/how to instrument; temporary versus retained hooks | `references\instrumentation.md` |
| Trace format, identity, time, events, state, completeness | `references\trace-contract.md` |
| Repeatable scenarios, inputs, expectations, stopping | `references\scenarios.md` |
| Summary-first analysis, anomaly windows, visual correlation | `references\analysis.md` |
| Movement/combat/AI/UI observation patterns | `references\observation-patterns.md` |
| Responsiveness, pacing, balance, and subjective limits | `references\feel-and-balance.md` |
| Godot timing, version checks, launch/cleanup safety | `references\godot-integration.md` |

Prefer named/scriptable controls, not hidden hotkeys alone. Record interventions.
Monitoring/controls must be disabled by default and excluded from normal/shipping
use; do not alter gameplay decisions to collect evidence.
Never treat an empty log, successful launch, or zero exit code as proof.
Preserve the user-owned editor; stop only task-owned runs through exact handles.
Do not add unrelated architecture or permanent infrastructure.
Resolve references from this skill's base directory; never load all references or
dump entire traces into context.
