# Scripts

Bash helpers creating the .NET templates and adding some additional files and updating Nuget packages.

## Testing

The `tests/` folder contains Python test scripts. Real starter checks access NuGet feeds and may take
several minutes. Godot setup checks require `godot-mono` on PATH.

```powershell
# Project-script tests only
python -m unittest discover -s .\scripts\tests -p test_scripts.py -v

# Installer tests only
python -m unittest discover -s .\scripts\tests -p test_install.py -v

# Both suites
python -m unittest discover -s .\scripts\tests -v
```

Project script tests create temporary generated projects to run against. They build projects and invoke their tests, and cover:

- Config/skill preservation,
- Package updates
- Failure propagation
- Local Git initialization
- Godot bootstrap/restructure/rerun behavior (skipped when Godot is unavailable)

Installer script tests use isolated feeds, Nuget configs, profiles, and .NET and CLI homes, and also disable Windows user PATH changes. They cover:

- Fresh/partial/repeated installation
- Snapshot independence
- PATH/profile deduplication
- Unmanaged-folder protection
- Package comparison
- Consumer restore/build. They use isolated feeds, NuGet configs, profiles, and .NET

