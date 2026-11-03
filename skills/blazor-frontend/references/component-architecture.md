# Component Architecture

Read this before creating a component, deciding where it goes, or deciding what belongs
inside it.

## Keep Business Logic Out of Components

Components own rendering and lightweight UI state. Presentation logic in a component is
fine; business rules and integration workflow logic are not — those belong in
injectable services.

When you find business logic in a `.razor` file, extract it into a service rather than
leaving it inline "just this once." A component that is hard to test usually has logic
in it that should have been a service.

## Where Components Go

- Routed pages go in `Components\Pages\`.
- Shared layout and chrome go in `Components\Layout\`.
- Everything else goes in a feature folder under `Components\`. Match the existing
  feature folders rather than inventing a parallel structure.

## Colocated Component Assets

UI-specific styling and JavaScript belong beside the component:

```
ConfirmDialog.razor
ConfirmDialog.razor.css
ConfirmDialog.razor.js
```

Load the JS module through `@Assets[...]`. Reserve the global stylesheet
(`wwwroot\app.css`) for app-wide styles only — not for styling one component.

## Render Modes Are Opt-In

Interactive behavior is opt-in per component. Apply `@rendermode InteractiveServer` at
the component or page level rather than globally, and use `[StreamRendering]` only when
progressive server rendering is actually intended.

## Splitting a Large Component

A component that has grown unwieldy is doing too much. Split along a real seam:

- A distinct piece of UI that can become a child component.
- Business logic that can move to a service.
- Repeated markup that can become a `RenderFragment`.

Split along the seam, not down the middle. See
[rendering-performance.md](./rendering-performance.md) for when *not* to extract a
child component.
