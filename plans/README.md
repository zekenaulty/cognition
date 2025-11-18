# Plans Folder Guide

A lightweight, shared workflow for planning work, tracking steps, and keeping an audit trail that can be read by humans and assistants across machines and conversations.

## Quick Start
- Create/choose a plan file in `plans/` (one per initiative).
- Take a pre-change snapshot of the repo state.
- Do work in discrete steps; each step gets its own step note file.
- Keep a scratchpad for quick notes and cross-assistant handoffs.

## Maintaining a Clear Definition of Done
- Every plan must capture a concise, testable definition of done (DoD) up front: what user-facing change ships, what data/API contracts stabilize, and what verification proves it works.
- Keep the DoD realistic—opt for thin vertical slices that can land inside a sprint/iteration instead of ever-growing horizontal work. If new scope appears, log it in the backlog or a follow-up plan unless it blocks the current DoD.
- Tie each step note back to the DoD. If an action does not directly close a DoD checkbox, call out why (e.g., prerequisite, bug fix uncovered while verifying).
- Closure means more than code written: API + console cleaned up, tests/telemetry updated, docs/readmes touched, and staging exercised. Leave no ambiguous “we’ll finish that later” gaps.

## Folder Structure
- Plan documents: `plans/<plan-key>.md`
- Step notes: `plans/<plan-key>_step_YYYYMMDD_HHMM_{topic}.md`
- Snapshot step note: `plans/<plan-key>_step_snapshot.md`
- Optional scratchpad: `plans/_scratchpad.md` or `plans/_scratch/`

Example (existing):
- `plans/personaId_to_agentId_refactor.md`
- `plans/personaId_to_agentId_refactor_step_snapshot.md`
- `plans/personaId_to_agentId_refactor_step_20250919_0004_db_migration_todo.md`

## Naming Conventions
- `<plan-key>`: short, kebab- or snake-case, stable (e.g., `personaId_to_agentId_refactor`).
- Step filename: `_<plan-key>_step_YYYYMMDD_HHMM_{topic}.md`
  - Time in local time (24h) for readability; topic is short slug (e.g., `api_dto_updates`).
- Use ✅/❌ for completion inside the step file, not in the filename.

## Snapshot Workflow (arc.py + Git)
- Create a Markdown snapshot of the repo (human-auditable):
  - `python ./arc.py`
  - Output: `..\<repo-name>.<YYYYMMDD_HHMMSS>.md` (in the repo’s parent folder).
  - arc.py honors `.gitignore` and excludes common binaries.
- Label the exact repo state with a Git tag:
  - `git tag <label>` (e.g., `git tag persona_to_agent_pre_migration`)
- Record metadata to the snapshot step note:
  - Label, timestamp, branch, commit, file count, snapshot file path.
- Diff later:
  - Code/config: `git diff <label>..HEAD`
  - Names only: `git diff --name-status <label>..HEAD`
  - Optional: generate another `arc.py` snapshot and diff the two Markdown files.
- Restore:
  - Surgical (paths): `git restore --source <label> -- src/** "migrations/**"`
  - Full: `git reset --hard <label>`

## Creating a Plan
- Add `plans/<plan-key>.md` with:
  - Objective (what and constraints)
  - Scope & out-of-scope
  - Deliverables
  - Data/API/Service/UI changes (as needed)
  - Migration strategy & rollout order
  - Testing/verification
  - Risk/rollback
  - Worklog protocol (step notes, naming, required fields)
  - Checklist (box-tickable)

Template snippet:
```
# <Plan Title>

Objective
- ...

Deliverables
- ...

Checklist
- [ ] Take snapshot (record metadata)
- [ ] Implement X
- [ ] Verify Y
- [ ] Cutover and cleanup
```

## Managing Step Notes
- Each discrete action gets its own step note: `plans/<plan-key>_step_YYYYMMDD_HHMM_{topic}.md`
- Required sections for every step:
  - Goal
  - Context (optional)
  - Commands Executed (exact commands)
  - Files Changed (paths only)
  - Tests / Results
  - Issues
  - Decision
  - Completion (✅/❌)
  - Next Actions

Step template:
```
Goal
- ...

Context
- ...

Commands Executed
- cmd 1
- cmd 2

Files Changed
- path/to/file

Tests / Results
- ...

Issues
- ...

Decision
- ...

Completion
- ✅ or ❌

Next Actions
- ...
```

## Scratchpad (Shared, Low-Ceremony Notes)
- Use for quick thoughts, links, partial commands, or TODOs that don’t warrant a full step note.
- Options:
  - Single file: `plans/_scratchpad.md`
  - Folder per collaborator: `plans/_scratch/<handle>/YYYYMMDD.md`
- Conventions:
  - Timestamp chunks with `## 2025-09-19 09:15` style headings.
  - Keep secrets elsewhere (.env, secret store). Scratchpad is committed.
  - Link to related step notes or commits when promoting notes to formal steps.

Scratchpad snippet:
```
## 2025-09-19 09:15 (zythis)
- Draft commands for vector backfill
- Cross-check ContentHash calc across services
```

## Sharing Across Assistants and Workstations
- Commit and push `plans/` with the code branch; treat it as first-class.
- Use descriptive branches for initiatives (e.g., `plan/persona-agent-refactor`).
- When switching machines:
  - Pull the branch; open `plans/<plan-key>.md` to reorient.
  - Review the latest step notes and scratchpad entries.
- When switching assistants/conversations:
  - Reference the exact step note as handoff context.
  - Continue with a new step note rather than editing prior steps (immutability of history).

## Daily Rhythm
- Start: read the plan + last 1–4 step notes.
- During work: keep a scratchpad open; cut a new step note when action begins.
- End: finalize step note (✅/❌), link commits/PRs, update plan checklist.

## Do / Don’t
- Do record exact commands; avoid paraphrasing.
- Do keep filenames and timestamps consistent.
- Do prefer new step notes over rewriting history.
- Don’t put secrets in `plans/`.
- Don’t mix multiple actions in one step note.

## References (Examples in this repo)
- Plan: `plans/personaId_to_agentId_refactor.md`
- Snapshot step: `plans/personaId_to_agentId_refactor_step_snapshot.md`
- Step note: `plans/personaId_to_agentId_refactor_step_20250919_0004_db_migration_todo.md`
