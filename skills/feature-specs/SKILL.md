---
name: feature-specs
description: 'Use when capturing what to build as requirements-only specs that state intent and never the solution — no file plans, no layers, no implementation sections. Turns one request into as many spec files as it contains, including large, vague, or batched asks, asking clarifying questions only when an outcome is genuinely ambiguous. Also use when asked to implement a spec or a numbered requirement such as spec 10.1 (requirement 1 of spec 10). Creation requests write spec files and stop; implementation requires a separate request.'
argument-hint: 'The outcome(s) you want, however rough — one line or a brain dump'
---

# Feature Specs

## Overview

An intent spec records **what should become true and why** — nothing about how. The
design is discovered while building, so it lives in the session, never in the file.

That split is the whole point: intent is durable and cheap to review, design is
disposable and wrong at the moment a spec is written. "A generated town survives a
restart" survives being wrong about serialization, scene layout, and storage. A file
plan survives none of it.

On a creation request, this skill **creates spec files and stops.** One request may
produce several specs. Implementation requires a separate request, made later for
one spec or one numbered requirement at a time.

## When to Use

- Any request to capture, write, or flesh out work as a spec — one outcome or twenty.
- A brain dump, a batch of requirements, or a vague "I want X to be better."
- A stated requirement too large to demo in one sitting, which needs splitting.
- A separate request to implement a spec or a numbered requirement ("spec 10.1"
  means requirement 1 of spec 10).

**When not to use:** when the request is to build something now without referencing
a spec, or when the user explicitly wants a spec that contains a technical plan —
file lists, layers, or numbered implementation sections. This format deliberately
has none of those.

## The One Hard Rule

> A spec may state constraints the solution must respect. It may **not** state the
> solution.

The test for any sentence you are about to write:

- Could it be satisfied two very different ways, and the spec doesn't care which?
  → requirement. Keep it.
- Does it name a class, file, project, layer, pattern, or library? → solution. Cut it.

Constraints are legitimate and belong in the spec, because the user owns them:
"must not hit the network in tests", "must run without launching Godot", "the core
stays free of engine types". Those bound the solution without choosing it.

## Location & Numbering

- **Location**: `.specs\` at the repo root.
- **Naming**: `SPEC-XX-Short-Name.md`, sequential two-digit numbers.
- Before writing, scan `.specs\` and continue from the highest existing number, no
  matter what wrote it. Never renumber or rewrite an existing spec.
- In a batch, assign the lower numbers to the specs that are buildable first.

## Sizing

- **One spec = one observable behavior change.** If it can't be demoed in one
  sitting, it is more than one spec.
- **Roughly 3–7 numbered requirements.** Fewer means it's a requirement, not a spec;
  more means at least two outcomes are hiding in it.
- Requirement count is the scope signal here, and unlike a file-change plan it needs
  no guesses about the design to be useful.

Split on these seams:

- Two things a person would look at separately → two specs.
- An "and" in the outcome sentence → usually two specs.
- Slices that could ship a month apart and each still be worth having → separate.
- A requirement that only makes sense once another is real → separate spec, with a
  `Depends on` line.

## Workflow

### 1. Intake — sort the request before writing anything

Read the whole request and sort every fragment into one of three buckets:

- **Ready to spec** — the outcome is clear enough that you could tell whether a
  result matches it.
- **Blocked on the user** — the *outcome* is genuinely ambiguous, so different
  answers would produce different requirements, or writing it would mean quietly
  assuming a decision the user would want to make.
- **Direction, not a spec** — real but too speculative to build soon. This does not
  become a spec file. Offer it for `design-docs\` instead.

Never promote a fragment out of the third bucket by inventing detail for it.

### 2. Ask only when a fragment is actually blocked

Asking is **not** the default. If intake put nothing in the "blocked" bucket, write
the specs and skip this step entirely. Most clear requests should reach step 3
without a single question.

Ask only when one of these is true:

- **Ambiguous outcome** — two readings of the request would produce *different
  requirements*, and you cannot tell which the user meant.
- **A design decision you'd otherwise assume** — you notice you are about to write a
  requirement that silently picks something the user probably wants to pick: what
  the player sees, what happens on failure, what persists, what the scope is. Ask
  about the *outcome* of that decision, never the mechanism.

Do **not** ask when:

- The answer only affects *how* it gets built. That belongs to execution.
- One reading is clearly the sensible default — write it, and note the assumption
  under `## Open questions` so it stays visible and cheap to correct.
- The question is really about polish, naming, or detail you could learn by building.

How to ask:

- Batch every question into a single message; aim for three or fewer, five at most.
- Say what you'd write if the user doesn't care, so "whatever you think" is a
  complete answer.
- Never withhold the whole batch over one blocked fragment. Carry on and write the
  specs that are ready, then ask about the rest alongside the report in step 5.
