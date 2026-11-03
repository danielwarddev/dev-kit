# bUnit Component Tests

Read this before writing or reviewing a component test.

Component tests live in the test project, mirroring the component's folder. Setup that
every component test needs belongs in a single shared test context class rather than
being repeated per test class.

For current bUnit API patterns, query the Context7 MCP server with library ID
`/websites/bunit_dev`.

The repository's general test rules — naming, assertion style, what not to test — apply
here as well; see the C# standards skill if the project has one.

## No Unit / Integration Split

bUnit renders the full component tree by default, so a parent-component test is
inherently integration-style. Do not try to maintain a separate unit and integration
layer for components.

## Test Observable Outcomes, Not Invocations

Prefer asserting that a message appeared, a component rendered or disappeared, or UI
state changed, over asserting that a method was called.

Use `Received()` only when a service call with specific arguments genuinely *is* the
behavior under test — typically when the service owns the mutable state and the parent
has no other way to observe the result.

## Avoid `DidNotReceive()`

If a visible outcome already proves the operation did not happen — a no-op message is
shown, the modal is closed — assert that outcome instead. A `DidNotReceive()` check
alongside such an assertion is redundant.

## Do Not Test Child Internals From the Parent

Each child component has its own tests. A parent's tests should verify only what the
*parent* is responsible for: wiring services, showing or hiding sections, and passing
correct data.

Do not assert on CSS state, internal properties, or render logic that belongs entirely
to a child.

## Do Not Test Static Copy

If text is unconditional and carries no state or behavior, a test only proves a literal
string exists in markup. That is brittle when wording changes and protects nothing.

Reserve automated tests for state changes, conditional rendering, user interactions,
and data-driven output. Verify static copy in the browser instead.

## Keep Shared Setup Minimal

Class-level setup should contain only what the component needs in order to render
without crashing: service registrations, JSInterop stubs, and fixed return values for
state read during initialization.

Tests needing specific mock behavior set it up in the test body. NSubstitute's default
return values — null, zero, empty collections — are already implicit; do not restate
them in setup.
