# Types and File Placement

## Colocate by default

Put supporting interfaces, records, DTOs, options, exceptions, enums, and helpers in
their consumer's file, above the primary class. A type does not get its own file by
habit; an `Abstractions` folder with one interface per file is not an exception.

```csharp
public interface IProcessRunner
{
    Task<ProcessResult> Run(string fileName, string arguments, CancellationToken cancellationToken);
}

public record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

public class ProcessRunner : IProcessRunner
{
    // Implementation.
}
```

When editing a type, merge separately filed supporting types that violate this
policy unless a split is justified below. This layout correction is in scope;
leave types you are not otherwise touching alone.

Split when navigation becomes cumbersome, a type has independently testable
behavior, or several unrelated consumers in the feature need it. Split along
cohesive seams, not arbitrary line counts. Keep extracted parsers, mappers, and
other collaborators in the same feature folder/namespace; leave the service
orchestrating them.

Each class should have one reason to change. More than one or two private helpers
is a signal to reconsider responsibilities, not a hard limit. Extract a cohesive,
meaningful concern when that improves the design, not merely to reduce a count.

## Placement and declarations

- Use existing projects; add a class library only for a genuine requirement.
- Organize by vertical slice: use the existing feature folder that matches the
  behavior, and create a new slice only when none fits.
- Within a slice, give distinct concerns focused sub-slices and services rather
  than growing a catch-all service. Share helpers only when several sub-slices
  actually reuse the same logic; otherwise keep them with their consumer.
- Put tests in the corresponding existing test project, mirroring source paths.
- UI components follow their framework's skill.
- Do not mark members `internal`; the repository also disallows explicit `internal`
  type modifiers when editing those types. Implicit accessibility is not addressed.
- Use records for immutable models when consistent with surrounding code.
- Group consecutive single-line fields, constants, interface declarations, and
  auto-properties without blank lines. Separate members with bodies and genuinely
  distinct groups.
