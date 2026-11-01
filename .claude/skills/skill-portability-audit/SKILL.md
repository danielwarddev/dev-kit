---
name: skill-portability-audit
description: 'Audit a skill directory for drop-in portability across projects — free of hardcoded project facts that would go stale or become false when copied elsewhere. Use when creating or editing a skill meant to be reused across repositories, before distributing a skill (submodule, template, copy-paste), or as a periodic regression check on skills that change often.'
argument-hint: 'Skill directory (or directories) to audit'
---

# Skill Portability Audit

A repeatable process for verifying that a skill's content is truly drop-in portable —
free of project-specific facts that would go stale or become false when copied into a
different repository. Re-run this after any nontrivial edit to a skill intended for
reuse.

## When to Use

- After creating or editing any skill intended to be reused across projects.
- Before distributing a skill directory (submodule, template repo, copy-paste) to
  another project.
- As a periodic regression check if skills are edited frequently.

## Why a Sub-Agent, Not Self-Review

The author of a skill is the worst reviewer of its portability. Having just written or
stared at the project's real folder names, services, and paths, the author's mind
silently fills in what a sentence like "match the existing feature folders" means —
making it impossible to notice that the *sentence itself* is fine but a neighboring
sentence still hardcodes a project-specific path. A fresh agent with no memory of the
authoring conversation is a reasonable stand-in for someone dropping the skill into an
unfamiliar project: it can only judge the text as literally written.

Use a read-only, exploration-focused sub-agent in its own context window (for example,
the `explore` agent type if the `task` tool is available). Read-only matters: the audit
must not "helpfully" fix issues mid-scan, because every flagged item needs human
adjudication first (see Adjudicate below).

## The Acceptance Test

Every sentence in a portable skill must pass:

1. **False-on-import** — would this sentence become factually false if the file were
   copied into an unrelated project? (Naming a specific project/folder/class/port/path
   that only exists in the current repo.)
2. **Unfollowable** — would any instruction be impossible to follow, or leave the agent
   stuck, in a project that isn't this one? (A bare pointer to project docs that may not
   exist, with no fallback.)
3. **Dangling cross-reference** — does the file reference another skill/file/path that
   would not exist if this skill's directory were copied on its own?
4. **Leakage of an unrelated domain's rules** — e.g. UI/framework-specific rules inside
   a supposedly framework-agnostic language skill, or vice versa.
5. **Reference-to-reference link** — does a file under `references/` link to or direct
   the reader to another file under `references/`? Reference files must be independent.
   Only `SKILL.md` may route readers to reference files.

### Explicit non-violations (must be stated in the prompt, or the agent over-flags)

- **Illustrative code examples with invented type names** (e.g. `ProcessRunner`,
  `Order`, `ConfirmDialog`) are fine — an example doesn't assert a fact about the
  project.
