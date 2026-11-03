# bUnit Component Tests

Use `csharp-standards`' `writing-tests.md` for naming, placement, setup, assertions,
and interaction verification. Query Context7 with `/websites/bunit_dev` for current
bUnit APIs.

## Component Boundaries

bUnit normally renders child components too. Choose whether to render or stub
children based on the behavior under test; do not create duplicate unit/integration
suites solely because a component tree is rendered.

Parent tests cover the parent's decisions: conditional sections, interactions, and
data passed to children. Do not re-test a child's internal rendering or CSS state
from the parent.

Test state changes, user interactions, and data-driven output, not unconditional
static copy. Verify static presentation in the browser.

## Rendering and Async Setup

- Reuse genuinely shared render setup, including required services, providers, and
  JSInterop stubs. Keep scenario-specific behavior in its test.
- Render with the providers and interactive context the component requires.
  A `.razor` test host requires the test project's Razor SDK.
- Use bUnit's wait helpers for asynchronous rendered outcomes, not sleeps.
- Configure JavaScript expectations for the behavior exercised; do not broadly
  silence unexpected JSInterop calls just to make rendering pass.

Component tests complement the skill's required browser verification; they do not
establish visual correctness.
