Prompt for next session (2025-11-16)
------------------------------------

Status recap
- Lore fulfillment is no longer a database chore: `/api/fiction/plans/{id}/lore/{requirementId}/fulfill` records branch-aware provenance, emits lifecycle telemetry, and the console roster + lore summary cards update in real time.
- Backlog and resumes are first-class citizens: `/api/fiction/plans/{id}/backlog` + `/resume` hydrate `ConversationTask` metadata, reset `FictionPlanBacklog` state, and hand control back to `FictionBacklogScheduler` with enforced provider/model/task IDs.
- Branch-aware rosters, blocked-lore dashboards, and author persona memory panes are live in both Fiction Projects and Planner Telemetry, giving admins a full picture of canon health per branch.
- Author personas now have end-to-end visibility: registry + prompt hydration were already in place, and the console surfaces persona summaries, latest memories, and world notes so obligations stay transparent.
- Deterministic tests cover lineage propagation, fulfillment flows, and the backlog metadata contract so regressions in provenance or resume handling get caught immediately.

Next targets
1. **Automate lore fulfillment + audit history:** Drive world-bible prompts/tooling when requirements are marked Blocked, persist fulfillment timelines, and expose “who/when/how” audits in both API and console (`plans/fiction/phase-001/plan-first-draft.md`, `character_persona_lifecycle.md`).
2. **Console backlog actions + notifications:** Wire the new backlog/resume API into UI controls, add action logs/alerts, and ensure console resumes collect provider/model metadata before hitting Hangfire.
3. **Author persona obligations:** Layer obligation tagging/change history onto the persona panes, add Ops alerts for persona drift, and thread obligations back into backlog items so follow-ups remain visible.
4. **Telemetry + drift monitoring:** Publish backlog vs `FictionPhaseProgressed` drift, resume-success/timeout counters, and lore-fulfillment metrics to Ops dashboards; hook telemetry events back to console actions for auditing.
5. **Regression suite expansion:** Add deterministic tests for backlog resumes (API -> scheduler -> Hangfire), lore fulfillment history, and persona obligation workflows before moving toward Milestone D feature unlocks.

Getting started
- Re-read `plans/fiction/phase-001/plan-first-draft.md` and `plans/fiction/phase-001/character_persona_lifecycle.md` to align on the refreshed priorities/checklists.
- Catalog outstanding backlog items via the new `/api/fiction/plans/{id}/backlog` endpoint and decide which ones should be surfaced or auto-resumed in the console.
- Sketch automation for world-bible fulfillment + persona obligations so design and telemetry requirements are clear before implementation.
- Check `plans/hot_targeted_todo.md` for metadata/backlog TODOs that now unblock after the recent API/console work.
