# Minimal control design

Discover before creating: existing debug actions/menus, scenario launch arguments,
save fixtures, service commands, encounter resets, and test helpers may suffice.
Add a control only when reaching/repeating the task's required state is otherwise
slow, unreliable, or impractical. Do not build a cheat console for a single shortcut.
The portable skill defines discipline; runtime implementations/adapters stay local.

## Useful categories

| Purpose | Examples |
|---|---|
| Reach state | Select level, start encounter, jump to checkpoint |
| Establish state | Set health/gold, grant item, select loadout or seed |
| Exercise outcome | Request victory/defeat/rewards/transition |
| Inspect behavior | Pause AI, invulnerability, adjust simulation speed |
| Repeat scenario | Reset encounter, restore isolated initial state |

Implement only categories needed now. A level selector must honor the project's
actual level identity/content model, not assume scene filenames are levels.

## Semantics and integration

Use named commands with clear parameters, preconditions, effects, and help/discovery.
Return/log explicit failure for unknown levels/items, unavailable commands, invalid
values, or rejected transitions. Verify committed state rather than acknowledging
only receipt. Prefer existing gameplay services, not disconnected field writes.

Human hotkeys/debug menus and agent scenario arguments may call the same command.
Do not assume the agent can use a GUI or needs a persistent network endpoint.
Reuse existing scriptable input/scenario facilities; no MCP requirement.

Separate normal-path operations from forced states with unambiguous names:

- Request completion through the normal objective/transition path.
- Force victory presentation for inspecting a screen.
- Grant a reward through its normal service.
- Set balance directly for isolated shop setup.

These are different controls, with different proof boundaries. Never silently
fallback from a rejected normal-path command to a forced state.
For coherent setup spanning several fields, use an existing fixture/reset service;
do not leave a partially configured state while reporting success.

## Safety and lifecycle

Require explicit development/test activation; disabled in ordinary play.
Verify shipping exclusion using the project's language/export/build conventions,
not just a hidden menu or undocumented hotkey. No gameplay dependency should
require the controls to be enabled.

Use disposable save slots/session state. Identify the actual save destination before
mutation; disabling autosave alone may not block explicit save code.
Do not grant items/gold or trigger outcomes against real player progress by default.
Do not expose commands to public networking or add remote-control infrastructure.

Maintain deterministic setup where practical and record seeds/settings, without
claiming a seed guarantees deterministic replay. Restore time scale, invulnerability,
AI pause, input holds, and other overrides during cleanup, including abnormal exit
where the host/project supports it. Never carry overrides into later measurements.

Temporary controls should be removed unless there is an explicit repeat-use reason
to retain them. Retained controls must remain development-only, discoverable,
bounded, and documented. Follow the project's engine/core and language conventions;
do not add an autoload or rewrite gameplay architecture automatically.
