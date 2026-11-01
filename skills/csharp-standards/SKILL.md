---
name: csharp-standards
description: 'C# and .NET standards. Use when adding or editing a class, interface, record, or enum; adding or editing any .cs file; designing a service or its public API; writing or changing any production C# code; writing Godot Node scripts, scene wiring, or input handling; adding persistence, entities, or EF migrations; or writing, reviewing, or fixing unit and integration tests. Routes to focused references for each of those tasks. Excludes UI and markup concerns, which belong to the UI framework skill.'
argument-hint: 'The C# task you are about to start'
---

# C# Standards

Read the applicable references before writing code; apply them to new and edited
code. Leave unrelated code alone.

## Routing

| You are about to... | Read |
|---|---|
| Create/edit types or `.cs` files, choose placement, or split a file | [creating-and-editing-types.md](./references/creating-and-editing-types.md) |
| Design service APIs, interfaces, dependencies, or registration | [designing-service-apis.md](./references/designing-service-apis.md) |
| Write method bodies — naming, readability, boundaries, async | [writing-csharp.md](./references/writing-csharp.md) |
| Add an entity, key, query, or EF migration | [persistence.md](./references/persistence.md) |
| Change Godot code, wiring, input, or the engine/core boundary | [godot-development.md](./references/godot-development.md) |
| Choose, write, or review tests | [writing-tests.md](./references/writing-tests.md) |

This skill covers C# only. UI and framework-specific concerns — markup, components,
styling, rendering, and component tests — belong to that framework's own skill.

## Procedure

Use the documented focused build, tests, and validators; otherwise use `dotnet build`
and `dotnet test`. Fix change-related failures without weakening diagnostics or
casually suppressing them. Build/analyzer configuration owns mechanical conventions;
skill references retain design decisions and uncovered behavior.
