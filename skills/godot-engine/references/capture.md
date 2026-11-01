# Rendered screenshot recipe

Prefer `scripts\capture.py` for a fresh scene capture. It uses the same runtime as
the playtesting scenario recipe; do not reconstruct window-capture code each time.
Python standard library and a graphical Godot executable are sufficient. Use a
.NET-enabled executable and build/import the project first for C# scenes.

```powershell
$scripts = 'C:\path\skills\godot-engine\scripts'
$project = 'C:\path\game' # Contains project.godot
$output = 'C:\path\session-artifacts\capture-01' # Must not already exist
python "$scripts\capture.py" --project $project --scene 'res://Scenes/Example.tscn' --output $output --godot godot-mono --frames 2 --timeout 60
```

The output directory contains `scene.png`, `result.json`, the request, and both
engine logs. The JSON records dimensions, render frame, run identity, and
completion. Inspect the image, not merely the success message.

## Scope and safety

- This launches a new task-owned game, not a capture of an existing game/editor.
  It loads the requested scene with normal project autoloads. Starting at that
  scene does not prove the normal navigation/progression path reaches it.
- The launcher copies the actual project, preserving imports and built assemblies,
  and redirects `user://` to a unique empty save directory before engine startup.
  The copied project and that run's save directory are removed afterward. Copying
  a large project adds disk/time overhead; this is intentional isolation, not a
  live-editor workflow.
- Isolation covers the project copy and `user://`, not arbitrary absolute file
  writes, networking, or external services. Review projects with such side effects
  before running. The source project and user-owned editor are not modified.
- `--frames` counts startup physics-frame boundaries, not application readiness.
  Capture waits for a completed rendered frame, avoiding an unrendered texture.
  For asynchronous readiness, a menu state, or a short-lived effect, use the
  `godot-playtesting` scenario recipe with relevant assertions/checkpoints instead.
- Headless captures are rejected. Root-viewport PNGs show game content without
  OS window borders; they do not establish actual desktop DPI/window appearance.
- Use fresh output directories outside the project. Existing output is never
  overwritten. Timeouts, engine errors, missing completion, and failed image
  writes are errors, with available partial evidence retained.

For an already-running window or unsupported capture need, choose a task-specific
method and name its limitations. Do not treat it as an interchangeable second
default recipe.
