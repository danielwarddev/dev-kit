# List Identity and `@key`

Use `@key` when item identity matters across updates, especially insertions,
deletions, or reordering that must preserve focus or component state. Changing a
key deliberately discards and rebuilds that keyed subtree. Keys are not a general
performance optimization.

## Key Selection

- Use a stable model ID when refreshed objects still represent the same item.
  Use the model object when its equality semantics represent the intended identity.
- Do not use a changing list index to represent stable item identity.
- Keys must be unique among siblings within the same parent; duplicates throw.

## Scope

Keys are compared only among siblings, not globally. Put the key on the outermost
element/component repeated by the loop.

Correct: the repeated wrappers share one parent and can be matched by identity.

```razor
@foreach (var person in people)
{
    <div @key="person.Id">
        <Details Data="@person.Data" />
    </div>
}
```

Incorrect for tracking the list: each key is isolated inside its own wrapper.

```razor
@foreach (var person in people)
{
    <div>
        <Details @key="person.Id" Data="@person.Data" />
    </div>
}
```

The same identity and scope rules apply to virtualized item templates.
Consult the current [Blazor relationship documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/element-component-model-relationships)
for API-dependent changes.
