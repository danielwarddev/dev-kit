# Editor and game lifecycle

## Editor

If the user's editor is open, leave it open. Do not launch duplicates merely to
validate resources. If launching is necessary:

```powershell
& $godot --editor --path $project
```

Use the runtime's attached background execution for this long-lived command.
An editor is user-owned; do not stop its session/process as task cleanup.
If the user explicitly wants it to survive the assistant session, use the runtime's
supported persistent/detached launch. Do not implicitly detach.

This starts an editor; it does not connect to or control its UI/unsaved scene state.

## Game

Start the game, optionally with a specific resource scene:

```powershell
& $godot --debug --path $project 'res://Scenes/Example.tscn'
```

Omit the scene for the project's configured main scene. Keep graphical rendering
for visual/input checks. Use `--headless` only when the scenario supports it and
the narrower evidence is acceptable.

Run through an attached async shell tool; retain its returned session handle.
Verify startup from actual process output/status and a scenario readiness event or
visible game window when appropriate. Never claim success solely from spawning.

Read stdout/stderr from that same shell session. Save logs in session artifacts
when long captures need querying. For file capture:

```powershell
& $godot --debug --path $project 'res://Scenes/Example.tscn' 1> $stdoutLog 2> $stderrLog
```

Redirected output may buffer; allow for incomplete final lines while the run is
active. Keep files after exit until inspected. Do not read stale logs from another
run or paste every frame into context. Normal process exit does not prove tests pass.

## Bounds and stopping

For short smoke runs, supported versions offer `--quit-after N`. This is an engine
iteration limit, NOT a promise of N physics ticks or a completed gameplay scenario.
Use an explicit scenario-completion/timeout mechanism for behavioral verification.

Prefer graceful project/scenario exit so observation logs finalize. Otherwise stop
the exact attached game session using the runtime's stop tool. If necessary use
the recorded specific PID, never a process-name kill. Verify the run exited.

Do not close the editor, kill all Godot processes, or stop a run someone else owns.
Do not replace the MCP's "one active process" behavior with global PID files or an
automatic previous-process kill. Independent task-owned session handles suffice.

Use `capture.md` for the preferred fresh-scene screenshot recipe, or
the bounded input/checkpoint recipe in `godot-playtesting`.
Retain visual inspection and applicable tests for the actual requested behavior.
