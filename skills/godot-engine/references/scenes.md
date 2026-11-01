# Scene operations

Call `operate(project, name, parameters)`; see `execution.md`.
All resource paths use project-relative or `res://` syntax. Operations load files
on disk, instantiate, pack, and save; they do not save the live editor scene.
Never overwrite unsaved user edits. Repacking may change inherited/instanced scenes;
inspect diffs and use a focused task-specific engine script when preservation needs
exceed the upstream helper's capabilities.

## create_scene

Required: `scene_path`. Optional: `root_node_type` (default `Node2D`).
Creates parent directories and a new PackedScene. Check for an existing destination
before invocation; upstream creation is not a safe merge.

```python
operate(project, "create_scene", {
    "scene_path": "Scenes/Example.tscn", "root_node_type": "Node2D"
})
```

## add_node

Required: `scene_path`, `node_type`, `node_name`.
Optional: `parent_node_path` (default `"root"`), `properties` (dictionary).
Parent paths are scene-root-relative; `"root/Actors"` is accepted by upstream.
The new node's owner is set to the scene root so it is packed.

```python
operate(project, "add_node", {
    "scene_path": "Scenes/Example.tscn",
    "node_type": "Sprite2D", "node_name": "Preview",
    "properties": {"visible": True, "z_index": 2}
})
```

Use unique node names and inspect the stored result. JSON supports primitive
properties, not native Godot Vector2/Color/NodePath/Resource values. Do not pass
arrays/dictionaries hoping Godot implicitly converts them to engine types.
For those values, exported C# references, scripts, signals, or complex composition,
use a focused task-specific Godot script with proper types and project conventions.
Custom instantiation supports native classes/GDScript; do not assume C# class support.

## load_sprite

Required: `scene_path`, `node_path`, `texture_path`.
Compatible nodes: Sprite2D, Sprite3D, TextureRect. Empty `node_path` means root;
otherwise use the relative node path, optionally prefixed `"root/"`.
Import the image first. The operation assigns `texture` and repacks the scene.

```python
operate(project, "load_sprite", {
    "scene_path": "Scenes/Example.tscn", "node_path": "Preview",
    "texture_path": "Art/preview.png"
})
```

## save_scene

Required: `scene_path`. Optional: `new_path`.
Reopens and repacks the disk scene; `new_path` saves it elsewhere and creates
the destination directory. This is a disk copy/repack, not an inherited-scene
creation API or a save of unsaved editor changes.

```python
operate(project, "save_scene", {
    "scene_path": "Scenes/Example.tscn", "new_path": "Scenes/ExampleCopy.tscn"
})
```

Verify the affected nodes/properties, ownership, and external references survive.
Run applicable build/tests and the affected scene; inspect errors and visuals.
