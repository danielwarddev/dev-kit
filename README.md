# Dev kit

A collection of the custom .NET tooling I use across my projects. This tooling is both the standards I like (such as project template structure and Roslyn analyzers), as well as my AI instructions (skills and `AGENTS.md`), so that I don't have to bootstrap projects manually the same way over and over.

## Requirements

- Python 3.10+ (for install script)
- .NET 10+ SDK
- Git
- Bash (as well as `tar` / `find` and `cygpath`; Git for Windows supplies all these)
- Godot .NET (optional)
- GitHub CLI (optional)
- Aseprite (optional)

## Installation

```bash
bash install
```

`install` runs `install.py`, which sets up everything in this repo on your local machine:

- Creates a local folder (configurable; default is `~/OneDrive/CustomScripts/dward-dev-kit`) where everything will be "deployed" to so that it does not depend on the local repo
- Adds that folder to your PATH, `.bashrc`, and your Bash login profile
- Places everything except analyzers there - .NET templates, scripts, skills, and other files used by projects (like `.editorconfig` and `AGENTS.md`)
- Installs the .NET templates
- For the analyzers, sets up a local Nuget feed at ~/.nuget/local-feed/Dward. The analyzers are packed and put there and `DwardLocal` is registered as a Nuget source

### Arguments

| Argument | Default | Description |
| --- | --- | --- |
| `-h`, `--help` | | Show installer usage and exit without installing. |
| `--custom-scripts <directory>` | Windows: `~/OneDrive/CustomScripts`; other platforms: `~/CustomScripts` | Parent directory for the deployed `dward-dev-kit` folder. |
| `--feed <directory>` | `~/.nuget/local-feed/Dward` | Local directory where analyzer NuGet packages are installed. |
| `--nuget-config <file>` | Windows: `%APPDATA%\NuGet\NuGet.Config`; other platforms: `~/.nuget/NuGet/NuGet.Config` | NuGet configuration to register the local source and package mapping in. Custom locations must also be supplied to restores unless they are in the normal NuGet configuration hierarchy. |
| `--profile <file>` | `.bashrc` plus the first existing `.bash_profile`, `.bash_login`, or `.profile`; creates `.bash_profile` if none exist | Bash profile to update; repeat this argument to update multiple files. |
| `--no-user-path` | Windows user PATH is updated | Skip Windows user PATH changes. Bash profiles are still updated. |

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
| `DWARDGD0003` | Validates constant input-action names against custom project actions and built-in Godot ones. Ensures the magic string input actions the code calls are actually defined. |
| `DWARDGD0004` | Enforces that there is exactly one `project.godot` file that is configured correctly for input action validation. |

## Templates

.NET projects with the project structure and Nuget packages I typically use.

| Template | Short name | Description |
| --- | --- | --- |
| [Console](templates/AiReadyConsoleApp) | `ai-ready-console` | A console app set up with service hosting, a runner, Serilog, and an xUnit test project. |
| [Library](templates/AiReadyLibrary) | `ai-ready-library` | A class library with an xUnit test project. |
| [MudBlazor](templates/AiReadyMudBlazorApp) | `ai-ready-mudblazor` | A Blazor app with MudBlazor set up and an xUnit test project with bUnit. |

Note: The MudBlazor test project currently specifies AngleSharp 1.5.0 to avoid the vulnerable 1.4.0 dependency selected by bUnit 2.7.2 (GHSA-pgww-w46g-26qg).

## Shared configuration

There are some files that projects or solutions share and should always be copied over, in addition to the agent skills. The `apply-project-files` script below brings these over into a project (in addition to the agent skills).

| File | Description |
| --- | --- |
| `.editorconfig` | C# preferences.`DWARD0006` is on for test projects. |
| `Directory.Build.props` | Always treat warnings as errors. |
| `AGENTS.md` | AI instructions; self-explanatory. |
| `CLAUDE.md` | Just points to `@AGENTS.md`, if using Claude Code. |

## Scripts

Bash scripts for general helpfulness, and also to be used in conjunction with the .NET templates to bring in everything I want for the projects. Running a script will  create a new project using one of the .NET templates, update all the Nuget packages to the latest stable version, and bring over the `.editorconfig` file, `Directory.Build.props` file, and agent instructions (skills and `AGENTS.md`).

| Script | Arguments | Description |
| --- | --- | --- |
| `ghinit` | `[--localOnly] [--private] [--blog]` | Initializes a new git repo, adds a .gitignore, pushes to a new GitHub repo, and sets a relevant README if it's for one of my blog posts. Also used by `godotcsharp` (see below section). |
| `new-ai-ready-app` | `-t <template> -n <name>` | The shared implementation behind the below row. Creates a new project from a .NET template then runs the `update-packages` and `apply-project-files` scripts. |
| `new-ai-ready-<apptype>-app` | | Small scripts which just set which .NET template to use and then call `new-ai-ready-app`. |
| `apply-project-files` | `<directory> [--godot]` | Copies `.editorconfig`, `Directory.Build.props`, `AGENTS.md`, `CLAUDE.md`, and the relevant agent skills into the project. |
| `update-packages` | `[directory]` | Runs `dotnet package update --project` on `.csproj` files in the directory, which updates package sto their latest stable version, regardless of their version in the .NET template. |


```bash
bash scripts/new-ai-ready-console-app -n MyApp -o "$HOME/projects"
```

### Godot

The `godotcsharp` script is slightly different than the other, as it does not use a .NET template. Instead, it creates the typical C# project that Godot will create for you, but updates it to be more to my liking. It:

- Adds it to a .NET solution
- Moves `project.godot` into the Godot project's folder
- Adds gdUnit4 to the Godot project
- Creates a `Core` project (intended to contain game logic without Godot dependencies for easy testing)
- Creates an xUnit test project for the `Core` project

```bash
# --bootstrap creates a new project in a new dir for you and puts the game in it
godotcsharp --bootstrap MyCoolNewGame
```
