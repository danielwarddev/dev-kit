# MeshLibrary export

```python
operate(project, "export_mesh_library", {
    "scene_path": "Scenes/Tiles3D.tscn",
    "output_path": "Resources/Tiles.tres",
    "mesh_item_names": ["Floor", "Wall"]
})
```

Required: `scene_path`, `output_path`. Optional: `mesh_item_names` (list of names).
Import referenced 3D assets first and create the output parent directory explicitly.
The upstream script discovers MeshInstance3D children, optionally filters by names,
and stores mesh/transform/collision information in a MeshLibrary.
This is not arbitrary mesh conversion or a guarantee all scene semantics survive.

Check actual item count/names, transforms, collisions, and use in GridMap.
An empty exported library is not proof of success. Inspect the output visually
in the relevant scene and use a focused engine script for unsupported composition.
