Prompt for next session (2025-11-15)
------------------------------------

Status recap
- Vision planner template + validator now emit structured `coreCast`/`supportingCast`/`loreNeeds` payloads with deterministic tests, and lifecycle parsers consume them.
- Scroll + scene runners enforce lore requirements up front; blocked runs emit `FictionLoreRequirementBlocked` telemetry instead of silently drafting.
- A new `AuthorPersonaRegistry` hydrates scroll/scene prompts with persona summaries + recent memories/world notes and appends new `PersonaMemory` entries after each pass.
- FictionWeaver jobs now enforce `conversationPlanId` + provider/model/task metadata so Hangfire, backlog items, and console resumes share the same authoritative contract (tests updated).
- CharacterLifecycleService now promotes personas/agents/memories with provenance, emits `fiction.backlog.telemetry`, and the new fiction roster API/console pages expose tracked characters + lore to admins.

Next targets
1. **World-bible provenance + lore fulfillment:** Wire branch lineage onto roster entries, expose ready/blocking lore dashboards, and define how fulfillment flows surface in the console (`plans/fiction/phase-001/plan-first-draft.md`, `character_persona_lifecycle.md`).
2. **Metadata contract everywhere:** Ensure console/front-end submits `conversationPlanId`/provider/model/task/backlog IDs for every run/resume, and keep emitting `fiction.backlog.telemetry` so UI paths are observable.
3. **Backlog-driven scheduling/resume UX:** Finish letting Hangfire enqueue strictly from backlog state, expose backlog/progress via API, and add resume success/drift metrics.
4. **Author persona visibility:** Surface new memories, world notes, and obligations directly in the console so writers can inspect what AuthorPersonaRegistry appends each pass.
5. **Test coverage:** Expand deterministic tests for lifecycle enforcement (world-bible manager, resume scenarios, backlog transitions) before moving toward Milestone D tooling.

Getting started
- Skim `plans/fiction/phase-001/plan-first-draft.md` + `plans/fiction/phase-001/character_persona_lifecycle.md` for the latest checklists and completed items.
- Review `plans/hot_targeted_todo.md` for any backlog/metadata TODOs that need updates after the recent telemetry/roster work.
- Sketch the remaining world-bible provenance + lore fulfillment hooks under the same plan so both world-bible and author-console efforts share context.
- Double-check `plans/README.md` if you need a refresher on how we organize plan docs before diving in.
