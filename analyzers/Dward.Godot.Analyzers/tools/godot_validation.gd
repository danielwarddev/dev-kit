# This script is gdscript because Godot can run it directly with --script.
# It checks:
# - Unresolved UIDs or missing UID targets (lost UID metadata, missing imports, or moved/deleted assets).
# - Outside-project or nonexistent resource paths (incorrect '../' segments or renamed/moved/deleted files).
# - Scene resource UID/path mismatches (copied references, replaced assets, or stale import metadata).
# - Scenes that cannot load or instantiate (malformed scene text, missing dependencies/imports,
#   broken scripts, or constructor/_init failures).
# - Missing saved nodes or unknown property names (renamed/deleted nodes or exports,
#   a changed attached script, or stale inherited-scene overrides).
# - Unresolved exported Node references (renamed/moved targets, incorrect NodePath assignments,
#   missing node_paths declarations, or incompatible Node types).
# - Final inherited-scene/subscene assignments (overrides can replace valid base references
#   with broken ones; checks use the final value rather than an overridden base value).

extends SceneTree

var failures: Array[String] = []


# Request execution: validate resources and scenes, report all detected violations,
# and exit nonzero on failure so the Python runner can fail the check.
func _initialize() -> void:
	var arguments := OS.get_cmdline_user_args()
	if arguments.size() != 1:
		push_error("Expected one Godot validation request file.")
		quit(1)
		return
	var request = JSON.parse_string(FileAccess.get_file_as_string(arguments[0]))
	if not request is Dictionary:
		push_error("Invalid Godot validation request JSON.")
		quit(1)
		return
	for resource in request.resources:
		validate_resource(resource)
	for path in request.scenes:
		validate_scene(path)
	if not failures.is_empty():
		for failure in failures:
			push_error(failure)
		quit(1)
		return
	print("GODOT_VALIDATION_OK: %s scenes, %s resource references" % [
		request.scenes.size(), request.resources.size()])
	quit(0)


# Resource validation: reject unresolved UIDs, missing UID targets, paths escaping
# the project, missing files/directories, and mismatched ext_resource UID/path pairs.
func validate_resource(reference: Dictionary) -> void:
	var path: String = reference.path
	var location := "%s:%s" % [reference.source, reference.line]
	if path.begins_with("uid://"):
		var id := ResourceUID.text_to_id(path)
		if id == ResourceUID.INVALID_ID or not ResourceUID.has_id(id):
			failures.append("%s: Unresolved UID '%s'. Godot does not know which file this ID belongs to. Check that the asset and its UID/import metadata were preserved, import the project, and update references to renamed or replaced assets." % [location, path])
		elif not FileAccess.file_exists(ResourceUID.get_id_path(id)):
			failures.append("%s: UID '%s' points to a missing file '%s'. Godot knows the path, but the file is gone. Check for a deleted or moved asset; restore it or update its references and import the project." % [location, path, ResourceUID.get_id_path(id)])
		return
	var absolute := ProjectSettings.globalize_path(path).simplify_path()
	var project := ProjectSettings.globalize_path("res://").simplify_path().trim_suffix("/")
	if absolute != project and not absolute.begins_with(project + "/"):
		failures.append("%s: Resource path escapes project: '%s'. res:// paths must stay inside the project folder. Check parent-directory segments such as '../' and move the asset into the project or correct the reference." % [location, path])
		return
	if not FileAccess.file_exists(path) and not DirAccess.dir_exists_absolute(path):
		failures.append("%s: Missing resource '%s'. Check whether the file or directory was renamed, moved, or deleted. Restore it or update every reference to its current project path." % [location, path])
	elif reference.has("uid"):
		var expected := ResourceUID.text_to_id(reference.uid)
		if expected == ResourceUID.INVALID_ID or ResourceLoader.get_resource_uid(path) != expected:
			failures.append("%s: Stale UID '%s' for '%s'. The scene's UID and path do not identify the same asset. Check for a copied reference, replaced asset, or stale import metadata; import the project and reassign the intended resource in the scene." % [location, reference.uid, path])


# Scene loading: resolve serialized wiring in an instance without entering the tree.
# Constructors may run; this does not exercise gameplay or prove required exports exist.
func validate_scene(path: String) -> void:
	var scene := ResourceLoader.load(path) as PackedScene
	if scene == null:
		failures.append("%s: Could not load PackedScene. Godot could not read this scene as a scene resource. Check the engine errors for malformed scene text, missing dependencies, broken scripts, or missing asset imports; confirm the path points to the intended scene." % path)
		return
	# Remain outside the tree: resolve exports without running _Ready/_Process.
	var instance := scene.instantiate()
	if instance == null:
		failures.append("%s: Could not instantiate scene. Godot loaded the scene resource but could not create its node hierarchy. Check the engine errors for broken scripts, constructor/_init failures, and invalid inherited or instanced scenes." % path)
		return
	var assignments: Dictionary = {}
	collect_assignments(scene.get_state(), ".", assignments)
	validate_assignments(assignments, instance, path)
	instance.free()


# Inheritance and instances: merge base/subscene assignments before local overrides
# so a legitimate overridden reference is checked against its final value.
func collect_assignments(state: SceneState, prefix: String, assignments: Dictionary) -> void:
	var base := state.get_base_scene_state()
	if base != null:
		collect_assignments(base, prefix, assignments)
	for index in state.get_node_count():
		var node_path := prefix.path_join(str(state.get_node_path(index))).simplify_path()
		if node_path.is_empty():
			node_path = "."
		var child_scene := state.get_node_instance(index)
		if child_scene != null:
			collect_assignments(child_scene.get_state(), node_path, assignments)
		if not assignments.has(node_path):
			assignments[node_path] = {}
		for property_index in state.get_node_property_count(index):
			var name := state.get_node_property_name(index, property_index)
			assignments[node_path][name] = state.get_node_property_value(index, property_index)


# Scene validation: reject missing serialized nodes, stale/renamed property names,
# and assigned Node references that fail to resolve (including missing node_paths).
# Omitted exports and empty optional assignments are not treated as violations.
func validate_assignments(assignments: Dictionary, root_node: Node, path: String) -> void:
	for node_path in assignments:
		var node := root_node.get_node_or_null(node_path)
		if node == null:
			failures.append("%s: Serialized node '%s' did not resolve. A node path saved in the scene is absent from the loaded hierarchy. Check renamed, moved, or deleted nodes and stale inherited-scene/subscene overrides; update the saved node paths." % [path, node_path])
			continue
		var properties: Dictionary = {}
		for property in node.get_property_list():
			properties[property.name] = property
		for name in assignments[node_path]:
			# Nested properties and metadata have engine-specific indexing semantics.
			if "/" in name or ":" in name:
				continue
			if not properties.has(name):
				failures.append("%s:%s: Unknown serialized property '%s'. The scene saves an assignment to a field/property that no longer exists on this node. Check renamed or removed exports, the attached script, and the current C# build; update the assignment in this scene or its inherited/subscene override." % [path, node_path, name])
				continue
			var value = assignments[node_path][name]
			if value is NodePath and not value.is_empty() and properties[name].type == TYPE_OBJECT:
				var target := node.get_node_or_null(value)
				var actual = node.get(name)
				if target == null or not actual is Node or actual != target:
					failures.append("%s:%s: Export '%s' did not resolve '%s'. The field/property exists, but Godot did not connect it to the intended node. Check renamed or moved target nodes, the NodePath assignment, its node_paths declaration, and the export's compatible Node type. Inherited-scene/subscene overrides are applied before this check; inspect the final assignment, not just the base scene." % [
						path, node_path, name, value])
