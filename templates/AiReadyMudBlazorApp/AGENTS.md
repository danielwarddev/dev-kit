# AiReadyMudBlazorApp

## General

- Whenever you make a code change, you MUST run `dotnet test` (or `dotnet build` if no tests were affected) to ensure the changes work as expected. If they don't pass, continue making changes until they do pass.
- If a build fails because the running dev server is locking `AiReadyMudBlazorApp.dll` or `AiReadyMudBlazorApp.exe`, kill the process yourself and retry the build. Do not ask the user first.
- To kill, rebuild, and restart the dev server, run `bash .claude/scripts/restart-dev.sh`. This script finds and kills any running `AiReadyMudBlazorApp.exe`, runs `dotnet build`, then starts the server in the background with logs at `.claude/app-logs/server.log`. This command is pre-approved and does not require a permission prompt.
- Whenever you start or restart the dev server, always include the URL (http://localhost:5000) in your response to the user so they can open it directly without needing to run `dotnet run` themselves.
- Tests are not afterthoughts. Use them early to flesh out design and ensure the code works as expected.
- Do not add DI wiring tests that only verify service registrations or framework configuration. Prefer behavior-focused tests and rely on build/runtime validation unless the DI setup contains application logic worth testing directly.

## High-level architecture

- This repository is a .NET 10 Blazor Web App with the app in `AiReadyMudBlazorApp\` and automated tests in `AiReadyMudBlazorApp.UnitTests\`.
- `AiReadyMudBlazorApp\Program.cs` is the composition root. It enables Razor Components with Interactive Server support, registers MudBlazor services, configures Serilog, enables production error handling plus 404 rewriting, and maps the root `App` component.
- `AiReadyMudBlazorApp\Components\App.razor` owns the HTML shell and shared assets. It loads Bootstrap, MudBlazor, `wwwroot\app.css`, the generated component stylesheet, `blazor.web.js`, and renders the global `ReconnectModal`.
- `AiReadyMudBlazorApp\Components\Routes.razor` is the routing hub. It routes page components from `Components\Pages\` and applies `Components\Layout\MainLayout` as the default layout.
- Styling and browser-side behavior are split between global assets in `wwwroot\` and component-scoped assets colocated with the component. `ReconnectModal` is the clearest example: `.razor`, `.razor.css`, and `.razor.js` live together and the JS module is loaded through `@Assets[...]`.

## Key conventions

- Do not make classes `sealed` or members `internal`.
- Keep routed pages under `AiReadyMudBlazorApp\Components\Pages\` and shared layout/chrome under `AiReadyMudBlazorApp\Components\Layout\`.
- Keep `Program.cs` as the place where new services, SDK wrappers, and HTTP pipeline behavior are registered.
- Use `int` for primary/foreign keys; do not introduce `Guid` IDs for those persistence flows.
- Treat interactive behavior as opt-in per component. `@rendermode InteractiveServer` is applied at the component/page level, and `[StreamRendering]` is used explicitly when progressive server rendering is intended.
- Keep Razor components focused on rendering and lightweight UI state. Put integration workflow logic and business rules in injectable services rather than directly in `.razor` files.
- Keep small feature-specific interfaces colocated with their implementations unless the pair grows enough to justify splitting.
- Keep application services, repositories, and feature code in the main web project unless a dedicated class library already exists or the repo explicitly needs one.
- Add committed EF migrations only when the repo actually needs them.
- Preserve the current not-found flow. The app rewrites unknown routes to `/not-found` with `UseStatusCodePagesWithReExecute`, and `Components\Pages\NotFound.razor` is the matching UI endpoint.
- Prefer colocated component assets for UI-specific styling or JS (`Component.razor`, `Component.razor.css`, `Component.razor.js`) and keep `wwwroot\app.css` for app-wide styles only.
- Repository-local skill docs under `.claude\skills\` are workflow guidance for this repo. If a skill drifts from the actual codebase, update the skill text and use the current project structure as the source of truth.

## Library Documentation

When working with any of the following libraries, query the Context7 MCP server with the listed library ID to get current API docs before writing or changing code:

| Library | Context7 library ID |
|---------|-------------------|
| MudBlazor | `/mudblazor/mudblazor` |
| bUnit | `/websites/bunit_dev` |

## Skills

Read the relevant skill file before starting work in these areas:

| Task | Skill file |
|------|-----------|
| Creating or implementing a spec | `.claude\skills\feature-specs\SKILL.md` |
| Writing or reviewing C# code, tests, or project structure | `.claude\skills\csharp-standards\SKILL.md` |
