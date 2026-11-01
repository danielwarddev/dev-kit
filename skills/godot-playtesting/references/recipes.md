# Bounded input and observation recipe

Prefer `scripts\run_scenario.py` when existing project tests/scenarios do not
already supply the needed input/checkpoint workflow. It reuses the sibling
`godot-engine\scripts\runtime.py` and `runtime.gd`; install both skills together
to use these scripts. No plugin, autoload, permanent cheat console, or network
server is added to the project.

```powershell
$scripts = 'C:\path\skills\godot-playtesting\scripts'
python "$scripts\run_scenario.py" --project 'C:\path\game' --scenario 'C:\path\session-artifacts\inventory.json' --output 'C:\path\session-artifacts\inventory-run-01' --godot godot-mono --timeout 60
```

Store task-specific JSON in session artifacts, or reuse a project's established
scenario fixtures. The following is an example, not a promise that these nodes
or actions exist in every game:

```json
{
  "scene": "res://Scenes/Example.tscn",
  "startup_frames": 2,
  "steps": [
    {"op": "wait_until", "node": ".", "property": "ready_for_input", "equals": true, "frames": 120},
    {"op": "key", "action": "toggle_inventory", "frames": 2},
    {"op": "assert", "node": "UI/Inventory", "property": "visible", "equals": true},
    {"op": "capture", "name": "inventory_open"},
    {"op": "click", "node": "UI/Inventory/CloseButton"},
    {"op": "assert", "node": "UI/Inventory", "property": "visible", "equals": false}
  ]
}
```

## Operations

| Operation | Fields and behavior |
|---|---|
| `wait` | `frames`: await this many physics-frame boundaries and then an idle boundary |
| `key` | `action`, `frames`: duplicate the first configured keyboard binding, deliver press/hold/release through input event parsing |
| `action` | `action`, `frames`: hold/release action state directly; explicitly recorded as bypassing key-event/UI dispatch |
| `click` | `node`: deliver pointer motion and left press/release to a visible Control's center through its viewport GUI path |
| `assert` | `node`, `property`, `equals`: compare the observed value with the requirement's expected value |
| `wait_until` | `node`, `property`, `equals`, `frames`: observe until the expected value appears or the physics-boundary budget expires |
| `sample` | `node`, `property`: record a value without asserting it |
| `capture` | `name`: save a root-viewport PNG after a completed rendered frame |

Node paths are relative to the current scene, including after a real scene change;
`.` selects the scene root. Properties must exist and be engine-visible; use indexed
subproperties such as `position:x` for scalar comparisons. Scalars, Vector2/Vector3
and Color observations are supported; vectors/colors appear as numeric arrays.
Do not widen production visibility solely for this recipe.

Each wait/hold is 1-3600 physics boundaries; startup defaults to 2. A request has
1-100 steps, with a process timeout defaulting to 60 seconds (maximum 600).
Capture names are unique letters/digits/underscores/hyphens. Unknown operations,
actions, nodes, properties, failed assertions, and invalid configuration fail
explicitly; there is no fallback to forced success.

## Evidence boundaries

- `key` exercises configured keyboard-event handling, including `_Input` and pause/
  modal processing. It preserves the binding's key/modifier/device fields (a wildcard
  device uses zero), and records keycodes/device. This is simulated input, not proof
  that physical hardware, OS focus, or the user's keyboard layout matches the binding.
- `action` is useful for polling-based movement tests, but does not establish key
  mappings or event-driven menus. Never substitute it silently when `key` fails.
- `click` exercises GUI input, not direct signal emission. Both clicks and captures
  require rendering; `--headless` is only for narrower state/input evidence.
- Physics-boundary waits are not a guarantee of tick-exact callback ordering or
  deterministic replay. Use existing project tick/scenario helpers for stricter
  measurements; this recipe does not change scheduling or simulation speed.
- Assertions are exact. Choose expected values from requirements, not observed
  implementation values; use project tests for tolerances or richer assertions.
  A successful capture/sample alone does not prove gameplay correctness.

The output contains `result.json`, PNG checkpoints, request, and stdout/stderr.
Results include step observations, frame markers, completion, and declared bypasses.
On failure/timeout, inspect partial results and logs. Setup uses a copied project
and empty isolated `user://`, then removes both; original saves/editor remain
untouched. Absolute file writes and external services are not sandboxed.
Build/import first for C# or new assets. Outputs must be fresh directories outside
the source project. Input is released and pause reset during controlled shutdown;
timeouts terminate only the task-owned process.
