# Writing C#

## Naming and expression clarity

Name methods for domain actions understandable at the call site, not generic verbs
such as `Process` or `Get` that require reading the body. Prefer names like `ReadOrders`,
`GetNutritionAmount`, or `ParseShipDate`. Correct unclear names in methods you edit;
a narrowly named class alone does not rescue an ambiguous operation.

Use private methods for reusable class operations. Reserve local functions for
genuinely local control flow or closures.

Assign meaningful calls, `await` expressions, and LINQ pipelines to named locals
rather than nesting them inside arguments:

```csharp
var existingFoods = await GetAllFoods(cancellationToken);
var existingSlugs = existingFoods.Select(food => food.Slug);
var slug = recipeSlugger.CreateUniqueSlug(food.Name, existingSlugs);
```

Separate logical phases with blank lines, especially around validation. Keep
related statements together; do not separate every statement or impose group
sizes. Break long arguments/chains at meaningful boundaries. Prefer whitespace
over comments labeling obvious phases.

## External data and nullability

Convert positional external data to a named raw model inside its adapter, retaining
source metadata for errors. Downstream parsing/normalization consumes named
properties, not CSV arrays and header/index maps. When needed, map first and then
transform/validate the raw model.

Do not manufacture empty/default values for structurally missing required positions;
fail explicitly at the boundary.

Nullable annotations provide compile-time analysis, not a runtime non-null guarantee.
Avoid redundant guards inside trusted, annotated internal flows. Validate non-null
requirements at public/external or unannotated boundaries when callers can supply
null, and for required scene wiring. Fail with an actionable error rather than a
silent default. Use nullable types when null is intentionally supported.

## Async and errors

Use asynchronous I/O and accept/propagate cancellation where meaningful, tied to
request, operation, or object lifetime. The forwarding diagnostic does not establish
that APIs accept a token or use the correct lifetime.

Catch narrowly for a specific recovery or actionable reporting. Do not swallow
exceptions, return success-shaped defaults, or continue in an unknown state.
General-catch and rethrow diagnostics do not detect every silent failure.
