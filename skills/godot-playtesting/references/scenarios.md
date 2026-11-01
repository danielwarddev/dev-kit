# Bounded runtime scenarios

Logging alone does not let the agent play reliably. A useful companion is a
repeatable sequence with a known starting state and a defined outcome.
Reuse existing tests, debug scenes, replay/input helpers, or command-line controls
before creating a scenario runner. A monitoring scenario is not a replacement
for the project's unit, integration, or end-to-end tests.

For the supported input/assertion/capture implementation, read `recipes.md` before
writing new scenario plumbing. Reuse project-specific fixtures for setup and
stricter tick ordering rather than growing the portable recipe into a game framework.

## Specify before running

- Question and expected behavior, including measurable tolerances.
- Starting scene/state, entity placement, relevant settings, and seed(s).
- Action sequence with timing, including press, hold, release, and idle periods.
- Whether input goes through the actual gameplay input path or bypasses it.
- Observation fields/events and any associated screenshots/video checkpoints.
- Maximum duration/ticks, success/failure/timeout stopping conditions, and cleanup.

Use real gameplay input when verifying input-to-outcome behavior. Directly calling
a movement/combat method can be useful for isolation, but cannot establish that
input mapping, UI focus, pause handling, or the controller integration works.
Label the bypass clearly.

Do not depend on wall-clock sleeps for tick-exact actions. Use the scenario's
declared simulation phase/tick boundaries, and always release held input on exit.
Do not create enemies/teleport players in normal gameplay just to obtain evidence.

## Example: movement into a wall

Start at a known position with an identified blocking wall. Hold move-right for
180 physics ticks, then release. Observe requested input, attempted motion, actual
displacement, and contact events. Expect motion before contact and no penetration
after it, within a defined numerical tolerance. Specify wall orientation and any
allowed sliding rather than equating all wall contact with zero total velocity.

```text
Scenario: move_into_wall
Observed: ticks 1-180; complete; no relevant records dropped
Input: move_right held, then released
Player displacement: 48 px
Wall contact began: tick 61
Displacement along blocked axis after contact: 0 px
Observed check: PASS within 0.1 px tolerance
```

Those numbers are illustrative, not a default expectation for every controller.

## Expectations that expose temporal bugs

| Behavior | Possible expectation |
|---|---|
| Combat | One attack damages a given target at most once |
| AI | Enemy reaches an appropriate state/range within the scenario window |
| Scene/chunk transition | Intended position/health survive; transition happens once |
| Countdown | Outcome triggers once, not twice, at the intended threshold |
| Input/focus | Intended control receives the action; gameplay is blocked when appropriate |
| Animation | State changes select the intended animation and completion behavior |

Choose expectations from the actual requirement. Do not infer "correct" values
from the implementation under investigation.

For feel/balance scenarios, define measurement boundaries and design targets
before interpreting results; see `feel-and-balance.md`. A target-range observation
is not proof of enjoyment. Preserve timed-out/incomplete cases and distinguish
scripted behavior from human play.

## Repeatability and limitations

Record seed(s), relevant settings, and initial state. Physics, scheduling, render
timing, and external inputs can still vary; do not promise deterministic replay
from a fixed seed alone. Repeat intermittent cases and preserve failing evidence.

Distinguish "not observed", "observed absent with sufficient coverage", and
"observed incorrect". A timeout or missing required event is not success.
Headless runs may help behavior inspection but do not verify rendered presentation,
focus behavior, or all aspects of a normal interactive session.

After a fix, rerun from a fresh declared starting state and run applicable existing
tests. Avoid comparing traces from different settings as if only code changed.
