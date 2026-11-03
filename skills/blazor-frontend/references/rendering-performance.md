# Blazor Rendering Performance Best Practices

Reference extracted from [ASP.NET Core Blazor rendering performance](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/rendering?view=aspnetcore-10.0) and [Blazor component virtualization](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/virtualization?view=aspnetcore-10.0).

---

## Avoid Unnecessary Rendering of Component Subtrees

When an event fires, Blazor re-renders the handler's component, then supplies new parameter values to every child. Each child re-renders if `ShouldRender()` returns `true` (the default) and the parameters *may* have changed. This recurses down the entire subtree.

### Strategy 1: Use Immutable Parameter Types

Blazor's built-in change detection automatically skips re-rendering when immutable parameter values haven't changed. Supported immutable types include `string`, `int`, `bool`, `DateTime`, and other primitives.

```razor
@* CustomerId is int — Customer only re-renders when the value changes *@
<Customer CustomerId="item.CustomerId" />
```

### Strategy 2: Override `ShouldRender`

When parameters are complex/mutable types or `RenderFragment` values that haven't logically changed, override `ShouldRender` to return `false`.

```csharp
@code {
    private int prevInboundFlightId = 0;
    private int prevOutboundFlightId = 0;
    private bool shouldRender;

    [Parameter]
    public FlightInfo? InboundFlight { get; set; }

    [Parameter]
    public FlightInfo? OutboundFlight { get; set; }

    protected override void OnParametersSet()
    {
        shouldRender = InboundFlight?.FlightId != prevInboundFlightId
            || OutboundFlight?.FlightId != prevOutboundFlightId;

        prevInboundFlightId = InboundFlight?.FlightId ?? 0;
        prevOutboundFlightId = OutboundFlight?.FlightId ?? 0;
    }

    protected override bool ShouldRender() => shouldRender;
}
```

---

## Virtualization

Use `Virtualize<TItem>` when rendering large lists or grids. It only renders items in the current scroll viewport, drastically reducing DOM size and rendering time.

### Basic Usage — Fixed Collection

Replace a `foreach` loop with `Virtualize`, wrapping it in a scroll container:

```razor
<div style="height:500px; overflow-y:scroll">
    <Virtualize Items="allFlights" Context="flight">
        <FlightSummary @key="flight.FlightId" Details="@flight.Summary" />
    </Virtualize>
</div>
```

Without a `Context` parameter, use `context` to access each item:

```razor
<Virtualize Items="allFlights">
    <FlightSummary @key="context.FlightId" Details="@context.Summary" />
</Virtualize>
```

### Item Provider Delegate — On-Demand Loading

When items come from an external source or the collection isn't `ICollection<T>`, use `ItemsProvider`:

```csharp
private async ValueTask<ItemsProviderResult<Employee>> LoadEmployees(
    ItemsProviderRequest request)
{
    var numEmployees = Math.Min(request.Count, totalEmployees - request.StartIndex);
    var employees = await EmployeesService.GetEmployees(
        request.StartIndex, numEmployees, request.CancellationToken);

    return new ItemsProviderResult<Employee>(employees, totalEmployees);
}
```

```razor
<Virtualize Context="employee" ItemsProvider="LoadEmployees">
    <p>@employee.FirstName @employee.LastName — @employee.JobTitle</p>
</Virtualize>
```

Call `RefreshDataAsync()` on the component ref when external data changes. If called from a background task (not a Blazor lifecycle method), also call `StateHasChanged()`.

### Placeholder and Empty Content

```razor
<Virtualize Context="employee" ItemsProvider="LoadEmployees">
    <ItemContent>
        <p>@employee.FirstName @employee.LastName</p>
    </ItemContent>
    <Placeholder>
        <p>Loading&hellip;</p>
    </Placeholder>
    <EmptyContent>
        <p>No employees found.</p>
    </EmptyContent>
</Virtualize>
```

### Item Size and Overscan

- `ItemSize` (default: 50px) — set to match actual rendered height to avoid a second re-render pass.
- `OverscanCount` (default: 3) — extra items rendered above/below the viewport to reduce flicker during fast scrolling.

```razor
<Virtualize Items="employees" ItemSize="25" OverscanCount="4">
    ...
</Virtualize>
```

### Keyboard Scroll Support

Add `tabindex="-1"` to the scroll container so keyboard scrolling works in Chromium browsers:

```razor
<div style="height:500px; overflow-y:scroll" tabindex="-1">
    <Virtualize Items="allFlights">...</Virtualize>
</div>
```

### Table Virtualization

Use `SpacerElement="tr"` when virtualizing inside a `<tbody>`:

