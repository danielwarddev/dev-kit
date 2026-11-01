# Writing Tests

Use tests early to shape design. Choose the smallest reliable level: unit tests for
isolated behavior; integration tests for collaborating units, end-to-end flows, or
file I/O. UI rendering follows its framework skill; file placement follows
`creating-and-editing-types.md`.

Do not test pure delegation, DI registration without application logic, unchanged
framework behavior, or unchanged exception propagation. Never call external
services. Do not widen production visibility for testing; extract meaningful logic.

## Naming

Use `When_<action>_Then_<expected_result>`, separating every English word with an
underscore. A verbatim code identifier counts as one unit:

```text
When_ComputeHash_Called_Then_Returns_Sha256_Hex_String
```

`ReturnsSha256HexString` is not a verbatim identifier and must be separated.

## Setup and assertions

- Keep tests deterministic; substitute external services, clocks, randomness, and
  environmental boundaries. Prefer `Substitute.For<T>()` over handwritten fakes
  when it can express the behavior.
- Use Arrange/Act/Assert without label comments. Omit phase-separating blank lines
  when each phase is one line (including two-line Act/Assert tests); otherwise
  separate phases and logical groups within substantial setup.
- Keep trivial setup inline. Extract helpers only for meaningful repetition or
  clearer intent. For shared setup, initialize class-level doubles and the subject
  in the constructor; xUnit creates a fresh instance per test.
- In database tests using the repository's base class, initialize the subject once
  from its context factory in the constructor and store it in a private readonly
  field, rather than calling `CreateService()` per test.
- Assert observable values/state/output. Use `BeEquivalentTo` for several properties
  when clearer than individual assertions. Use `Received`/`DidNotReceive` only when
  the interaction itself is the behavior and cannot otherwise be observed.
- Cover meaningful outcomes of changed decisions, including errors, empty, and
  unavailable states.
- Use theories for cases sharing one verification path. Expected values may vary,
  but flags must not branch assertions or expected-diagnostic setup; use separate
  named facts for diagnostic/no-diagnostic or otherwise different paths.
