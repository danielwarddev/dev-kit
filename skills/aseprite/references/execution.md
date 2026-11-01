# Execution

## Prerequisites

Aseprite with Lua scripting support and Python 3.13+ are required. Python standard
library modules suffice except for text/font operations, which require Pillow.
No MCP, uv project, dotenv, HTTP client, or custom CLI is needed.

Use the skill's actual base directory when setting `$scripts`:

```powershell
$scripts = Join-Path (Get-Location) '.claude\skills\aseprite\scripts'
$env:PYTHONPATH = $scripts
$env:ASEPRITE_PATH = 'C:\Program Files (x86)\Steam\steamapps\common\Aseprite\Aseprite.exe'
& $env:ASEPRITE_PATH --version
python --version
```

The Steam location above is this machine's installation, not a requirement. On
another machine, set `ASEPRITE_PATH` to its executable. If unset, the scripts use
`aseprite` on PATH. They do not read `.mcp.json` or `.env`.

Shell environments do not persist between tool calls: set these variables in the
same call that runs the task. Keep cwd at the asset/project directory; do not change
cwd to the skill directory. For a skill installed elsewhere, use its absolute path.
Use an existing working Python environment if available.

Only if text is needed and `python -c "import PIL"` reports a missing dependency,
create an isolated environment and install the supplied requirements:

```powershell
python -m venv "$scripts\.venv"
& "$scripts\.venv\Scripts\python.exe" -m pip install -r "$scripts\requirements.txt"
```

Alternatively, use `uv venv "$scripts\.venv" --python 3.13` and
`uv pip install --python "$scripts\.venv\Scripts\python.exe" -r "$scripts\requirements.txt"`.
Do not install the original MCP dependencies. Use that environment's Python for
text tasks. On non-Windows platforms, use native paths and the `bin/python` executable.

## Calling operations without a CLI

References show **declarations**, not executable calls. Import the module shown
and call its function with actual values. Each operation is async and returns a
string: either human-readable status or JSON, as documented.

For small tasks, pipe Python via a PowerShell single-quoted here-string. For longer
tasks or large pixel arrays, create a task-specific `.py`/JSON file in session
artifacts and run it with the same environment. No file should be added to the
project just to invoke the skill.

```powershell
$env:PYTHONPATH = Join-Path (Get-Location) '.claude\skills\aseprite\scripts'
$env:ASEPRITE_PATH = 'C:\Program Files (x86)\Steam\steamapps\common\Aseprite\Aseprite.exe'
@'
import asyncio
from pathlib import Path
from aseprite_ops.tools import canvas, drawing, export

async def main():
    sprite = str(Path("example.aseprite").resolve())
    preview = str(Path("example-preview.png").resolve())
    if Path(sprite).exists():
        raise FileExistsError(sprite)
    result = await canvas.create_canvas(32, 32, sprite)
    print(result)
    if not result.startswith("Canvas created successfully"):
        raise RuntimeError(result)
    result = await canvas.add_layer(sprite, "body")
    print(result)
    if not result.startswith("Layer 'body' added"):
        raise RuntimeError(result)
    result = await drawing.draw_rectangle_at(
        sprite, "body", 1, 8, 8, 16, 16, "#D04648", True
    )
    print(result)
    if not result.startswith("Rectangle drawn"):
        raise RuntimeError(result)
    result = await export.export_frame(sprite, 1, preview, scale=8)
    print(result)
    if not result.startswith("Frame 1 exported"):
        raise RuntimeError(result)
    if not Path(preview).is_file():
        raise RuntimeError("Preview was not written")

asyncio.run(main())
'@ | python -
```

Inspect the resulting PNG with the available image-view tool. Do not merely
print its filename or dump image bytes into the conversation.

## State and coordinates

- Each call starts Aseprite in `--batch` mode, reopens the file, then exits.
  A previous `set_layer`/`set_frame` call is not a dependable cross-call selection.
  Prefer explicit layer/frame parameters; for several active-state-dependent
  edits use one Lua script.
- Frame arguments are **1-based**, generally inclusive for ranges. Aseprite's
  raw CLI `--frame-range` is **0-based**; bundled export functions convert it.
- Drawing and region operations use sprite-global coordinates. Raw Lua
  `cel.image:getPixel`/`putPixel` use cel-local coordinates; subtract cel.position.
- Colors generally accept `#RRGGBB` or `#RRGGBBAA`. Check each operation's limits.
- Use absolute paths or cwd-relative asset paths. Create output parent directories
  explicitly. Several operations reject `..` components; resolve paths first.
- Preserve the `.aseprite` master; export PNG/GIF/sheets separately.
  Do not edit a file concurrently with another process or overwrite unsaved editor work.
- Do not execute parallel mutations of the same sprite. Await sequentially.

## Errors and verification

Upstream operation return strings are not a uniform result type. **Always print
and examine results**. Failures may start with `Failed`, `ERROR`, `Invalid`,
`File`, `Unknown`, or a parameter-specific message such as `scale must ...`.
Do not use a short universal prefix blacklist as proof of success.

For automated task scripts, require the expected positive status for that operation,
or parse JSON and assert its documented fields; stop immediately on unexpected
results. Confirm exported files exist and inspect the output. Query metadata/pixels
when an exact size, color, frame count, duration, or tag is required.

`AsepriteCommand.execute_lua_script_checked` handles `ERROR:` lines as failures.
The bundled runner also catches uncaught Lua failures with an explicit marker;
a process exit code of zero alone does not establish script success.

## Raw Lua, including parameters

Use the bundled helper to execute generated/custom Lua without copying runner code:

```python
from aseprite_ops.core.commands import AsepriteCommand

success, output = AsepriteCommand.execute_lua_script_checked(
    """
local spr = app.activeSprite
if not spr then print("ERROR:No active sprite") return end
print("SIZE:" .. spr.width .. "x" .. spr.height)
""",
    filename=sprite,
)
print(output)
if not success:
    raise RuntimeError(output)
```

Custom edits are not auto-saved: explicitly call `spr:saveAs(spr.filename)` after
successful edits. Use `app.transaction(function() ... end)` for grouped mutations.
Never interpolate untrusted paths/strings into Lua; use the existing `lua_escape`.

For a standalone `.lua` file, use Aseprite directly:

```powershell
& $env:ASEPRITE_PATH --batch $sprite --script-param 'key=value' --script $luaFile
```

Parameters are available as `app.params["key"]`. Script parameters precede
`--script`. For raw direct invocations, inspect stdout/stderr as well as exit status.
Query current official API docs before writing code using unfamiliar engine APIs:
Context7 `/aseprite/api` or `/aseprite/docs`; do not fetch docs for every bundled call.

Official CLI: https://www.aseprite.org/docs/cli/
Official scripting API: https://www.aseprite.org/api/