```razor
<table>
    <thead style="position: sticky; top: 0; background-color: silver">
        <tr><th>Item</th><th>Value</th></tr>
    </thead>
    <tbody>
        <Virtualize Items="fixedItems" ItemSize="30" SpacerElement="tr">
            <tr @key="context" style="height: 30px;">
                <td>Item @context</td>
                <td>Another value</td>
            </tr>
        </Virtualize>
    </tbody>
</table>
```

### Layout Requirements

`Virtualize` uses spacer `div` elements and Intersection Observer. Correct functioning requires:

- All items are the **same height**.
- Items render in a **single vertical stack** filling full width.
- Scroll container `display` is `block`, `table-row-group`, or `flex` with `flex-direction: column`.
- Don't interfere with spacer element sizing via CSS.

---

## Avoid Thousands of Component Instances

Each component instance adds ~0.06ms rendering overhead. At 2,000 instances that's 120ms — noticeable UI lag.

### Inline Child Components into Parents

Instead of:

```razor
@foreach (var message in messages)
{
    <ChatMessageDisplay Message="message" />
}
```

Inline the markup when thousands of items are rendered:

```razor
@foreach (var message in messages)
{
    <div class="chat-message">
        <span class="author">@message.Author</span>
        <span class="text">@message.Text</span>
    </div>
}
```

Trade-off: loses independent child re-rendering.

### Use `RenderFragment` for Reusable Markup

Define render fragments in `@code` to reuse markup without per-component overhead:

```razor
<div class="chat">
    @foreach (var message in messages)
    {
        @ChatMessageDisplay(message)
    }
</div>

@code {
    private RenderFragment<ChatMessage> ChatMessageDisplay = message =>
        @<div class="chat-message">
            <span class="author">@message.Author</span>
            <span class="text">@message.Text</span>
        </div>;
}
```

Static `RenderFragment` fields can be shared across components:

```csharp
public static RenderFragment SayHello = @<h1>Hello!</h1>;
```

---

## Don't Receive Too Many Parameters

Each parameter passed to a component rendered 4,000 times adds ~15ms. Ten parameters = ~150ms lag.

**Bundle related parameters** into a single object:

```razor
@code {
    [Parameter]
    public TItem? Data { get; set; }

    [Parameter]
    public GridOptions? Options { get; set; }
}
```

**Caveat:** Non-primitive parameter objects always trigger re-render because Blazor can't detect internal mutations. Primitive parameters only re-render when values actually change. Benchmark both approaches.

---

## Ensure Cascading Parameters Are Fixed

`CascadingValue` with `IsFixed="false"` (default) sets up a change subscription for each recipient — substantially more expensive than a regular `[Parameter]`.

Set `IsFixed="true"` when the cascaded value never changes:

```razor
<CascadingValue Value="this" IsFixed="true">
    <SomeOtherComponents />
</CascadingValue>
```

---

## Avoid Attribute Splatting with `CaptureUnmatchedValues`

Attribute splatting is expensive because the renderer must match all supplied parameters against known ones and track overwrite order. Avoid on components rendered at scale (list items, grid cells). Fine for one-off components like dialogs or forms.

---

## Don't Trigger Events Too Rapidly

Events like `onmousemove` and `onscroll` fire tens or hundreds of times per second. Use JS interop with throttling to limit callback frequency:

```csharp
@code {
    [JSInvokable]
    public void HandleMouseMove(int x, int y)
    {
        message = $"Mouse move at {x}, {y}";
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            selfReference = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("onThrottledMouseMove",
                mouseMoveElement, selfReference, 500);
        }
    }
}
```

---

## Avoid Rerendering After Events Without State Changes

`ComponentBase` calls `StateHasChanged` automatically after every event handler. To suppress this:

### Per-Component: Implement `IHandleEvent`

```csharp
@implements IHandleEvent

Task IHandleEvent.HandleEventAsync(
    EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
```

### Per-Handler: Use `EventUtil.AsNonRenderingEventHandler`

```razor
<button @onclick="EventUtil.AsNonRenderingEventHandler(HandleClick)">
    Click (no re-render)
</button>
```

---

## Avoid Recreating Delegates in Loops

Lambda delegates created inside `@for`/`@foreach` loops are recreated on every render. For large collections, pre-assign delegates:

```csharp
@code {
    private List<ButtonInfo> Buttons { get; set; } = new();

    protected override void OnInitialized()
    {
        for (var i = 0; i < 100; i++)
        {
            var button = new ButtonInfo
            {
                Id = Guid.NewGuid().ToString(),
                Action = e => UpdateHeading(button, e)
            };
            Buttons.Add(button);
        }
    }
}
```

```razor
@foreach (var button in Buttons)
{
    <button @key="button.Id" @onclick="button.Action">
        Button #@button.Id
    </button>
}
```
