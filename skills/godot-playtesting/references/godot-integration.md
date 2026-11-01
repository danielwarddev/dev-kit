# Godot integration and portability

## Before writing runtime code

Identify the actual project root, engine version, language, existing logging/input/
test facilities, and coding standards. Do not hardcode paths from the project in
which this skill was authored. Respect project instructions and user-owned state.

Use the project's C#/GDScript standards. This skill supplies no mandatory runtime
implementation or API signatures. Generate/adapt the smallest component needed;
query current Godot docs before using unfamiliar engine APIs or version behavior.

When Context7 is available, query it first using `/godotengine/godot-docs` as
required by this repository. If unavailable or that ID cannot be resolved, use
official docs matching the installed engine version and state relevant uncertainty.
Do not assume the moving `stable` docs match the installed engine.

## Timing semantics

Godot distinguishes variable-rate idle/render processing from fixed-rate physics
processing. Physics defaults to 60 ticks per second, but project settings may
change it. Do not assume "hundreds of frames" means hundreds of physics ticks.
Physics and presentation can advance differently, including during pause or under
time scaling.

Define the observer's clock and phase rather than incrementing independent counters
in every entity. Record relevant settings/changes. A shared tick number does not
guarantee samples were taken after all mutations for that tick.
Check callback/process ordering before interpreting a collector as end-of-tick state.
Do not change controller scheduling or physics settings merely to simplify logging.

## Execution and ownership

CLI, existing test harnesses, or Godot MCP can all launch a bounded observation run.
The evidence format does not depend on MCP. Prefer existing project workflows.
Confirm the requested scene/project actually ran and emitted the expected run
identity; stale files from an earlier run are not current evidence.

Use explicit start/stop/timeout conditions. Keep interactive rendering when
checking presentation or input focus; headless evidence is narrower.
Stop only the run started for the task, using its exact session/process handle.
Never close or terminate the user-owned Godot editor or unrelated game processes.

Flush and finalize traces at controlled shutdown when possible. Detect missing
finalization after forced shutdown. Do not claim success from an abruptly truncated run.
Keep trace output outside shipping assets and clean only known temporary artifacts.

## References

- Processing: https://docs.godotengine.org/en/stable/tutorials/scripting/idle_and_physics_processing.html
- Command line: https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html
- Debugging: https://docs.godotengine.org/en/stable/tutorials/scripting/debug/index.html

These URLs are entry points; select the installed-version documentation when
implementing behavior that depends on engine specifics.
