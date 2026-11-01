# .NET Project

## General

- Whenever you make a code change, you MUST run `dotnet test` (or `dotnet build` if no tests were affected) to ensure the changes work as expected. If they don't pass, continue making changes until they do pass.
- Tests are not afterthoughts. They are to be used first in the development process to flesh out design and ensure the code is working as expected.
- Do not add DI wiring tests that only verify service registrations or framework configuration. Prefer behavior-focused tests and rely on build/runtime validation unless the DI setup contains application logic worth testing directly.

## High-level architecture

- This repository is a .NET solution organized into a main application project and a sibling unit-test project, with shared build settings in `Directory.Build.props`.
- `Directory.Build.props` enforces `TreatWarningsAsErrors`, so any new warning must be resolved before the build passes.
- The solution file is `.slnx`-formatted at the repo root. New projects should be added to that solution file rather than introducing a separate `.sln`.

## Key conventions

- Do not make classes `sealed` or members `internal`.
- Keep the application's composition root (e.g., `Program.cs`) as the place where services and pipeline behavior are registered.
- Use `int` for primary/foreign keys; do not introduce `Guid` IDs for those persistence flows.
- Keep small feature-specific interfaces colocated with their implementations unless the pair grows enough to justify splitting.
- Repository-local skill docs under `.claude\skills\` are workflow guidance for this repo. If a skill drifts from the actual codebase, update the skill text and use the current project structure as the source of truth.

## Skills

Read the relevant skill file before starting work in these areas:

| Task | Skill file |
|------|-----------|
| Creating or implementing a spec | `.claude/skills/feature-specs/SKILL.md` |
| Writing or reviewing C# code, tests, or project structure | `.claude/skills/csharp-standards/SKILL.md` |
