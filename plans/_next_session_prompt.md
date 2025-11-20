Prompt for next session (2025-11-18)
------------------------------------

Definition of Done
- Backlog guardrails flag/auto-resume stale items per SLA with provider/model validation enforced server + console side, and telemetry proves the flows generate alerts.
- Persona obligations exist end-to-end (model, API, scheduler, console actions with audit notes) and at least one staging scenario exercises resolve/dismiss.
- Console workflow alerts + action feeds call out backlog/lore/obligation drift without relying on external dashboards, with regression tests covering resume → scheduler → lore job loops.

Status recap
- Lore fulfillment automation is live: the scheduler now queues Hangfire lore jobs when requirements linger in `Blocked`, the runner writes world-bible entries + marks requirements Ready, and API/console surfaces show fulfillment history automatically.
- Backlog resumes remain metadata-safe: `/api/fiction/plans/{id}/backlog` + `/resume` enforce provider/model/task IDs, console backlog panels include resume controls plus action logs, and telemetry/workflow events stay in sync.
- Branch-aware rosters, lore summaries, and author persona panes continue to reflect canon health in real time; lore history now appears directly in the roster so admins can see who fulfilled what and when.
- Deterministic tests cover lore automation (scheduler ➝ job ➝ world bible entry) alongside the existing backlog contract/resume flows, keeping Hangfire + API behavior anchored.
- **ScopePath Factory Lockdown** is complete: direct `ScopePath` construction is banned via analyzers, and the factory is restricted to internal builders.
- **Regression Expansion**: `FictionResumeRegressionTests` now covers the `resume -> scheduler -> Hangfire -> lore job` loop end-to-end.

Next targets

1. **Backlog guardrails + stale policies:** auto-resume or escalate when backlog items sit `InProgress` past SLA, enforce provider/model expectations during `/resume`, and surface branch dependency chains (blocked scenes, persona obligations) in the console.
2. **Persona obligation loop:** introduce an obligation model tied to personas/backlog items, emit obligations when planners flag continuity hooks, and add UI actions to resolve obligations with audit logs.
3. **Console workflow alerts:** add backlog/lore alert banners, notification hooks for automation events, and richer action feeds so admins can spot drift without opening telemetry dashboards.
4. **Regression expansion (continued):** add persona obligation/regression tests before moving toward Milestone D unlocks.

Getting started

- Re-read `plans/fiction/phase-001/plan-first-draft.md` + `character_persona_lifecycle.md` for the updated automation notes and open checklist items.
- Inventory stale backlog items via `/api/fiction/plans/{id}/backlog`; decide which should auto-resume vs alert, and document provider/model expectations per phase for the upcoming `/resume` validator enhancements.
- Sketch the persona obligation schema and console UX so obligation + guardrail work can progress in parallel.
- Check `plans/hot_targeted_todo.md` for backlog/metadata tasks that now unblock with lore automation landed.
