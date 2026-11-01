# Provenance, tool coverage, and boundaries

Vendored `scripts\godot_operations.gd` from installed
`@coding-solo/godot-mcp` 0.1.1, repository https://github.com/Coding-Solo/godot-mcp.
Its MIT license is included at the skill root. The engine script is adapted;
the MCP registration/server and Node dependencies are not bundled.

| Former MCP tool | Replacement |
|---|---|
| launch_editor | Direct `--editor --path` command |
| run_project | Direct `--debug --path` command in an attached session |
| get_debug_output | Read the owned session's output or capture files |
| stop_project | Stop the exact owned game session/process |
| get_godot_version | `--version` |
| list_projects | Direct-import filesystem helper |
| get_project_info | File counts/settings inspection and engine version |
| create_scene | Bundled `create_scene` |
| add_node | Bundled `add_node` |
| load_sprite | Bundled `load_sprite` |
| export_mesh_library | Bundled `export_mesh_library` |
| save_scene | Bundled `save_scene` |
| get_uid | Bundled sidecar reader; limitations documented |
| update_project_uids | Bundled `resave_resources`; limitations documented |

The inspected MCP has no live editor socket/plugin/EditorInterface integration.
It starts processes, captures stdout/stderr, and modifies files via headless runs.
This replacement does not claim new live-editor functionality.

The Python helper adds required-parameter/path validation, timeout/error checking,
and success confirmation. Project-info output uses actual file counts rather than
the original heuristics. Run lifecycle uses explicit task-owned handles rather than
the original single-process state/automatic prior-run termination. Output can be
retained after exit rather than disappearing with the MCP's active-process reference.

Scratch-project verification exposed invalid root self-ownership and leaked
instantiated nodes in upstream scene operations. The bundled copy removes the
self-ownership assignment and frees operation-owned roots before normal exit.

Engine-script limitations remain: JSON property types, resource repacking,
sidecar-only UID reading, and resource-type/language scope. References explicitly
direct the agent to targeted engine scripts when the helper cannot safely perform
the task. No blanket parity claim is made for unsupported engine workflows.
