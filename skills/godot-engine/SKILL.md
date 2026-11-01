---
name: godot-engine
description: 'Use when operating Godot projects: discovering projects, inspecting engine/version, launching editor or game, collecting debug output or rendered screenshots, stopping an owned run, creating/editing/saving scenes, assigning textures, exporting MeshLibraries, or inspecting/updating UIDs. Uses direct Godot commands and bundled scripts instead of Godot MCP; does not connect to a live editor.'
---

# Godot Engine Tools

Prefer these commands/scripts over Godot MCP. No MCP runtime, Node.js, npm,
custom CLI, addon, or editor plugin is required.

## Start

For C# Godot work, use `csharp-standards` and its `godot-development.md` reference.
Before operations, identify the needed evidence and how the desired state is reached.
If reaching the desired runtime state is impractical, consult `godot-playtesting`.

1. Read `references\execution.md` to locate the engine/project and invoke scripts.
2. Read only the topic needed below. Never load the whole script/tool catalog.
3. Run the bounded operation, inspect output/errors, and verify the actual result.
   Scene/resource changes also require applicable tests and visual/runtime checks.

| Need | Reference |
|---|---|
| Engine discovery, invocation, paths, failures | `references\execution.md` |
| Start editor/game, capture output, stop owned run | `references\runs.md` |
| Capture a rendered scene with the supported screenshot recipe | `references\capture.md` |
| Discover projects, version, project structure/settings | `references\projects.md` |
| Create scene, add node, assign texture, save/copy scene | `references\scenes.md` |
| Export a MeshLibrary | `references\meshes.md` |
| UID sidecars, imports, resource resaving | `references\uids.md` |
| Original tools, port limitations and license | `references\provenance.md` |

Resolve references/scripts from this skill's base directory; keep task cwd in the
project. Use the directory containing `project.godot`, not a solution/repository
root by assumption. Use native filesystem paths; Godot resource/node paths use
engine syntax (`res://`, `/`) even on Windows.

**Disk operations do not edit the open editor's in-memory scene.** Preserve unsaved
work: do not save over a file with unsaved editor edits. Repacking scenes can affect
inheritance/instances; inspect diffs and choose a targeted task script if needed.

Never close/terminate the user-owned editor. Stop only a run started for the task
using its exact handle. Do not automatically kill a previous run to start another.
Do not interpret successful process startup or a zero exit code as correct gameplay.

For trace/state analysis, timing/feel measurements, or temporal evidence that
existing tests and bounded normal inspection cannot establish, use
`godot-playtesting` when available. Ordinary launches, error inspection, and
screenshots stay in this skill. Monitoring complements tests, screenshots/video,
and playtesting.
Follow project coding standards and consult installed-version engine docs for
unfamiliar APIs. The original MIT license is included in `LICENSE`.
