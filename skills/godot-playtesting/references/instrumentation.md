# Minimal instrumentation

## Purpose and escalation

Instrumentation gives the agent runtime visibility, not a new gameplay feature,
test replacement, analytics pipeline, or universal debugger.
Choose based on the evidence gap, not the availability of a template:

| Situation | Preferred response |
|---|---|
| Existing tests/logs/debug views answer the question | Reuse them; add nothing |
| One transition or value is missing | Add a targeted development-only event/sample |
| Several behaviors need a shared timeline | Add a small recorder using the trace contract |
| Reliable reproduction requires controlled actions | Add a bounded scenario using existing input/test facilities |
| Game concepts need translating into events | Add narrow project-specific adapters |
| Same recorder repeatedly needed across distinct projects | Consider extraction later; do not assume a framework now |

The runtime transport can be generalized: run identity, ordered records, bounded
storage, serialization, sampling, and error reporting. Its implementation can still
be small and project-specific. Adapters identify the local player, damage pipeline,
AI transitions, or other semantics. Neither is mandatory in every project.

Before editing, identify exactly which question each recorded field answers.
Prefer explicit registration of a few relevant values to automatic reflection or
scene-tree/property dumps. Prefer existing signals and decision points to polling
everything. Reuse an existing logger/recorder if suitable; do not duplicate it.

## Correctness and overhead

- Monitoring must not drive decisions, consume gameplay RNG, modify health/state,
  change physics order, or create gameplay dependencies on recording being enabled.
- Copy relevant values at observation time; do not retain mutable objects whose
  later serialization changes the recorded past.
- Label observation phase: requested action, before movement, after movement, etc.
  A collector callback is not automatically after every entity has updated.
- Record important events immediately. Sample routine state less often, with the
  rate chosen for the question. Short collision/hit windows need event records or
  targeted tick-level capture, not sparse samples.
- Keep serialization/file I/O out of hot paths where practical. Bound buffers,
  file size, duration, entity count, and expensive queries.
- Flush at appropriate boundaries; shutdown/crash may prevent a final flush.
  Missing finalization is incomplete evidence, not success.
- If records are dropped or limits hit, emit/report counts and affected ranges.
  Do not silently overwrite evidence or turn an I/O failure into an empty success.
- Check that enabled monitoring does not materially alter the behavior being
  investigated. Use profiling for actual performance claims.

Do not prescribe a complicated worker-thread architecture for a small trace.
Use the simplest implementation that satisfies the chosen limits and semantics.

## Lifecycle and scope

Default to disabled. Use the project's development/test activation convention;
do not rely on a vague "debug build" assumption without checking export behavior.
Normal play and shipping exports must not enable agent observation by accident.
Keep traces out of assets, source control, and shipping exports.

Temporary diagnosis hooks should be removed after the investigation unless a
repeat-use reason justifies retaining them. Retained hooks must be explicitly
development-only, documented at the activation point, bounded, and inactive by
default. Do not make a new permanent autoload or addon as the automatic choice.

Use session artifacts or a designated ignored diagnostic output directory.
Record only necessary game state. Exclude credentials, player personal data,
chat, arbitrary filesystem contents, and sensitive machine paths.
Do not upload traces to external services without authorization.

Respect user-owned runs and unsaved editor state. Stop only the specific run
started for the task; cleanup only known task-owned files.

## Validation of observation code

Verify activation/deactivation, intended event wiring, output readability, and
completeness/error reporting. When introducing reusable recorder behavior, use
focused tests for its actual semantics, such as dropped-record reporting or a
serialization failure. Apply the repository's standards rather than adding a
new test framework.

Monitoring observations supplement the task's existing tests. Convert a reproduced
bug into a regression test when appropriate; a one-off trace is not that test.
