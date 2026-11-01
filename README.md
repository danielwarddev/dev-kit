# Dev kit

A collection of the .NET tooling I use across my projects. The skills are meant to be generalized enough to be able to drop in to any .NET project, and the other tooling (scripts, analyzers, etc.) are meant to make it easy to create new projects with the standards I like so I don't have to bootstrap them every time.

## Skills

| Skill | Description |
| --- | --- |
| [aseprite](skills/aseprite/SKILL.md) | This is a skill port of [diivi/aseprite-mcp](https://github.com/diivi/aseprite-mcp). Perform Aseprite commands to create pixel art. Requires Aseprite. |
| [csharp-standards](skills/csharp-standards/SKILL.md) | General C# standards for things like creating types, writing tests, project structure, and more. |
| [feature-specs](skills/feature-specs/SKILL.md) | Creating feature specifications using simple numbered-bullet requirements without technical details that just describe the desired behavior. Made to be easy for a human to scan and avoid specifying details upfront before implementation. In theory, this helps the specs not become outdated and avoids putting too much thought into them before implementation. |
| [godot-engine](skills/godot-engine/SKILL.md) | This is a skill port of [Coding-Solo/godot-mcp](https://github.com/Coding-Solo/godot-mcp). Perform actions with the Godot engine, like run engine commands, and manage projects, scenes, and resources. |
| [godot-playtesting](skills/godot-playtesting/SKILL.md) | In contract to the godot-engine skill, this skill is for showing the AI agent how to run effective playtests and get the information it needs during them to see if things work as expected. Does not replace human testing! |

## Analyzers

The [analyzer solution](analyzers/Dward.Analyzers.sln) contains the analyzers, code
fixes, regression tests, and packaged Godot validation tools.

| Package | Contents |
| --- | --- |
| `Dward.Analyzers` | General C# rules, code fixes, and explicitly opted-in test naming configuration. |
| `Dward.Godot` | Godot export, dependency-boundary, and input-action rules; export code fixes; opt-in scene/resource validation. |

Both packages are version **1.0.0**. They add no application runtime dependencies.
The packages remain local/private; ordinary builds do not publish them or apply
code fixes.

### Build and test

Use the .NET 10 SDK. From the repository root:

```powershell
dotnet test .\analyzers\Dward.Analyzers.sln
python -m unittest discover -s .\analyzers\tools\tests
```

The second command requires Python 3 and runs packaging/consumer checks as well as
validation-runner tests. Run the .NET command first to restore the projects.
Set `GODOT_PATH` to a Godot .NET executable to include engine-backed validation
tests; otherwise those tests are skipped. Analyzer and code-fix tests do not launch
Godot.

### Local packages

Pack explicitly to a machine-local feed:

```powershell
$feed = Join-Path $env:USERPROFILE '.nuget\local-feed\Dward'
dotnet pack .\analyzers\Dward.Analyzers\Dward.Analyzers.csproj -c Release -o $feed
dotnet pack .\analyzers\Dward.Godot.Analyzers\Dward.Godot.Analyzers.csproj -c Release -o $feed
```

On a new machine, register that directory once with
`dotnet nuget add source $feed --name DwardLocal`. If package source mapping is
enabled, map `Dward.*` to that source. Keep feed paths and credentials in user-level
NuGet configuration, not generated projects. Do not commit package archives or
NuGet caches.

Reference either package using `PrivateAssets="all"`:

```xml
<PackageReference Include="Dward.Analyzers" Version="1.0.0" PrivateAssets="all" />
<PackageReference Include="Dward.Godot" Version="1.0.0" PrivateAssets="all" />
```

Existing rules are warnings by default, except `DWARD0006`, which is disabled by
default. Severity remains configurable through `.editorconfig`.

To opt a whole test project into the bundled naming configuration, enable the
package path property and explicitly include the configuration:

```xml
<PackageReference Include="Dward.Analyzers" Version="1.0.0"
                  PrivateAssets="all" GeneratePathProperty="true" />
<GlobalAnalyzerConfigFiles Include="$(PkgDward_Analyzers)\build\Dward.Tests.globalconfig" />
```

For Godot-free Core projects, set `<GodotFreeProject>true</GodotFreeProject>`.
For Godot projects, add `<AdditionalFiles Include="project.godot" />` to enable
input-action checks. Scene/resource validation stays off unless
`<RunGodotValidation>true</RunGodotValidation>` is set in the Godot project's
property group. Do not enable it globally for a solution containing Core projects.
Validation requires Python 3 and Godot, configured through `GODOT_PATH`, `GODOT_BIN`,
or the `GodotExecutable` MSBuild property.

After restoring a consuming project, explicitly apply supported fixes with
`dotnet format analyzers`, selecting diagnostic IDs and changed files. For example:

```powershell
dotnet format analyzers .\MyProject\MyProject.csproj --diagnostics DWARD0004 --include .\MyProject\Subject.cs --no-restore
```

`DWARD0003`, `DWARD0004`, and `DWARDGD0001` support automated fixes.
The `DWARD0005` rename is review-only and does not support Fix All.
Inspect the resulting diff and rebuild after applying a fix.

The initial 1.0.0 packages were refreshed during this migration to include code
fixes. Use new package versions for subsequent releases; rebuilding an existing
version does not invalidate copies cached on other machines.
