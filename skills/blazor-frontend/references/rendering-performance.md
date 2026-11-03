# Rendering Performance

Measure the affected interaction before optimizing. Identify whether the cost is
DOM size, component rendering, parameter processing, or event frequency; do not
apply fixed item-count or timing thresholds.

Use Context7 for current APIs and examples from the
[rendering](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/rendering)
and [virtualization](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/virtualization)
documentation.

## Avoid Unnecessary Rendering of Component Subtrees

- Unchanged parameters of framework-recognized immutable types can avoid
  parameter-driven rerenders. Custom immutable models and `RenderFragment` values
  are not automatically recognized as unchanged.
- Use `ShouldRender` only when a measured expensive subtree can safely skip updates.
  Track all state affecting its output; an incomplete comparison leaves stale UI.
- Bundling parameters can reduce parameter-processing cost but lose automatic
  change detection. Measure the trade-off rather than always bundling.
- Set `CascadingValue.IsFixed` only when its supplied value stays fixed; this avoids
  change subscriptions but is inappropriate for values that need replacement updates.

## Virtualization

Use `Virtualize` when rendering offscreen list items is a measured bottleneck.
It is not a general solution for arbitrary grids or variable-height content.

- Items and placeholders must have uniform height and form a single vertical stack.
  Preserve spacer sizing and prevent flex layouts from shrinking rows.
- Match item-size configuration to actual rendered height; tune overscan against
  scrolling smoothness and rendering cost, not copied defaults.
- Use spacer elements compatible with the container, especially inside tables.
- Ensure the scroll container or virtualized elements are focusable for keyboard
  scrolling; verify this in the browser.
- For on-demand data, honor provider cancellation and handle loading/empty states.
  Follow current refresh and rendering APIs when external data changes.

## Component Granularity

Repeated component instances have overhead, but child components also isolate state
and rendering. Inline markup or use `RenderFragment` only when measurement justifies
losing those independent boundaries. Avoid expensive attribute splatting in hot
repeated components; retain it where its flexibility is useful.

## Events and Delegates

- Throttle or debounce high-frequency browser events before forwarding every event
  into .NET, particularly with server interactivity.
- Suppress event-triggered rendering only when no visible state changes; preserve
  normal exception handling and error-boundary behavior.
- Reuse delegates in hot loops when allocation is a demonstrated cost, while
  preserving the correct item binding and callback lifetime.
