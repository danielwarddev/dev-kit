# UIDs and imports

## get_uid

```python
data = operate(project, "get_uid", {"file_path": "Scripts/Example.gd"})
print(data)
```

This retained upstream operation reads the file's `.uid` sidecar. It returns
`exists`, and `uid` when found. **It is not a universal resource UID query.**
Scenes/resources can have embedded/imported IDs without a sidecar. An `exists:false`
result means no readable sidecar was found, not that the resource has no UID.
Upstream also conflates unreadable and missing sidecars; inspect filesystem errors
if an expected sidecar exists. Do not silently replace references with guessed IDs.

Use installed-version ResourceLoader UID APIs in a targeted engine script when
an actual resource UID rather than sidecar content is needed.

## update_project_uids replacement

The original tool invoked `resave_resources`, not a live editor operation:

```python
print(operate(project, "resave_resources", {"project_path": "Scripts"}))
```

`project_path` is an optional resource subdirectory (default project root), NOT
the absolute project filesystem directory. Empty parameters `{}` are supported.
It recursively resaves scenes and scripts/shaders missing sidecars.
Use a narrowly scoped directory when possible; bulk resaves can alter many files.
Review diffs and backup valuable inputs. This is not a safe automatic migration
of every reference and does not cover C# scripts.

Godot 4.4+ introduced script UID changes, but exact support depends on resource/
language/version. Import using the appropriate editor/.NET engine and let Godot
generate UIDs. Verify sidecars/references actually exist after resaving, not just
the upstream completion text. For C#, follow the project's import/build workflow.

Use `--headless --editor --path <project> --import` when imports are needed; never
hand-edit `.godot` caches. Do not run bulk resaves concurrently with the live editor
saving unsaved work.
