# Templates

Custom .NET templates for new projects set up with the structure and packages that I prefer.

## Testing

Requires Python 3.10+ and the .NET 10+ SDK. The test script installs the templates in a temporary location and:

- Verifies package references
- Restores/builds projects
- Runs their tests
- Probes analyzer diagnostics and configuration overrides
- Checks that config, skills, machine-specific feed settings, and build artifacts are not embedded in the scaffold
