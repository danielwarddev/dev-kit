# Project discovery and information

Version: invoke `$godot --version`, or `run_engine(["--version"])`.
Actual engine version/build matters, particularly for C# and UID operations.

Direct imports:

```python
from operations import list_projects, project_info

print(list_projects(r"C:\repos\gamedev", recursive=True))
print(project_info(r"C:\path\to\project"))
```

Default discovery includes the selected directory and immediate child projects.
Recursive discovery skips hidden/generated directories and directory symlinks.
Use only an explicitly scoped directory; do not recursively scan a user's home
or filesystem to find projects.

`project_info` returns project path, engine version, actual scene/script/asset/other
file counts, and the settings-file location. This is not live scene-tree inspection.
Inspect `project.godot` selectively for name, main scene, input, or relevant settings.
Use glob/search tools to locate actual scenes/scripts when needed; do not dump the
whole project tree or every asset.

The replacement output is intentionally not byte-identical to the MCP's project
information: the MCP used directory-name heuristics and a default-name fallback.
The helper reports counts and the authoritative settings location explicitly.
Filesystem-read or executable errors propagate rather than masquerading as empty data.
