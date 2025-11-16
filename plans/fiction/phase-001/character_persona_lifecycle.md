# Phase 001 – Character & Lore Lifecycle Plan

## Objective
- Ensure every fiction phase (vision → blueprint → scroll → scene) explicitly plans characters and lore _before_ prose generation.
- Automatically mint and maintain `Persona`, `Agent`, `PersonaMemory`, and `FictionWorldBible*` rows the moment a character or lore pillar is declared important.
- Guarantee writing personas reuse the correct author persona memories and carry their history into every LLM call.

## Current Pain
- World bible entries are created, but they are not linked to personas/agents; provenance is missing.
- ~~Vision planner output is now structured, yet world-bible manager and consoles still need to display the roster so authors can track promotions.~~ Fiction plan roster API + console dashboards now expose tracked characters/lore with provenance; remaining gap is keeping world-bible lineage metadata in sync per branch.
- Lore requirements block scroll/scene writers when missing, and the new API/console affordances let admins mark requirements Ready with branch-aware provenance; remaining gap is automated fulfillment (world-bible prompts + audit trails) instead of manual note-taking.
- Author persona memories feed scroll/scene runners, and consoles now expose the latest memories/world notes for inspection; follow-up work focuses on obligation tagging and persona-change history.

## Success Criteria
- Vision Planner emits structured `characters[]` and `lore[]` payloads with “track?” booleans, importance scores, and continuity hooks.
- Whenever `track == true`, the job immediately creates/updates personas, agents, persona memories, and world-bible entries with provenance (plan id, pass index, scene id, author persona id).
- Scroll/Scene phases cannot start unless the prerequisites list (characters + lore) is satisfied; validators enforce this.
- Author personas are loaded with their `PersonaMemory` stack before each writing call; new outputs append memories automatically.

## Deliverables
1. **Structured Vision Output:** Template + validator changes so Phase 001 vision payload includes detailed character dossiers and lore pillars.
2. **Character Promotion Service:** Shared helper that takes planner output and mints/updates persona+agent+memory rows with provenance metadata.
3. **Lore Requirement Tracker:** Table + APIs tying `FictionWorldBibleEntry` to scroll/scene prerequisites; runners check the list before drafting.
4. **Author Persona Context Injection:** AgentService / runner changes so writer personas load their memories; new memories are appended after each pass.
5. **Tests:** Deterministic planner/runner tests asserting persona creation, lore prerequisites, and author memory usage.

## Workstreams

### A. Vision Planner Upgrade _(✅ completed 2025-11-02)_
- Extend planner prompt to include sections:
  - `coreCast`: hero/foil/support with full bios, motivations, arcs, POV flags.
  - `supportingCast`: minor characters with importance scores.
  - `loreNeeds`: factions, locations, systems plus “requiredFor” list.
- Update `FictionResponseValidator` to require these arrays.
- Persist structured data into `FictionPlanPass.Metadata`.
- Emit `createdCharacters` / `createdLore` metadata for downstream jobs.
_Status:_ Landing complete in C# runners + tests; Python feature flag retired. Future changes are additive (e.g., richer importance scoring).

### B. Character Promotion & Provenance _(✅ service + telemetry live)_
- Create `FictionCharacter` table linking `PersonaId`, `AgentId`, `FictionPlanId`, `WorldBibleEntryId`, `FirstSceneId`, `CreatedInPass`.
- Implement `CharacterLifecycleService` (shared by runners) that:
  - Looks up existing personas/agents by slug.
  - Creates personas (OwnedBy.System), binds new agents, seeds baseline persona memories (bio, voice, goals).
  - Writes provenance metadata into persona/agent/character rows.
  - Writes an initial `PersonaMemory` entry referencing the world-bible payload.
- Update Vision Planner + WorldBible Manager runners to call the service before returning success.
_Status:_ Service now mints personas/agents/memories, auto-infers world-bible provenance, and stamps branch lineage metadata that flows through roster/telemetry dashboards. Remaining work: tie world-bible fulfillment + lineage history together for audit automation.

