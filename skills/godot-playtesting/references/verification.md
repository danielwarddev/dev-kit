# Using shortcuts without invalidating evidence

Declare which control establishes the scenario and which behavior is under test.
List bypasses in scenario/evidence metadata. "Reached the screen" is not "verified
the natural gameplay path."

| Shortcut | Useful for | Does not establish |
|---|---|---|
| Give/set gold | Shop rendering, purchases from a known balance | Earning gold, reward calculations |
| Force victory | Victory presentation, navigation | Objective detection or winning |
| Select level | Inspect a particular scene/encounter | Unlocking/progression into that level |
| Set health/invulnerability | Inspect late combat states | Natural survivability/balance |
| Pause AI | Inspect layout/state | Normal AI timing or combat interaction |
| Change time scale | Inspect a transition more closely | Unmodified timing, latency, or game feel |

When the bypassed system is the task, start near the scenario then execute its real
path. To check earning gold, trigger the real reward; to check winning, complete
the relevant objective; to check countdown expiry, let the countdown actually expire.
Do not use a force-outcome command as proof of trigger correctness.

## Observation and checks

Where runtime observation is active, emit a developer-control intervention event
with run/tick/sequence context, command, relevant parameters, effect, and success/
rejection reason. Record before/after values when useful. Never label an intervention
as an organic gameplay event. Do not dump sensitive save contents.

Verify setup actually committed, then use the intended input/gameplay path and
check results with applicable tests, screenshots/video, traces, and playtesting.
Disabling AI or changing speed invalidates conclusions about ordinary AI timing;
restore overrides and rerun unmodified before making those claims.

Controls need focused verification too: correct activation, parameter validation,
state coherence, explicit rejection, and normal/shipping unavailability.
Use existing tests/workflows when possible, adding only tests for introduced
behavior. Do not claim release exclusion without checking actual export/build rules.

Clean up disposable state/temporary hooks and confirm real progress was untouched.
Describe remaining evidence gaps, especially paths that were deliberately bypassed.
