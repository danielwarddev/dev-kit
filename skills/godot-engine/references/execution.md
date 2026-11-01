# Execution and setup

Godot 4 editor executable is required. Use a .NET-enabled build for C# projects.
Python 3.10+ standard library is sufficient for the optional direct-import helpers.
No packages need installing.

Resolve the project's configured executable first (`GODOT_PATH`, existing scripts,
PATH). On this machine the inspected server used:

```powershell
$godot = 'C:\Program Files\Godot\4.7\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --version
$project = 'C:\path\to\directory-containing-project.godot'
$scripts = 'C:\path\to\skill\scripts'
```

That location is an example, not a portable default. On Windows prefer the console
executable to capture output. Check the actual version and `--help` before using
version-specific flags; unsupported Godot arguments can be silently ignored.
Use the current task's actual project root and engine, not authoring-machine paths.

## Invoking bundled operations

The original MCP already invoked `godot_operations.gd` as a headless script.
Use the same script, with snake_case JSON parameters. The helper is an importable
module, not a CLI/dispatcher application:

```powershell
$env:GODOT_PATH = $godot
$env:PYTHONPATH = $scripts
@'
from operations import operate
output = operate(
    r"C:\path\to\project",
    "create_scene",
    {"scene_path": "Scenes/Example.tscn", "root_node_type": "Node2D"},
)
print(output)
'@ | python -
```

Set environment variables in the same shell call as execution; tool shell state
may not persist. Write longer task scripts/JSON in session artifacts, not the game.
For strings/large payloads, do not build a shell command through interpolation.
The Python helper passes an argument array with serialized JSON.

Direct invocation is also possible:

```powershell
& $godot --headless --path $project --script "$scripts\godot_operations.gd" create_scene '{"scene_path":"Scenes/Example.tscn","root_node_type":"Node2D"}'
```

Prefer the helper on Windows when complex JSON quoting is involved. The upstream
script parses `OS.get_cmdline_args()` after `--script`, not user args after `--`.

## Errors, imports, and validation

`operate` returns stdout, except `get_uid` returns parsed data. It rejects missing
required arguments and invalid resource paths, surfaces nonzero exit, stderr,
script/resource errors, and missing success confirmation. Timeouts raise explicitly.
It intentionally treats stderr conservatively: inspect warnings rather than
silently suppressing them. This policy applies to bounded operations, not an
assertion that every runtime warning means gameplay failure.

Upstream script branches can print errors yet exit zero. Do not bypass output
checks when calling it directly. Debug mode can create diagnostic files and dump
environment values; do not enable `--debug-godot` in normal use.

Import new assets before loading them:

```powershell
& $godot --headless --editor --path $project --import
```

Capture both output streams and check them. For C# projects use the repository's
build workflow; no arbitrary rebuild/import is needed for every command.
Do not edit generated `.godot` files.

Paths passed to operation parameters are project-relative or `res://`, using
engine forward slashes. Python rejects traversal/absolute resource paths; pass
native absolute paths only for the project, engine, script, or log files.
Operations mutate disk immediately. Do not run simultaneous mutations on the
same scene/project, replace existing scenes unintentionally, or conflict with
unsaved editor state.

Confirm actual resources/fields/UIDs, not only success-shaped text. Check diffs,
engine reload, relevant tests, and visible output as appropriate.
The resource operations do not play the game or capture screenshots.
For a fresh rendered scene capture, use `capture.md` and `scripts\capture.py`.
For bounded input/checkpoint scenarios, use `godot-playtesting`'s recipe rather
than rebuilding input/capture plumbing.

Context7 should be queried first when available; otherwise use official docs
matching the installed engine. Entry points:
https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html
https://docs.godotengine.org/en/stable/classes/class_resourcesaver.html
