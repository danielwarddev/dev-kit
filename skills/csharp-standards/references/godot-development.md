# Godot Development

## Context and evidence

Identify the actual engine version, existing patterns, and project root containing
`project.godot`. Query Context7 `/godotengine/godot-docs` for API usage, engine
behavior, scene setup, and version details before engine-dependent changes.
If unavailable, use installed-version official docs and disclose uncertainty.

Before implementation, briefly identify the changed behavior, existing evidence,
and how to reach the relevant state. Reassess only for changed scope, failed checks,
or new evidence. Use `godot-engine` for engine operations, ordinary runs, errors,
screenshots, and resource edits. Use `godot-playtesting` only for specialized
trace/state analysis, timing/feel measurements, temporal evidence existing tests
and bounded normal inspection cannot establish, or impractical scenario access.

Reuse existing tests, logs, scenarios, and controls; add only the smallest missing
mechanism, not a universal recorder, console, or addon. Optional skills are not
prerequisites for coding; if unavailable, use existing tools and state evidence
gaps. Install only when needed and authorized.

## Architecture

Keep gameplay rules/state in the engine-free core; the view renders and relays
input. Keep callbacks short and nodes focused. Deterministic wave/spawn decisions,
formation, transitions, and hit resolution belong in core logic. Pure helpers
using Godot types belong in Godot-facing code even without an editor dependency.
The Godot-free dependency analyzer protects the core boundary, not the placement
of rules within node scripts.

Use reliable engine Tween/Path2D/PathFollow2D features for presentation rather than
reimplementing them just to unit-test engine behavior. Use core-side movement math
when core-owned data drives it or deterministic queries outside node lifecycle
require it. Do not introduce projects/frameworks solely for a small task.

## Wiring and input

Use `[Export]` for scene-authored startup child/sibling/ancestor references,
not name-based `GetNode` or fixed-depth parent chains. Runtime lookups are for
dynamic relationships. Required wiring must fail loudly, not fall back or skip
behavior; use non-`OrNull` accessors for required lookups. Optional references
may be null and use optional accessors.

Exported C# member names are serialized scene keys: rename through the editor or
update every assignment and validate resolution. For exported Node references,
preserve `node_paths` declarations alongside NodePath assignments.

Use small node components and sub-scenes; export designer-configurable values.
Prefer C# `event Action`/`Action<T>` for C# object communication; reserve Godot
signals for editor-visible or scene-tree hookups. When components drive a physics
body, integrate in `_PhysicsProcess` and coordinate velocity changes through
component methods. Pool frequently spawned nodes and let them signal lifecycle
completion instead of making every caller manage return.

Use declared input actions instead of hardcoded gameplay keys/scancodes. Update
every consumer when action names, exports, node/resource paths, or scene names
change. Build diagnostics check supported C# constant input arguments; GDScript,
wrappers, and runtime-computed names still need manual/runtime verification.

## Resources and ownership

Prefer Godot-aware tools/editor operations. Preserve format, inheritance, and
user-authored edits when text editing; never regenerate a scene wholesale or
overwrite unsaved editor work.

Use `res://` asset paths and engine-generated UIDs, including `.cs.uid` files
generated on import; never invent UIDs. Do not edit generated `.godot`, `bin`, or
`obj` contents. Keep secrets out of source, scenes, resources, and settings.
Never close the user-owned editor; stop only task-owned runs by exact handle.

Touch nodes/scene tree only from valid engine contexts. Keep blocking I/O off the
main thread and bound async work/cancellation to node/scene lifetime.

## Validation

Run focused tests for changed behavior, with core rules tested in core tests.
Engine-facing changes also require exercising the affected scene/flow and
inspecting script/resource/scene errors. Core tests do not prove wiring, input,
lifecycle, or presentation works. Do not widen visibility to test nodes; extract
reusable rules.

Follow repository build/import and Godot validation commands. Passing Godot validation
does not establish required-but-omitted wiring, computed names/paths, gameplay
correctness, or visual quality; verify those separately.

For frame/tick-spanning behavior, establish timing and state sequences, not just
one screenshot. Use a known state, inspect actual output, and correlate visuals
with events when needed. Monitoring does not replace tests, visual checks,
profiling, or human playtesting. Setup shortcuts do not prove bypassed behavior;
metrics do not prove fun or aesthetic quality. Report direct paths versus bypasses
and remaining uncertainty. Clean up temporary hooks/state and preserve real saves.
