# Provenance and boundaries

Operation scripts and shared helpers were adapted from the local `aseprite-mcp`
checkout, revision `90d1696a7e41edff89bbd0823ae6a5f86c114bcc` (including its
working-tree contents at port time). Copyright (c) 2024 Divyansh Singh.
The upstream MIT license is included at the skill root.

113 operations across 18 importable modules are bundled. The source README's
104-tool count is older than the inspected implementation.

Port changes:

- Removed MCP imports, registration decorators, and the server launcher.
- Kept Python functions, generated Lua, shared algorithms, parameters, and result
  strings; no comprehensive CLI or dispatcher was introduced.
- Made imports selective instead of eagerly importing all operation modules.
- Removed implicit `.env` loading; executable discovery uses `ASEPRITE_PATH` or PATH.
- Wrote temporary Lua in UTF-8 and added explicit uncaught-script error detection.
- Replaced detached HTTP-preview operations with documented standard Python usage.
- Moved the animation guide into `workflow.md` rather than a callable tool.

This is a vendored snapshot, not a dependency on the external source checkout.
It runs without MCP installed. Pillow is only needed for text operations.
The original `.mcp.json` is retained as a fallback; this skill prefers scripts.

Do not mechanically extract the f-string Lua into `.lua` files: Python validates
inputs and constructs code/data, and text rendering runs on the Python side.
Direct imports preserve that behavior without another command interface.
