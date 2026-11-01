# Domain-specific observation patterns

These are menus, not required instrumentation. Select only fields/events that
answer the current question. Record domain semantics at local decision points;
do not impose one controller/combat/AI architecture across Godot projects.

## Movement and collisions

Observe input intent, requested velocity/displacement, actual position/displacement,
contact identity/normal, grounded state, and any movement mode transition.
Distinguish local/global coordinates and blocked versus sliding axes.
Record jump requests versus accepted jumps, landing, dash start/end, or teleport.

Look for no movement despite accepted input, penetration, unexpected acceleration,
jitter around contacts, stale grounded state, or position jumps.
Use physics-tick evidence for brief failures; screenshots show geometry and feel.

## Combat and health

Use action/attack IDs, source/target IDs, attack start, hit-window open/close,
collision/hit acceptance, rejection reason, damage before/after, cooldown, and death.
Separate an overlap from a valid hit and a valid hit from applied damage.

Look for duplicate damage from one attack, damage outside the active window,
missed targets, dead entities still acting, or cooldown/timer double completion.
Visual inspection is still needed for hit feedback, animation alignment, and clarity.

## AI and navigation

Observe state transitions with reasons, target ID/distance, detection/loss,
path request/result, next goal, actual movement, and attack eligibility.
Record relevant line-of-sight/obstruction results only when they affect decisions.

Look for state oscillation, unreachable goals, stale targets, path failure,
movement stalled despite a path, or attacks from unintended ranges.
A generic "chase" label alone does not explain a decision.

## Animation and presentation

Observe requested versus selected animation/state, transitions, playback position
when needed, completion events, facing, and relevant visibility/camera changes.
Rendering clocks may differ from physics clocks; label them.

Look for missing transitions, unintended restart loops, wrong direction, or
completion-triggered effects firing repeatedly. Correct numeric state cannot
establish smooth animation, visible effects, or good pacing: inspect screenshots/video.

## UI, input, and pause

Observe actions, focus owner, menu/modal state, input acceptance/consumption where
the project's input path exposes it, pause changes, selection, confirmation,
cancellation, and timer outcome.

Look for input leaking through modals, focus lost after closing a panel, double
confirmation, actions during pause, or countdown outcomes firing twice.
Use interactive visual checks for layout, focus indication, and readability.

## Spawning, scenes, chunks, and persistence

Observe spawn/despawn IDs, transition request/commit, old/new scene identity,
intended versus actual placement, relevant state before/after, and active counts.
Do not dump whole saves or scene trees when a few fields establish continuity.

Look for duplicate entities, missing cleanup, stale references, unintended health/
inventory resets, wrong placement, or repeated transition callbacks.
Use actual save/load tests when persistence correctness is the requirement.

## Other game genres

For strategy games, record orders, resource changes, path failures, and completion.
For physics puzzles, record constraints, contacts, triggers, and win-state transitions.
For UI-heavy games, focus on navigation and state flow rather than player position.
Generalize the record envelope and workflow, not gameplay terminology.
