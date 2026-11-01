extends SceneTree

var request: Dictionary
var report: Dictionary
var scene: Node
var held_event: InputEventKey
var held_action := ""


func _initialize() -> void:
	var arguments := OS.get_cmdline_user_args()
	if arguments.size() != 1:
		push_error("Expected one recipe request file.")
		quit(1)
		return
	var parsed = JSON.parse_string(FileAccess.get_file_as_string(arguments[0]))
	if not parsed is Dictionary:
		push_error("Invalid recipe request JSON.")
		quit(1)
		return
	request = parsed
	report = {
		"run_id": request.run_id, "scene": request.scene, "complete": false,
		"user_data": OS.get_user_data_dir(), "steps": [],
		"clock": "wait counts physics_frame boundaries; capture waits frame_post_draw",
		"bypasses": ["fresh copied project and empty isolated user://; explicit starting scene"]
	}
	if not _write_report():
		quit(1)
		return
	if OS.get_user_data_dir().replace("\\", "/").get_file() != request.user_name:
		_fail("Save isolation failed; refusing to instantiate the requested scene.")
		_finish(1)
		return
	call_deferred("_run")


func _run() -> void:
	var packed := load(request.scene) as PackedScene
	if packed == null:
		_fail("Could not load scene: %s" % request.scene)
		_finish(1)
		return
	scene = packed.instantiate()
	packed = null
	if scene == null:
		_fail("Could not instantiate scene: %s" % request.scene)
		_finish(1)
		return
	root.add_child(scene)
	current_scene = scene
	await _wait_frames(int(request.get("startup_frames", 2)))
	for step in request.steps:
		if not await _step(step):
			_finish(1)
			return
		if not _write_report():
			_finish(1)
			return
	report.complete = true
	if not _write_report():
		_finish(1)
		return
	print("GODOT_RECIPE_OK")
	_finish(0)


func _wait_frames(count: int) -> void:
	for index in count:
		await physics_frame
	# Give the last physics frame and input callbacks an opportunity to complete.
	await process_frame


func _node(path: String) -> Node:
	if not is_instance_valid(current_scene):
		return null
	return current_scene.get_node_or_null(NodePath(path))


func _step(step: Dictionary) -> bool:
	var entry := {"op": step.op, "physics_frame": Engine.get_physics_frames()}
	report.steps.append(entry)
	match step.op:
		"wait":
			await _wait_frames(int(step.frames))
		"key", "action":
			if not InputMap.has_action(step.action):
				return _fail("Unknown input action: %s" % step.action)
			if step.op == "key":
				for binding in InputMap.action_get_events(step.action):
					if binding is InputEventKey:
						held_event = binding.duplicate() as InputEventKey
						break
				if held_event == null:
					return _fail("No keyboard binding for action: %s" % step.action)
				held_event.pressed = true
				held_event.echo = false
				if held_event.device < 0:
					held_event.device = 0
				held_event.window_id = root.get_window_id()
				entry.action = step.action
				entry.keycode = held_event.keycode
				entry.physical_keycode = held_event.physical_keycode
				entry.device = held_event.device
				Input.parse_input_event(held_event)
				Input.flush_buffered_events()
			else:
				held_action = step.action
				report.bypasses.append("action '%s': Input.action_press bypasses key events/UI input dispatch" % step.action)
				Input.action_press(held_action)
			await _wait_frames(int(step.frames))
			_release_input()
			await _wait_frames(1)
		"click":
			if DisplayServer.get_name() == "headless":
				return _fail("GUI clicks require graphical rendering.")
			var control := _node(step.node) as Control
			if control == null or not control.is_visible_in_tree() or control.size.x <= 0 or control.size.y <= 0:
				return _fail("Click requires a visible, sized Control: %s" % step.node)
			var viewport := control.get_viewport()
			var position := control.get_global_transform_with_canvas() * (control.size / 2.0)
			var motion := InputEventMouseMotion.new()
			motion.position = position
			viewport.push_input(motion, true)
			var button := InputEventMouseButton.new()
			button.button_index = MOUSE_BUTTON_LEFT
			button.position = position
			button.pressed = true
			viewport.push_input(button, true)
			button = button.duplicate() as InputEventMouseButton
			button.pressed = false
			viewport.push_input(button, true)
			entry.node = step.node
			entry.position = [position.x, position.y]
			await _wait_frames(1)
		"assert", "sample", "wait_until":
			if step.op == "wait_until":
				for attempt in range(int(step.frames) + 1):
					if not _observe(step, entry):
						return false
					if entry.value == step.equals:
						entry.end_physics_frame = Engine.get_physics_frames()
						return true
					if attempt < int(step.frames):
						await _wait_frames(1)
				return _fail("Condition timed out: %s.%s expected %s, observed %s" % [
					step.node, step.property, JSON.stringify(step.equals), JSON.stringify(entry.value)])
			if not _observe(step, entry):
				return false
			if step.op == "assert" and entry.value != step.equals:
				return _fail("Assertion failed: %s.%s expected %s, observed %s" % [
					step.node, step.property, JSON.stringify(step.equals), JSON.stringify(entry.value)])
		"capture":
			if DisplayServer.get_name() == "headless":
				return _fail("Capture requires graphical rendering.")
			await RenderingServer.frame_post_draw
			var image := root.get_texture().get_image()
			if image == null or image.is_empty():
				return _fail("Viewport capture is empty.")
			var path := String(request.output).path_join("%s.png" % step.name)
			var error := image.save_png(path)
			if error != OK:
				return _fail("Could not save capture '%s': %s" % [path, error_string(error)])
			entry.file = path
			entry.width = image.get_width()
			entry.height = image.get_height()
			entry.render_frame = Engine.get_process_frames()
		_:
			return _fail("Unknown recipe operation: %s" % step.op)
	return true


