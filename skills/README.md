# Skills

Project-agnostic agent skills I use across my projects (except only using the Godot-related skills in Godot projects).

## Testing

Not needed; `aseprite` and `godot-engine` do have their own test suites, but these are just skill ports of MCP servers. Nevertheless, they can be ran as follows:

```powershell
# Aseprite integration tests: set the actual executable path
$env:ASEPRITE_PATH = 'C:\path\to\aseprite.exe'
python -m unittest discover -s .\skills\aseprite\scripts\tests -v

# Godot helper unit/integration tests: use a Godot 4 .NET executable
$env:GODOT_PATH = 'C:\path\to\godot-mono.exe'
python -m unittest discover -s .\skills\godot-engine\scripts\tests -v

# Skill selection and copy-preservation checks
python -m unittest discover -s .\scripts\tests -p test_scripts.py -k test_defaults_preserve -v
python -m unittest discover -s .\scripts\tests -p test_scripts.py -k test_godot_bootstrap -v
```
