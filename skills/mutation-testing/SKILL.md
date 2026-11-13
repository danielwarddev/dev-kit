---
name: mutation-testing
description: 'Use when running Stryker.NET mutation testing on a .NET project, analyzing a mutation report, or measuring and improving test effectiveness beyond line coverage.'
argument-hint: 'Project to mutate or a survivor to analyze'
---

# Mutation Testing

## When to Use

- Run a mutation report, measure mutation score, or check whether tests catch regressions.
- Analyze an existing Stryker HTML or JSON mutation report.
- Assess test quality before a release or a tricky refactor.

## Workflow

1. Discover the project, test projects, framework, existing Stryker configuration, and
   tool version. Verify the source tests are green. For setup and the conditional xUnit
   v2 workaround, see [project setup](references/project-setup.md).
2. Run Stryker, check that the run is valid, and classify survivors using
   [run and analyze](references/run-and-analyze.md). If a report was supplied, analyze
   it without running or installing anything unless further verification is needed.
3. Implement agreed tests in the original test project, re-run the relevant tests and
   mutation run, and clean up only temporary artifacts created for the run.

## Key Rules

- A failed coverage capture or an unexpectedly zero-killed run is not reliable evidence
  of test quality. Diagnose it before drawing conclusions; zero kills alone does not
  prove a particular runner is at fault.
- An xUnit v2 clone is a workaround for an affected xUnit v3/VSTest combination, not a
  prerequisite for every project. Check current Stryker and runner support first.
- Keep new tests in the original test project. Mirror them into a temporary clone only
  when re-verifying mutations.
- A surviving mutant isn't automatically a missing test. Use the triage rubric and
  present suggestions in importance buckets, not a raw list.
- Don't fabricate test doubles solely to exercise behavior the product cannot currently
  produce; describe it as future coverage instead.
- Preserve existing configuration and unrelated changes. Never use a destructive Git
  checkout to undo temporary solution edits.
