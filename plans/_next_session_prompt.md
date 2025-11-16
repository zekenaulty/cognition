Prompt for next session (2025-11-10)
------------------------------------

Status recap
- Vision planner template + validator now emit structured `coreCast`/`supportingCast`/`loreNeeds` payloads with deterministic tests, and lifecycle parsers consume them.
- Scroll + scene runners enforce lore requirements up front; blocked runs emit `FictionLoreRequirementBlocked` telemetry instead of silently drafting.
- A new `AuthorPersonaRegistry` hydrates scroll/scene prompts with persona summaries + recent memories/world notes and appends new `PersonaMemory` entries after each pass.
- FictionWeaver jobs now enforce `conversationPlanId` + provider/model/task metadata so Hangfire, backlog items, and console resumes share the same authoritative contract (tests updated).
- CharacterLifecycleService now promotes personas/agents/memories with provenance, emits `fiction.backlog.telemetry`, and the new fiction roster API/console pages expose tracked characters + lore to admins.

Next targets
1. Close the loop on world-bible provenance + lore fulfillment (branch lineage, ready-state dashboards) now that the roster API/console surfacing is live (`plans/fiction/phase-001/plan-first-draft.md`, `character_persona_lifecycle.md`).
2. Update console + front-end flows to always pass the metadata contract (conversationPlanId/provider/model/task/backlog IDs) and keep instrumenting UI paths with the new telemetry feed.
3. Extend backlog scheduling/resume UX: have Hangfire enqueue strictly off backlog state and expose plan/backlog progress via API.
4. Document and expose author persona context in the consoles (new memories, world notes, tracked obligations) so writers can inspect what the registry is appending.
5. Keep building tests around lifecycle + backlog enforcement (world-bible manager, resume scenarios) before moving to Milestone D tooling.

Getting started
- Read `plans/fiction/phase-001/plan-first-draft.md` + `plans/fiction/phase-001/character_persona_lifecycle.md` for the refreshed context/checklists.
- Audit the console/UI backlog flows to ensure they populate the new metadata contract; capture gaps in `plans/hot_targeted_todo.md`.
- Plan the remaining lifecycle hooks (world-bible provenance + lore fulfillment UI) and note the work under the same plan so world-bible + author console teams can consume it next session.
- refer to plans\README.md for overview of how to the plans directory
