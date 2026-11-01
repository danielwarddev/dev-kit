# Service APIs and Dependencies

## The Robustness Principle for Signatures

Accept the most general type that makes sense; return the most specific type
actually produced. Do not widen a known concrete return type to an interface
when the implementation already knows the concrete type:

```csharp
public List<Order> Filter(IEnumerable<Order> orders, string term)
```

## Interfaces and Dependencies

Interfaces expose real application use cases, not internal adapter/factory helpers,
pass-through methods, or convenience overloads. Collapse identical delegating
overloads into one method with an optional parameter and named call-site arguments.

Register services, SDK wrappers, and HTTP behavior at the existing composition root
(`Program.cs` where applicable). Group related registrations in a slice-local
`ServiceCollectionExtensions` method following existing patterns.

Supply service collaborators through constructor injection, including manually
wired concrete classes. Construct parsers, repositories, clients, and other
dependencies at the composition root, not inside their consuming service.
Avoid global mutable state and static service locators hiding dependencies.

Do not create a mapper/service merely to move a few field lookups out of its only
caller. Direct raw-model construction belongs in the owning external-data adapter
unless mapping has meaningful reusable or independently testable behavior.

Normalization/interpretation needed for validation belongs in the validator;
return the validated representation instead of requiring callers to prepare the
validator's internal data shape.
