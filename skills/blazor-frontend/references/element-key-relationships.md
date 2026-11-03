# Element, Component & Model Relationships — `@key`

Reference extracted from [Retain element, component, and model relationships in ASP.NET Core Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/element-component-model-relationships?view=aspnetcore-10.0).

---

## What `@key` Does

When rendering a list of elements or components that subsequently change (insert, delete, reorder), Blazor must decide which previous elements to retain and how model objects map to them. By default, Blazor uses index-based matching, which can cause:

- Loss of focus or input state when items are inserted at the beginning.
- Incorrectly reusing DOM elements for different data items.

The `@key` directive attribute gives you explicit control over this mapping by associating each element/component with a specific key value.

---

## When to Use `@key`

Use `@key` when:

- A list is rendered with `foreach` **and** items may be inserted, deleted, or reordered.
- You need to preserve per-item DOM state (focus, input values, scroll position).
- An object instance change should force Blazor to discard and rebuild the subtree.

```razor
@foreach (var person in people)
{
    <Details @key="person" Data="@person.Data" />
}
```

With `@key="person"`, Blazor:
- Inserts a new `Details` instance only for the new `Person`.
- Leaves existing instances unchanged.
- Preserves user focus and input state.

---

## When NOT to Use `@key`

- There is a small performance cost to key-based diffing.
- If items are never inserted, deleted, or reordered (only appended), `@key` adds overhead without benefit.
- Without `@key`, Blazor still preserves child instances as much as possible using index matching.

---

## Values to Use for `@key`

| Value Type | Example | Notes |
|-----------|---------|-------|
| Model object instance | `@key="person"` | Preserves based on reference equality |
| Primary key (int, string, Guid) | `@key="person.Id"` | Preserves based on value equality |

**Keys must be unique within their parent scope.** Clashing keys throw an exception.

---

## Scoping Rules

`@key` is scoped to its **sibling elements within the same parent**. Keys are NOT compared globally across the document.

### Correct — keys scoped to the same parent

```razor
<div>
    @foreach (var person in people)
    {
        <Details @key="person" Data="@person.Data" />
    }
</div>
```

```razor
@foreach (var person in people)
{
    <div @key="person">
        <Details Data="@person.Data" />
    </div>
}
```

```razor
<ol>
    @foreach (var person in people)
    {
        <li @key="person">
            <Details Data="@person.Data" />
        </li>
    }
</ol>
```

### Incorrect — key scoped to inner wrapper, not the loop's parent

These patterns scope `@key` to an inner element that wraps each item individually, so the key doesn't help Blazor track items across the collection:

```razor
@* BAD: @key is on <Details> inside a per-iteration <div>, not on the <div> itself *@
@foreach (var person in people)
{
    <div>
        <Details @key="person" Data="@person.Data" />
    </div>
}
```

```razor
@* BAD: @key is on <Details> inside a per-iteration <li>, not on the <li> itself *@
<ol>
    @foreach (var person in people)
    {
        <li>
            <Details @key="person" Data="@person.Data" />
        </li>
    }
</ol>
```

**Fix:** Move `@key` to the outermost element within the loop:

```razor
@foreach (var person in people)
{
    <div @key="person">
        <Details Data="@person.Data" />
    </div>
}
```

---

## Combining `@key` with `Virtualize`

When using `Virtualize`, apply `@key` on the item template element:

```razor
<Virtualize Items="allFlights" Context="flight">
    <FlightSummary @key="flight.FlightId" Details="@flight.Summary" />
</Virtualize>
```

This ensures that when Virtualize recycles DOM elements during scrolling, it correctly maps each element to the right data item.
