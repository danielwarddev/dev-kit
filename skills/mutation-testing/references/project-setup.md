# Mutation Testing — Project Setup

Discover the project under test, its test projects, target frameworks, test framework,
solution, existing Stryker configuration, and local tool manifest. Substitute their
actual paths for `<Project>`, `<TestProject>`, and `<Solution>` below.

## Tool and baseline

Check `dotnet stryker --version` and the project's instructions. Use the existing local
tool when available; if Stryker is missing or incompatible, follow the project's tool
policy rather than silently changing the user's global tools. Stryker.NET 4.16.0 or
newer has been needed for some .NET 10/Razor projects; check current compatibility for
the actual SDK and tool version.

Run the relevant source suite and record the result and test count before mutation:

```powershell
dotnet test <TestProject>\<TestProject>.csproj
```

Investigate baseline failures first. Inspect existing configuration rather than
overwriting it; use a temporary configuration or preserve and restore any changes
needed for the run.

## xUnit v3 workaround, when needed

Some Stryker.NET/VSTest combinations cannot capture coverage from xUnit v3 tests on
Microsoft.Testing.Platform. A coverage-capture failure with all mutants surviving is a
symptom, not evidence that the suite is effective or ineffective. Check the current
Stryker runner support and project setup first; use a temporary xUnit v2 clone only if
the installed combination needs it.

1. Inspect the v3 test project and its dependencies, including fixtures, linked files,
   resources, project references, imports, and shared build settings. Check for v3-only
   APIs and lifecycle differences; adapt the clone only as needed. For example,
   `TestContext.Current.CancellationToken` may need a clone-only shim, and asynchronous
   fixture disposal may need an xUnit v2 `IAsyncLifetime` implementation.
2. Create `<TestProject>.XunitV2` without overwriting an existing directory. Copy the
   test sources and required project assets, excluding generated `bin` and `obj`
   content. Preserve namespaces and ensure project references and build imports still
   resolve from the clone.
3. Copy the project file and replace the v3 xUnit packages with compatible xUnit v2 and
   VSTest runner packages. Preserve the target framework, test SDK, and other required
   dependencies. Check current package compatibility rather than assuming version numbers.
4. Configure Stryker to target the clone. If solution discovery requires adding it to
   `<Solution>`, record the pre-existing solution state and remove only the temporary
   project entry afterward. Do not commit a solution reference to a temporary clone.
5. Run the clone's tests and compare the discovered test count and outcomes with the
   original. A VSTest output line alone does not prove Stryker is using the clone; verify
   the configured test project and the mutation run itself.

For example, if the only incompatible API is
`TestContext.Current.CancellationToken`, a clone-only shim can preserve call sites:

```csharp
namespace Xunit;

public static class TestContext
{
    public static TestContextShim Current { get; } = new();

    public sealed class TestContextShim
    {
        public CancellationToken CancellationToken => CancellationToken.None;
    }
}
```

Only use this if a non-cancelable token preserves the tests' intended behavior. For
fixtures that need asynchronous disposal, verify the clone actually disposes them
asynchronously; changing the lifecycle can change test outcomes.

An example package swap is `xunit` 2.9.3 and `xunit.runner.visualstudio` 3.1.5 in
place of the v3 package. Keep the existing test SDK and other dependencies, and check
the runner version against the project's framework and tool versions.

## Configuration

Use the existing project configuration where possible. For a temporary configuration,
adapt this example to the project and verify the paths against the installed Stryker
version:

```json
{
  "stryker-config": {
    "solution": "<Solution>",
    "project": "<Project>.csproj",
    "test-projects": ["<TestProject>/<TestProject>.csproj"],
    "reporters": ["html", "progress", "json"]
  }
}
```

For the workaround, point `test-projects` at the clone instead. Exclude files or use
`ignore-methods` only when their mutations are outside the behavior being evaluated.
For example, `*Exception.ctor` ignores constructor argument mutations; `.constructor`
is not the equivalent suffix. Do not ignore arguments that affect observable behavior.

Next: [run and analyze](run-and-analyze.md).