- **Discovery-style instructions** ("match the existing feature folders," "follow the
  project's existing persistence pattern") are the *desired* pattern, not violations.
- **Pointer-style references** ("use the repository's documented build command") are
  the desired pattern, provided they degrade gracefully (see Adjudicate below).
- **Conventions the skill itself establishes** (e.g. "specs live in `.specs/`") are not
  project facts — they stay true on import because the skill is what defines them.

## Exact Prompt Template

```
You are auditing a set of AI-agent "skill" documents in the repository at
<REPO_PATH> for PORTABILITY. These skill files are intended to be dropped into ANY
<LANGUAGE/FRAMEWORK> project unchanged. Project-specific facts are supposed to live
only in the project's top-level agent instructions file (e.g. CLAUDE.md), NOT in the
skills.

Read every markdown file under these directories:
- <list every skill directory in scope, e.g. SKILL.md and references\*.md>

Apply this acceptance test to EVERY sentence, and report violations:

1. FALSE-ON-IMPORT: Would the sentence become factually FALSE if this file were
   copied into an unrelated <language> project? Examples of violations: naming a
   specific project/assembly/folder that only exists in this repo ("the test
   project is Foo.UnitTests", "the slices are A\, B\, C\"), describing what a
   specific file in this repo contains, referencing a specific script path or port
   number, or referencing a specific class that exists only here.
   - NOTE: Illustrative CODE EXAMPLES using invented type names (e.g. ProcessRunner,
     Order, Document, ConfirmDialog) are ACCEPTABLE and are NOT violations, because
     an example does not assert a fact about the project. Do not flag these.
   - NOTE: Instructions that tell the agent to LOOK at the project ("match the
     existing top-level feature folders", "follow the project's existing
     persistence pattern", "use the repository's documented build command") are the
     DESIRED pattern and are NOT violations.

2. UNFOLLOWABLE: Would any instruction be impossible to follow, or leave the agent
   stuck, in a project that is not this one? For example, a pointer to a document or
   path that may not exist there.

3. DANGLING CROSS-REFERENCE: Does any file reference another skill, file, or path
   that would not exist if the skill directory were copied on its own?

4. <DOMAIN> LEAKAGE (<skill name> only): The <skill name> skill must contain NO
   <other-domain>-specific rules. Generic statements that merely DELEGATE that
   concern to another skill (e.g. "X concerns belong to the Y skill") are
   acceptable. Flag any actual <other-domain> rule.

5. REFERENCE-TO-REFERENCE LINKS: Does any file under `references/` link to or direct
   the reader to another file under `references/`? Reference files must be
   independent and must not reference each other. Only `SKILL.md` may route readers
   to reference files. Flag sibling references even when their links resolve.

Also separately note anything that is a genuine JUDGMENT CALL rather than a clear
violation (e.g. naming specific libraries/packages the project assumes, or a
convention the skill itself establishes such as specs living in `.specs/`), so the
human can decide.

Report your findings as a concrete list, grouped by file, quoting the exact
offending text and the line it is on. Be precise and exhaustive. Do NOT edit any
files — this is a read-only audit.
```

Fill in `<REPO_PATH>`, `<LANGUAGE/FRAMEWORK>`, the directory list, and the domain names
for the specific audit. Keep the five numbered criteria and the "non-violations" notes
verbatim — they are what keeps the false-positive rate down.

## Dispatching the Audit

Use a read-only exploration sub-agent, giving it the exact prompt above (filled in).
If working in the GitHub Copilot CLI, this is the `task` tool with `agent_type:
"explore"`. In another environment, use whatever read-only, isolated-context
research/explore agent is available.

## Adjudicate Every Finding — Do Not Auto-Apply

Treat the agent's output as a candidate list, not a verdict. In practice the audit
over-flags: it tends to re-flag the exact "non-violation" patterns called out in the
prompt (discovery instructions, pointer references, self-defined conventions) because a
fast model leans toward recall over precision on judgment-heavy tasks.

For each finding:

1. Re-check it against the four non-violation notes above before accepting it.
2. If it's a real violation, decide the fix:
   - Replace a hardcoded fact with a discovery instruction ("look at the existing
     folders") if the answer is something the agent can find by looking.
   - Replace it with a pointer to the project layer ("the repository's documented
     build command") if it's something the project's top-level instructions file
     should own.
   - Add a fallback to a bare pointer so it degrades gracefully when no project
     documentation exists (e.g. "...or `dotnet build`/`dotnet test` if none is
     documented").
   - Move the fact itself into the project's top-level instructions file if the
     skill needs the concrete value nowhere but the current project does.
3. If it's a judgment call (e.g. "should this skill assume a specific test
   framework?"), surface it to the user rather than deciding unilaterally — the
   tradeoff is compliance/specificity vs. portability, and reasonable people
   disagree.
4. After fixes, re-validate cross-references and links mechanically (a short
   script pass checking every relative markdown link resolves and that files under
   `references/` do not link to one another), since moving content around breaks links
   silently and valid sibling links still violate reference independence.

## Self-Check

- [ ] Does every reference file in scope pass the five-part acceptance test?
- [ ] Were all four non-violation carve-outs applied before accepting a finding as real?
- [ ] Does every bare pointer to "project docs" degrade gracefully when none exist?
- [ ] Do all relative cross-references between skill files still resolve?
- [ ] Are reference files linked only from `SKILL.md`, with no reference file linking
   to another reference file?
- [ ] Were judgment calls (assumed libraries, self-defined conventions) surfaced to the
      user instead of decided unilaterally?
