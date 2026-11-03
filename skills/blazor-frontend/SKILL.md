---
name: blazor-frontend
description: 'Use when creating or changing Blazor Razor components, forms, loading/empty/error states, component-scoped CSS or JavaScript, render modes, MudBlazor components, bUnit tests, or UI rendering/performance. Covers component architecture, parameters, list identity, virtualization, current documentation, and browser verification. Complements csharp-standards, which owns general C# and service design.'
argument-hint: 'Component, UI state, bUnit test, or rendering scenario'
---

# Blazor Front-End

Read the applicable reference before changing components or component tests.
Use `csharp-standards` for general C#, business services, and non-component tests.
Preserve the project's existing routing, render modes, and asset-loading conventions.

## Routing

| Need | Read |
|---|---|
| Component placement, responsibilities, scoped assets, or render modes | [component-architecture.md](./references/component-architecture.md) |
| Rendering performance, parameters, virtualization, or event handling | [rendering-performance.md](./references/rendering-performance.md) |
| List identity, focus preservation, or `@key` scope | [element-key-relationships.md](./references/element-key-relationships.md) |
| Component rendering, interaction, or async-state tests | [bunit-component-tests.md](./references/bunit-component-tests.md) |

## Current Documentation

Use Context7 before API-dependent changes. Use `/mudblazor/mudblazor` for
MudBlazor and `/websites/bunit_dev` for bUnit; resolve the current ASP.NET Core
Blazor documentation for framework behavior. Reference examples illustrate
patterns, not a guarantee of compatibility with the project's installed versions.

## Verification

Run the project's focused build and affected tests. For every UI change, also
start the app using its documented run command and inspect the affected page in
an available browser tool, such as Playwright. Verify appearance and interactions;
tests alone do not establish visual correctness. Report the listening URL and any
verification gaps.

Stop only the server started for the task, using its exact process/session handle.
Do not terminate unrelated servers to resolve a port conflict or build lock.

## Principles

- Components own rendering and lightweight UI state; business workflows belong
  in services.
- Use `@key` to express item identity, not as a general performance optimization.
- Measure rendering bottlenecks before adding virtualization, suppressing renders,
  or changing component granularity. Do not use fixed item-count or timing thresholds.
- Keep UI-specific CSS and JavaScript beside their component.