func _observe(step: Dictionary, entry: Dictionary) -> bool:
	var target := _node(step.node)
	if target == null:
		return _fail("Missing observation node: %s" % step.node)
	var base_property := String(step.property).split(":")[0]
	var found := false
	for property in target.get_property_list():
		if property.name == base_property:
			found = true
			break
	if not found:
		return _fail("Unknown property '%s' on '%s'" % [step.property, step.node])
	var value = target.get_indexed(NodePath(step.property))
	if not _supported(value):
		return _fail("Unsupported observation type for '%s'; select a scalar subproperty." % step.property)
	entry.node = step.node
	entry.property = step.property
	entry.value = _value(value)
	return true


func _supported(value: Variant) -> bool:
	return typeof(value) in [TYPE_NIL, TYPE_BOOL, TYPE_INT, TYPE_FLOAT, TYPE_STRING, TYPE_STRING_NAME, TYPE_VECTOR2, TYPE_VECTOR3, TYPE_COLOR]


func _value(value: Variant) -> Variant:
	if value is Vector2:
		return [value.x, value.y]
	if value is Vector3:
		return [value.x, value.y, value.z]
	if value is Color:
		return [value.r, value.g, value.b, value.a]
	if value is StringName:
		return str(value)
	return value


func _release_input() -> void:
	if held_event != null:
		held_event.pressed = false
		Input.parse_input_event(held_event)
		Input.flush_buffered_events()
		held_event = null
	if not held_action.is_empty():
		Input.action_release(held_action)
		held_action = ""


func _write_report() -> bool:
	var file := FileAccess.open(String(request.output).path_join("result.json"), FileAccess.WRITE)
	if file == null:
		push_error("Could not write recipe result: %s" % error_string(FileAccess.get_open_error()))
		return false
	file.store_string(JSON.stringify(report, "\t"))
	file.flush()
	if file.get_error() != OK:
		push_error("Could not flush recipe result.")
		return false
	return true


func _fail(message: String) -> bool:
	report.error = message
	_write_report()
	push_error(message)
	return false


func _finish(code: int) -> void:
	_release_input()
	paused = false
	if is_instance_valid(scene):
		scene.free()
	scene = null
	# Let coroutine locals and deferred engine cleanup finish before shutdown.
	await process_frame
	await process_frame
	quit(code)
