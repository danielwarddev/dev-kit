# Mutation Testing — Run & Analyze

Use the project's existing Stryker setup or the setup in
[project setup](project-setup.md). Run from the directory expected by its configuration:

```powershell
dotnet stryker
```

Check the output for compilation failures, failed tests, coverage-capture errors, and
the intended test project. An unexpected zero-killed result warrants investigation;
it can indicate a broken run, but does not by itself identify the cause. Don't treat a
failed capture as a meaningful mutation score.

## Parse the report

Prefer the JSON reporter when available. The HTML report also embeds report data.
For a JSON report, summarize surviving and uncovered mutants with:

```powershell
$r = Get-Content "StrykerOutput\<run>\reports\mutation-report.json" -Raw | ConvertFrom-Json
$s = @{}
foreach ($f in $r.files.PSObject.Properties) {
  foreach ($m in $f.Value.mutants) {
    $s[$m.status] = 1 + $s[$m.status]
    if ($m.status -in 'Survived','NoCoverage') {
      Write-Host ("{0}:{1} [{2}] {3} => {4}" -f (Split-Path $f.Name -Leaf), `
        $m.location.start.line, $m.status, $m.mutatorName, ($m.replacement -replace '\s+',' '))
    }
  }
}
$s.GetEnumerator() | Sort-Object Name | Format-Table -Auto
```

Read the surrounding code and tests before interpreting a survivor. Report the score
with its scope and limitations; don't optimize a score at the expense of meaningful tests.

## Triage and report

Classify survivors in order and explain the reasoning instead of dumping a raw list:

1. **Ignore — equivalent or cosmetic.** No test can distinguish an equivalent mutant;
   state the invariant that makes it equivalent. For non-contractual message or log
   text, consider a narrowly scoped `ignore-methods` entry instead of brittle assertions.
2. **Worth fixing — real, cheap wins.** Test observable behavior the product promises,
   such as a boundary or an uncovered guard. Fold cases into an existing parameterized
   test when appropriate.
3. **Worth a look — real but expensive or edge-case.** Describe the behavioral gap and
   effort, such as a setup where ordering changes the result. Implement if agreed.
4. **Future coverage — non-existent conditions.** If the killing scenario depends on
   behavior the product doesn't implement, don't invent a test double solely to force it.
   Note when the gap would become relevant.

## Implement and verify

- Add agreed tests to the original test project, not a temporary clone.
- If using a clone, mirror changed test files into it and re-run Stryker to check the
  target mutants. Run the original tests and any relevant build checks as well.
- Remove only temporary clone/configuration artifacts created for this run, and undo
  only the temporary solution entry. Restore any pre-existing configuration and
  preserve unrelated edits. Keep report artifacts as appropriate for the user.
- Confirm cleanup and report any remaining failure or limitation.
