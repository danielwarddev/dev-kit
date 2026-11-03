# Component Architecture

Components own rendering and lightweight UI state. Business rules and integration
workflows belong in services; use `csharp-standards` for their design.

## Placement and Assets

- Routed pages go in `Components\Pages\`; shared layout and chrome go in
  `Components\Layout\`. Other components belong in existing feature folders under
  `Components\`, following the project's structure.
- Colocate component-specific assets as `.razor.css` and `.razor.js`. Reserve
  global stylesheets for app-wide styles.
- Follow the project's asset and JS-module loading conventions; do not introduce
  a parallel loading mechanism.

## Rendering Strategy

Preserve the project's chosen render strategy, whether interactivity is configured
globally or per page/component. Change render boundaries deliberately; interactive
controls and their providers must be in a compatible interactive scope.
Use streaming rendering only when progressive server output is intended.

## Component Boundaries

Extract a child component for a cohesive UI responsibility, not an arbitrary size
limit. Repeated markup can use a `RenderFragment` when independent component state
and rendering are unnecessary. See [rendering-performance.md](./rendering-performance.md)
before changing granularity for performance.
