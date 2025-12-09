# Plans Folder Guide

A lightweight, shared workflow for planning work, tracking steps, and keeping an audit trail that can be read by humans and assistants across machines and conversations. These are instructions for agents: keep plans aligned with code, and update every step when completed.

## Quick Start
- Create/choose a plan file in `plans/` (one per initiative).
- Do work in discrete steps; each step MUST get its own step note file.
- Keep a scratchpad for quick notes and cross-assistant handoffs.

## Maintaining a Clear Definition of Done
- Every plan must capture a concise, testable definition of done (DoD) up front: what ships, what contracts stabilize, and what verification proves it works.
- Keep the DoD realistic—opt for thin vertical slices. New scope belongs in backlog/follow-up unless it blocks the current DoD.
- Tie each step note back to the DoD. If an action doesn’t close a DoD item, say why (prereq, bug found during verify).
- Closure means more than code: API/console cleaned up, tests/telemetry updated, docs/readmes touched, and staging exercised. No “we’ll finish later” gaps.

## Folder Structure
- Plan documents: `plans/<plan-key>.md`
- Step notes: `plans/<plan-key>_step_YYYYMMDD_HHMM_{topic}.md`
- Snapshot step note: `plans/<plan-key>_step_snapshot.md`
- Optional scratchpad: `plans/_scratch/<plan-key>_step_YYYYMMDD_HHMM_{topic}.md`

## Naming Conventions
- `<plan-key>`: short, kebab- or snake-case (e.g., `personaId_to_agentId_refactor`).
- Step filename: `_<plan-key>_step_YYYYMMDD_HHMM_{topic}.md` (24h local time; short topic slug).
- Use ✅/❌ for completion inside the step file, not in the filename.

## Creating a Plan
- Add `plans/<plan-key>.md` with:
  - Objective
  - Scope & out-of-scope
  - Deliverables
  - Data/API/Service/UI changes (as needed)
  - Migration/rollout order
  - Testing/verification
  - Risk/rollback
  - Worklog protocol (step notes, naming, required fields)
  - Checklist (box-tickable)

## Managing Step Notes
- One discrete action per step note: `plans/<plan-key>_step_YYYYMMDD_HHMM_{topic}.md`
- Every step must be updated by the agent when completed.
- Required sections:
  - Goal
  - Context
  - Commands Executed (exact commands)
  - Files Changed (paths only)
  - Tests / Results
  - Issues
  - Decision
  - Completion (✅/❌)
  - Next Actions

## Scratchpad (Shared, Low-Ceremony)
- Use for quick thoughts, links, partial commands, or TODOs that don’t warrant a full step note.
- Options: `plans/_scratchpad.md` or `plans/_scratch/<handle>/YYYYMMDD.md`
- Conventions: timestamp chunks (`## 2025-09-19 09:15`), no secrets, link to related steps/commits when promoting notes.

## Sharing Across Assistants and Workstations
- Commit and push `plans/` with the code branch; treat it as first-class.
- When switching machines: pull, open the plan file, read latest step notes/scratchpad.
- When switching assistants: hand off with the exact step note; start a new step note rather than editing prior history.

## Daily Rhythm
- Start: read the plan + last 1–4 step notes.
- During work: keep scratchpad open; start a new step note when action begins.
- End: finalize step note (✅/❌), link commits/PRs, update plan checklist.

## Do / Don’t (for agents)
- Do record exact commands; avoid paraphrasing.
- Do keep filenames/timestamps consistent.
- Do prefer new step notes over rewriting history.
- Do keep plans aligned with code; update steps immediately when work completes.
- Don’t put secrets in `plans/`.
- Don’t mix multiple actions in one step note.
- Do be explicit when referencing code: cite class/filename/method paths and evidence of behavior; no vague “lip service” summaries.
  - Rationale: vague statements like “fixed planner health alerts” without file/method references or test evidence create ambiguity, hide drift, and break handoffs. Always cite exact paths (e.g., `src/Cognition.Api/Infrastructure/Planning/PlannerHealthService.cs:630`), what changed, and how it was verified (tests run, telemetry observed).
  - 
- Capture scope/context changes as you work (static RAG in `plans/`) so later sessions can anchor. Always.

Keep new code small: controllers delegate to services; React pages split into hooks + presentational components. Use `ScopePathBuilder` for identity, prefer agent-first defaults, and keep admin/public surfaces clearly separated.
