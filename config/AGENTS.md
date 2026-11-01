# Project Guidance

## Development

- Read the applicable skills before changing code.
- Use focused builds and tests to validate changes. Fix change-related failures
  without weakening diagnostics or adding suppressions.
- Prefer behavior-focused tests; do not test framework configuration or DI
  registration alone.
- Keep changes scoped to the requested work and preserve unrelated local edits.
- Keep credentials and machine-specific paths out of source control.
- Stop only task-owned processes by exact process ID. Never kill processes by name.

## Project structure

- Application or library code and its sibling `.UnitTests` project belong in the
  existing solution. Preserve its solution format and shared build settings.
- `Directory.Build.props` treats warnings as errors. `.editorconfig` owns naming,
  style, and diagnostic severity; skills cover design decisions.
- Keep the composition root in `Program.cs` for executable projects. Keep library
  APIs intentional and supporting types close to their consumers.
- For Blazor projects, keep rendering and lightweight UI state in Razor components
  and business workflows in services. Preserve existing routing, render modes,
  error handling, and colocated component assets.
- Use the actual project and launch settings when running an app; report the
  listening URL rather than assuming a fixed port.

## Skills

| Task | Skill file |
| --- | --- |
| Creating or implementing a spec | `.claude/skills/feature-specs/SKILL.md` |
| Writing or reviewing C# code, tests, persistence, or project structure | `.claude/skills/csharp-standards/SKILL.md` |

## Library documentation

Use current library documentation before API-dependent changes. When Context7 is
available, use `/mudblazor/mudblazor` for MudBlazor and `/websites/bunit_dev` for
bUnit when those libraries are present.