- If the user defers or says "just write it", write the spec anyway and record the
  unknowns under `## Open questions`. A question blocking a requirement must be
  resolved before that requirement is implemented, not before the file is written.

### 3. Split into specs

Apply the sizing rules to the "ready to spec" bucket. A large stated requirement
becomes several specs, and the original sentence becomes the theme rather than a
spec of its own.

If a request would produce **more than about six specs**, write the ones that are
well understood and list the rest as named candidates. A batch of speculative specs
is the same up-front-design bet this format exists to avoid.

### 4. Write every spec file in one pass

Use the template below verbatim. Write all of the files before reporting.

### 5. Report the batch, then stop

- A flat list, one line per spec: `SPEC-XX-Short-Name.md — one-sentence outcome`.
- Order the list by current belief about build order, and say that it is belief and
  not commitment.
- Then: the candidates you deliberately did not spec, and any open questions.
- **Do not begin implementing.** Creating specs and executing one are separate
  requests, even when the user's message sounds eager.

## Template

```markdown
# SPEC-07: Short Name

**Status**: 📋 Not Started
**Depends on**: none

## What we're going for

One to four sentences of plain English. What changes for whoever uses this, and why
it's worth doing. Written so a non-programmer could tell whether the result matches.
No file names, no class names, no layers.

## Requirements

1. [ ] An observable, checkable statement
2. [ ] Another one
3. [ ] A third one

## Constraints

Boundaries the solution must respect — not choices about how to build it.

## Explicitly not this time

- Overflow pushed to a named follow-up spec, or deliberately dropped.

## Done when

One sentence naming the thing you will look at to believe it works — a run, a scene,
a generated artifact, a printed output.

## Open questions

Outcome questions that are still unanswered, and assumptions made in place of
asking. Empty is normal and good.
```

Notes on the template:

- Record a single **Status**. Legal values: `📋 Not Started`, `🚧 In Progress`,
  `✅ Delivered`, `🛑 Superseded`. `🛑` matters — without a way out other than
  finishing, sunk cost decides.
- **Requirements** are a numbered checkbox list — `1. [ ]`, `2. [ ]`, `3. [ ]` — and
  never plain bullets. The numbers give every requirement a stable name to refer to
  in conversation, and the checkboxes let the file show progress without containing a
  plan.
- Numbers are assigned in the order written and then left alone. Append a new
  requirement at the end rather than inserting it, and if one is dropped, strike it
  through in place instead of renumbering the rest.
- **Done when** is what keeps the format honest. A spec whose `Done when` no human
  can observe is a spec that can be "finished" without anything being real.
- **Open questions** is the pressure valve that lets asking stay optional. Anything
  you decided rather than asked about goes here, phrased as the assumption you made,
  so it is visible and cheap for the user to overturn.

## Key rules (don't violate)

- **No solution in the file.** No file paths, no type names, no layers, no
  implementation sections. This is the rule the format lives or dies by.
- **On creation requests, create, then stop.** Never slide from writing specs into
  implementing them without a separate request.
- **Don't invent to fill a template.** An empty `Open questions` or
  `Explicitly not this time` is fine; a fabricated requirement is not.
- **Don't ask by reflex, and don't assume silently.** Questions are for genuinely
  ambiguous outcomes only; every assumption made instead of asking gets written
  down under `Open questions`.
- **Don't inflate to hit a count.** Three real requirements beat seven padded ones.
- **Number the requirements.** Always `1. [ ]`, `2. [ ]`, … — never unnumbered
  bullets — and keep existing numbers stable once written.
- **Don't write technical detail back into a spec later.** Decisions worth keeping
  go to `design-docs\` or a code comment. Otherwise the spec rots into a stale
  technical doc — the exact thing this format removes.

## Implementing one later

These specs hold no implementation plan of any kind, so the slicing happens at
implementation time. When asked to implement `spec 10.1`, read the existing spec 10
and implement only its numbered requirement 1; the decimal suffix is a requirement
number, not a new spec number or permission to complete the whole spec. If that
requirement depends on another unfinished requirement, resolve the dependency with
the user before expanding scope. When asked to implement a whole spec, follow the
same workflow for all its requirements:

1. Read it. If a requirement isn't checkable, or a blocking open question remains,
   resolve it before writing code.
2. Explore the codebase, then **state the technical plan in chat and the todo list —
   never in the spec file.** That is where the approach gets reviewed, at the moment
   it is actually informed and with no sunk cost attached to it.
3. Slice into the smallest sequence that keeps the build green, ordered so
   `Done when` becomes true as early as possible rather than last. Every step must
   be fully buildable on its own; do not leave a broken intermediate state for a
   later step to repair. Tests are implicit in every step, never a separate testing
   step: all logic added or changed in a step must be covered by appropriate
   automated tests before proceeding.
4. Check off only the numbered requirements that are demonstrably true, and say which
  numbers you checked. For a dotted request, leave other requirements untouched and
  mark the spec Delivered only when all its requirements are demonstrably complete.
