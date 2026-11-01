# Dev kit

A collection of the .NET tooling I use across my projects. The skills are meant to be generalized enough to be able to drop in to any .NET project, and the other tooling (scripts, analyzers, etc.) are meant to make it easy to create new projects with the standards I like so I don't have to bootstrap them every time.

## Skills

Common skills I use across my .NET projects. Some are Godot-specific, so I use a script to install the skills, which removes the Godot glossary entries from AGENTS.md if it's not a Godot project.

| Skill | Description |
| --- | --- |
| [aseprite](skills/aseprite/SKILL.md) | This is a skill port of [diivi/aseprite-mcp](https://github.com/diivi/aseprite-mcp). Perform Aseprite commands to create pixel art. Requires Aseprite. |
| [csharp-standards](skills/csharp-standards/SKILL.md) | General C# standards for things like creating types, writing tests, project structure, and more. |
| [feature-specs](skills/feature-specs/SKILL.md) | Creating feature specifications using simple numbered-bullet requirements without technical details that just describe the desired behavior. Made to be easy for a human to scan and avoid specifying details upfront before implementation. In theory, this helps the specs not become outdated and avoids putting too much thought into them before implementation. |
| [godot-engine](skills/godot-engine/SKILL.md) | This is a skill port of [Coding-Solo/godot-mcp](https://github.com/Coding-Solo/godot-mcp). Perform actions with the Godot engine, like run engine commands, and manage projects, scenes, and resources. |
| [godot-playtesting](skills/godot-playtesting/SKILL.md) | In contract to the godot-engine skill, this skill is for showing the AI agent how to run effective playtests and get the information it needs during them to see if things work as expected. Does not replace human testing! |

## Analyzers

Custom Rosyln analyzers (and code fixes where relevant) with some standards I like using.

### General C# analyzers

`Dward.Analyzers` - general C# conventions I like not covered by other existing analyzers.

| Analyzer ID | Description |
| --- | --- |
| `DWARD0003` | Use a typed empty value instead of bare `[]` for an empty collection in NSubstitute sequential returns. |
| `DWARD0004` | Disallow explicit `sealed` modifiers on classes and record classes. AI seems to use sealed by default and I'm not a fan of it. |
| `DWARD0005` | Allow an `Async` suffix on member names only when there's a non-async version with a matching signature. I prefer not to have the `Async` suffix on all `async` methods, so I only use it to tell apart async and sync identical methods. |
| `DWARD0006` | Require descriptive names instead of `sut` or `_sut`. AI seems to like using this convention, and I don't. Disabled by default and must be enabled on purpose since the analyzer can't tell what is a test project and what isn't. |

### Godot analyzers

`Dward.Godot.Analyzers` - same as above, but specific to Godot projects. It also includes a python script, which runs a gdscript, which checks if scene references are set up correctly. This runs on build.

| Analyzer ID | Description |
| --- | --- |
| `DWARDGD0001` | Require Godot `[Export]` fields and properties to be private. If these need to be exposed, a read-only property should be created instead. |
| `DWARDGD0002` | Reject Godot dependencies in projects explicitly marked Godot-free. I like having a Core project with as much game logic as possible that is separate from the actual Godot dependencies. This makes this logic easy to unit test. |
| `DWARDGD0007` | Validates constant input-action names against custom project actions and built-in Godot ones. Ensures the magic string input actions the code calls are actually defined. |
| `DWARDGD0008` | Enforces that there is exactly one `project.godot` file that is configured correctly for input action validation. |

## Templates

.NET projects with the project structure and Nuget packages I typically use.

| Template | Short name | Description |
| --- | --- | --- |
| [Console](templates/AiReadyConsoleApp) | `ai-ready-console` | A console app set up with service hosting, a runner, Serilog, and an xUnit test project. |
| [Library](templates/AiReadyLibrary) | `ai-ready-library` | A class library with an xUnit test project. |
| [MudBlazor](templates/AiReadyMudBlazorApp) | `ai-ready-mudblazor` | A Blazor app with MudBlazor set up and an xUnit test project with bUnit. |

Note: The MudBlazor test project currently specifies AngleSharp 1.5.0 to avoid the vulnerable 1.4.0 dependency selected by bUnit 2.7.2 (GHSA-pgww-w46g-26qg).