### C. Lore Prerequisites _(🚧 partial)_
- Add `FictionLoreRequirement` table referencing plan + scroll/scene ids; status = Planned/Ready/Missing.
- Scroll/Scene runners read requirements; if missing entries, they either:
  - Auto-create via lore tool using the prompt payload, or
  - Fail early with actionable error.
- When lore is fulfilled, mark requirement “Ready” and link to `FictionWorldBibleEntry`.
_Status:_ Scroll + scene runners now block on `Planned` entries and emit telemetry; lore fulfillment can be triggered via API/console with branch-aware provenance, and dashboards show blocked vs ready totals. Remaining work: automate fulfillment tooling (world-bible prompts/assistants) and attach audit history to each requirement.

### D. Author Persona Context _(✅ runtime hydrated; UI pending)_
- Introduce `AuthorPersonaRegistry` mapping fiction projects to author personas.
- Runner helper loads:
  - Persona baseline system prompt.
  - Latest `PersonaMemory` slice (configurable window).
  - Recent world-bible notes tagged with that persona.
- After each writing call, append new persona memories capturing summary, tone, obligations, open loops.
- Tests ensure switching author personas changes prompt context.
_Status:_ Registry, prompt hydration, and automatic memory append landed in Scroll/Scene runners with deterministic tests; consoles now surface persona summaries, newest memories, and world notes. Next: obligation tagging, change history, and Ops alerts for drifting personas.

### E. Tooling & UI Hooks _(✅ roster/lore/persona/backlog APIs + console panes live; backlog action widgets pending)_
- API endpoints `/api/fiction/plans`, `/api/fiction/plans/{id}/roster`, `/lore/summary`, `/lore/{id}/fulfill`, `/backlog`, and `/author-persona` surface roster + provenance, fulfillment telemetry, persona memories, and backlog state.
- Console “Fiction Projects” page + Planner telemetry card display branch-aware rosters, blocked-lore groupings, backlog lists/resume actions, and author persona memories so admins can inspect tracked assets.
- Remaining work: backlog action widgets + alerting, fulfillment history timelines, and persona obligation lists tied back to backlog items.

## Dependencies
- Existing plan graph tables (`FictionPlan*`, `FictionWorldBible*`).
- Persona/agent tables and AgentService.
- Fiction runners already wired through Hangfire (Phase-001 Milestone C).

## Testing & Verification
- Deterministic LLM fixtures for Vision Planner, WorldBible Manager, Scene Writer.
- Tests assert:
  - Character flagged `track=true` results in persona + agent + world-bible entry.
  - Lore requirement missing blocks SceneWriter.
  - Author persona memories grow after each writing pass.
- Integration test walks vision → blueprint → scroll using stubs and inspects DB snapshots.

## Rollout Plan
1. Implement Vision planner changes behind `Fiction:VisionCharacters` feature flag.
2. Enable CharacterLifecycleService in staging; migrate existing projects by replaying latest passes.
3. Roll out lore requirement gating per branch; allow override flag until backlog cleared.
4. Flip author persona memory enforcement once regression tests pass.

## Checklist
- [x] Update Vision planner template/validator + unit tests.
- [x] Implement `CharacterLifecycleService` + EF migration for `FictionCharacter`.
- [x] Wire service into Vision + WorldBible runners.
- [x] Add `FictionLoreRequirement` tables + runner checks (scroll + scene gating live; fulfillment API/console actions live).
- [x] Inject author persona memories into AgentService writing calls (runners hydrate + append memories; console panes surface the newest memories/world notes).
- [x] Build deterministic tests covering persona creation + lore gating (branch-lineage + fulfillment propagation scenarios added).
- [ ] Automate world-bible fulfillment tooling + audit history for lore requirements.
- [ ] Document API/UI updates and handoff to console team (roster/lore/backlog/persona usage notes + alerting plan).
