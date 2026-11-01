# Project Planning Notes

Use this file only for active implementation planning that should persist across Claude sessions.

## Current baseline

- App project: `AiReadyMudBlazorApp\`
- Test project: `AiReadyMudBlazorApp.UnitTests\`
- Solution file: `AiReadyMudblazorApp.slnx`
- Runtime: .NET 10 Blazor Web App with Interactive Server rendering
- UI stack: MudBlazor plus Bootstrap assets

## Planning conventions

- Keep plans focused on the current feature or bug fix.
- Prefer small, reviewable implementation slices that include their matching tests.
- Update this file when a long-running plan changes materially.
- Remove stale feature-specific notes once they no longer describe active work.

## Verification

- Run `dotnet build AiReadyMudblazorApp.slnx` for code changes when tests are not affected.
- Run `dotnet test AiReadyMudblazorApp.slnx` whenever tests are added or changed.
- For UI changes, start the dev server with `bash .claude/scripts/restart-dev.sh` and verify the affected flow in the browser at `http://localhost:5000`.
