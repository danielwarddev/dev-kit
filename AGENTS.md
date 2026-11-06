# Dev Kit Maintenance

## Repository Map

| Location | Purpose |
|---|---|
| `analyzers` | Analyzer packages, code fixes, validators, and their tests. See `analyzers\README.md`. |
| `templates` | Standalone .NET scaffolds and generation tests. See `templates\README.md`. |
| `scripts` | Project setup, file transfer, and package-update commands, plus script/installer tests. See `scripts\README.md`. |
| `skills` | Canonical agent skills and bundled helpers. See `skills\README.md`. |
| `config` | Files distributed to consuming projects. `config\AGENTS.md` is project guidance, not this repository's maintenance instructions. |
| `install`, `install.py` | Installer launcher and implementation. See the root README for arguments. |

## Validation

Run the smallest checks covering the change, using the section READMEs:

- Analyzer or code-fix changes: affected .NET tests; package-consumer tests when
  packaging or loading changes. Validator changes: the corresponding tool tests.
- Template changes: template generation tests. Wrapper or shared-copy changes:
  affected project-script tests.
- Pull/push changes: `test_sync_skills.py`. Installer or deployment changes:
  `test_install.py`.
- Skill helper changes: the helper's tests. Prose-only changes: inspect guidance
  and relative links; builds are unnecessary.
- Shared config changes: relevant copy/generation tests when behavior changes.

Use temporary consumers and isolated feeds, profiles, and CLI homes. Do not use
the real installation as a routine test or change user PATH, feeds, or deployed
files merely to validate source changes.

## Skill Maintenance

- Keep reusable skills free of project-specific facts; use discovery instructions
  or put those facts in the consuming project's guidance.
- Ensure referenced files and companion skills are available to the consumer,
  or provide a fallback.
- Keep framework-specific rules in their owning skill rather than general
  language guidance.
- Verify relative links after edits.
- When importing a skill from a project, copy only its Git-tracked files
  (`git ls-files`); project copies can hold ignored dependencies and caches.
- When adding or removing a skill, update the mode-specific `skills=` lists in
  `scripts\apply-project-files`, the expected skill list in
  `scripts\tests\test_scripts.py`, the root README's skill table and "Shared
  configuration" row, and the intro of `skills\README.md`.
- Skill copy exclusions are duplicated in `scripts\apply-project-files` (`tar
  --exclude`), `scripts\sync-skills_Resources\sync.py`, and `install.py`
  (`deploy_files`); keep all three in sync.

## Source and Local Deployment

This Git checkout is the editable, version-controlled source: change and test
skills, scripts, templates, and configuration here.

Run the root `install` script to deploy those changes locally. It copies a snapshot
into the configured CustomScripts parent under `dward-dev-kit`, registers templates
from that snapshot, and installs analyzer packages into the configured local feed.
The deployment is a copy, not a link to this checkout.

The separate deployment keeps everyday commands and new-project generation on the
last installed snapshot while changes are developed and evaluated in Git. Editing
the checkout does not update installed commands or templates; rerun `install` to
deploy. The checkout is not required for normal installed-command execution.

Project-local copies can be edited independently. `pull-project-files` replaces
them from the installed snapshot; `push-project-files <dev-kit-repo>` sends changes
back to this checkout. Pushing does not deploy them or update other projects.

Do not install/deploy automatically unless requested. Installation changes local
tooling, template registrations, NuGet configuration/feed contents, and shell PATH.
