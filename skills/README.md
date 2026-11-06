# Skills

Project-agnostic agent skills I use across my projects. Godot-related skills are
included only in Godot projects, and `blazor-frontend` is only included only in Blazor
projects.

## Testing

Not needed; `aseprite` and `godot-engine` do have their own test suites, but these are just skill ports of MCP servers. `pixel-art-design` has helper tests, and `strudel-music` documents its renderer tests in `references/rendering.md`. Nevertheless, they can be ran as follows:

```powershell
# Aseprite integration tests: set the actual executable path
$env:ASEPRITE_PATH = 'C:\path\to\aseprite.exe'
python -m unittest discover -s .\skills\aseprite\scripts\tests -v

# Godot helper unit/integration tests: use a Godot 4 .NET executable
$env:GODOT_PATH = 'C:\path\to\godot-mono.exe'
python -m unittest discover -s .\skills\godot-engine\scripts\tests -v

# Pixel-art profile/validation helpers (rendering tests skip without ASEPRITE_PATH)
python -B -m unittest discover -s .\skills\pixel-art-design\scripts\tests -p 'test_*.py' -v

# Skill selection and copy-preservation checks
python -m unittest discover -s .\scripts\tests -p test_scripts.py -k test_defaults_preserve -v
python -m unittest discover -s .\scripts\tests -p test_scripts.py -k test_godot_bootstrap -v
```
