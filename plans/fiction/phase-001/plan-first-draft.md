# Fiction Iteration Phase 001

## Objective (Pre-Alpha Reality)
- This is a pre-alpha fiction weaver. Only work that unblocks the living story loop (personas, lore, planners, author UX) is worth doing; no table migrations or dashboards unless they directly support that loop.
- Run the entire `iterate-book` chain (vision ? iterative passes ? blueprint ? scroll ? woven prose ? world bible updates) inside Cognition with the same guardrails as the Python prototype.
- Persist every phase's chain-of-thought, checkpoints, transcripts, metrics, and branch lineage so assistants can resume or fork safely.
- Treat characters and lore as first-class data: planning phases must declare them up front, mint personas/agents immediately, and keep canon in sync.
- Keep author personas, tool prompts, and world bible data under configuration so the experience is reproducible across jobs, consoles, and assistants.

## Current Architecture Snapshot (Nov 2025)
- **Phase runners** live in `src/Cognition.Clients/Tools/Fiction/Weaver/` (VisionPlannerRunner, IterativePlannerRunner, ChapterArchitectRunner, ScrollRefinerRunner, SceneWeaverRunner, WorldBibleManagerRunner) and inherit from `FictionPhaseRunnerBase` for logging + validations.
- **Job orchestration** is handled by `FictionWeaverJobs`/`FictionWeaverJobClient` (Hangfire). They lock checkpoints (`FictionPlanCheckpoint`), publish `FictionPhaseProgressed` events, and write transcripts via `FictionPlanTranscript`.
- **AgentService** backs every LLM call (persona-aware chat, instruction stages, SignalR logging). Personas exist, but new fiction characters are not minted automatically yet.
- **Data layer** already includes the plan graph tables (`FictionPlan*`, chapter structures, story metrics, world bible entries). World-bible payloads are stored as structured JSON and linked to scrolls/scenes.
- **Reference spec** (`reference/iterate-book.py`) remains the parity oracle for prompts, schemas, retry semantics, and scene rewrites.
- **Character/lore lifecycle plan** lives in `plans/fiction/phase-001/character_persona_lifecycle.md` and describes how planners must promote characters + lore into personas/agents/world-bible entries.

## State of Play
- C# runners + Hangfire orchestration are still live end-to-end, but we now have structured vision payloads (core/supporting cast + lore arrays with `track` flags) and schema validation/tests to back them.
- `CharacterLifecycleService` handles persona/world-bible promotion and scroll + scene runners refuse to run when `FictionLoreRequirement` entries remain `Planned`, so lore gaps surface before prose is attempted.
- Author persona context now flows through an `AuthorPersonaRegistry`; scroll/scene prompts load persona summaries + memories/world notes and automatically append new `PersonaMemory` entries after each pass.
- FictionWeaver jobs enforce backlog metadata contracts (conversationPlanId, provider/model IDs, backlog task IDs) and fail early if clients omit them, keeping Hangfire + UI in sync.
- Remaining gaps sit in tooling (UI/console still needs backlog/task plumbing), world-bible provenance wiring, and surfacing of tracked characters/lore inside author dashboards.

## Success Criteria
- Vision/iterative planners emit detailed `characters[]` and `lore[]` payloads, flag importance, and automatically spin up personas/agents/memories when needed.
- Every fiction phase records its context (prompt template, instruction set, scope path) plus branch lineage so branches can resume/fork safely.
- Scroll/scene writers refuse to run unless required lore + character assets exist (or they create them via the lifecycle service).
- Author personas always speak with their stored memories; new prose automatically appends memories, world bible diffs, and canon obligations.
- Consoles/agents can inspect characters, lore, checkpoints, branches, and transcripts via API.

## Immediate User-Facing Priorities (Nov 2025)
1. **Backlog-driven orchestration + resume UX:** Server-side guardrails now reject missing `conversationPlanId`/provider/model/task IDs; finish the `FictionPlanBacklog` work so Hangfire jobs and `ConversationTask` scheduling derive entirely from backlog state and ensure front-ends always send the required metadata (`phase-001_step_20250926_2327_inventory.md`, `hot_targeted_todo.md`). This keeps console progress, transcripts, and auto-resume flows aligned with what users initiated.
   - _User impact:_ Authors see phase cards progress exactly in the order they kicked off, resumes land in the right scene, and transcripts stop drifting from backlog truth.
   - _Work to land:_ Wire backlog state into `FictionWeaverJobs` enqueueing + `ConversationTask` scheduling, update UI/API contracts to require plan/provider/model IDs, migrate existing backlog items, and add guardrails that fail jobs lacking metadata.
   - _Proof/telemetry:_ Dashboards comparing backlog status vs `FictionPhaseProgressed` events, resume-success counters, and alerting when UI posts missing identifiers.
2. **Character & lore lifecycle enforcement:** Structured vision output, lifecycle service hooks, and scroll/scene gating are in place; next is wiring world-bible manager + UI so any `track=true` payload immediately surfaces personas/agents/memories/world-bible entries with provenance (`character_persona_lifecycle.md`). Authors should see a living roster with provenance instead of guessing which assets exist.
   - _User impact:_ Character sheets, lore pillars, and drafting prerequisites become visible before prose starts, preventing canon drift and surprise failures mid-scroll.
   - _Work to land:_ EF migrations + service implementation, runner integrations (Vision, WorldBible, Scroll, Scene), provenance metadata, lore requirement enforcement/auto-fill, and deterministic tests proving persona creation.
   - _Proof/telemetry:_ Console roster fed by new APIs, lifecycle logs per character, blocking errors when prerequisites missing, and test snapshots attached to Milestone H.
3. **Author persona context hydration:** Inject author persona memories (baseline prompt + latest `PersonaMemory` window + recent world-bible deltas) into every writing call and append memories after each pass so prose keeps a consistent voice and exposes obligations for subsequent edits.
   - _User impact:_ Writers experience a stable tone/voice per author persona; edits carry previous obligations and world bible reminders without manual copy-paste.
   - _Work to land:_ Author persona registry, AgentService prompt hook, memory window selection, automatic memory append after each pass, and console surfacing of newest obligations.
   - _Proof/telemetry:_ Persona-memory diff logs per SceneWeaver run, regression tests swapping personas, and UX validation clips showing tone consistency.
4. **Console/API surfacing for canon assets:** Build the `/projects/{id}/characters` and lore requirement endpoints plus console tabs/dashboards that show character status, first appearance, pending lore, and branch lineage. This is the first tangible UI the author will use to trust the lifecycle plumbing.
   - _User impact:_ Authors can inspect who is tracked, whether lore is missing, and which branch each asset belongs to without digging through raw transcripts.
   - _Work to land:_ API controllers/DTOs, lineage queries, console tabs/cards, and alert banners that link missing requirements back to lifecycle enforcement.
   - _Proof/telemetry:_ Usage metrics on the new tabs, health checks confirming roster vs DB parity, and author-reported issues dropping in console feedback.

## Author-Facing Acceptance Scenarios
1. **"Resume right where I left off" rehearsal**
   - Setup: start a plan, pause after Blueprint, capture `FictionPlanBacklog` + console state.
   - Expectation: backlog + `FictionPhaseProgressed` stay in sync, resume button requeues the correct `ConversationTask`, and telemetry shows matching plan/provider/model IDs.
   - Evidence: console recording, Hangfire job log, and resume-success metric incrementing.
2. **"Characters exist before prose" roster check**
   - Setup: run Vision -> WorldBible while flagging `track=true` on two characters and one lore pillar.
   - Expectation: CharacterLifecycleService mints personas/agents/memories, `FictionLoreRequirement` rows appear, and ScrollRefiner refuses to start until requirements mark Ready.
   - Evidence: API `/projects/{id}/characters` response, console roster screenshot, failing validator log before requirements satisfied, passing log after fulfillment.
3. **"Author voice stays consistent" tone pass**
   - Setup: select two author personas, run SceneWeaver twice on the same scene with each persona.
   - Expectation: prompts show the correct memory window, outputs include the persona’s tone notes, and persona memories append deltas after each run.
   - Evidence: AgentService prompt trace, PersonaMemory table diff, UX review highlighting tone differences.
4. **"Show me canon health" dashboard walkthrough**
   - Setup: open new console tabs for Characters and Lore Requirements on an active plan with branches.
   - Expectation: cards list status (Planned/In Draft/Ready), first appearance, linked branch IDs, and highlight missing lore with actionable CTAs.
   - Evidence: analytics events for tab usage, screenshot of alert banner, API log proving roster/DB parity check succeeded.

## Coordination & Kickoff Tasks (Nov 2025)
1. **Hangfire + front-end backlog metadata contract (blocks Scenario #1):**
   - Deliverables: shared spec doc for `ConversationTask` payload (planId, backlogItemId, provider, model, resumeToken), API contract changes for console, and job-queue validation that rejects missing metadata.
   - Owners: Hangfire orchestration lead + Console/UX lead; schedule a joint design review and a sandbox rehearsal capturing console + job logs.
   - Telemetry hooks: emit `FictionBacklogContractMissing` events when metadata absent, add resume-success/timeout counters, and annotate dashboards with backlog vs `FictionPhaseProgressed` drift.
2. **CharacterLifecycleService + lore requirement migrations (blocks Scenarios #2 & #4):**
   - Deliverables: EF migrations for `FictionCharacter` + `FictionLoreRequirement`, service implementation with provenance logging, runner integrations (Vision, WorldBible, Scroll, Scene), and initial telemetry events (`FictionCharacterPromoted`, `FictionLoreRequirementBlocked`).
   - Owners: Data/EF engineer + Fiction runner engineer + Console/API engineer for roster endpoints.
   - Telemetry hooks: add structured logs when personas/lore are minted, counters for blocked scroll/scene runs, and console analytics for roster tab usage.

### Backlog Metadata Contract ? Spec Draft
| Field | Source of Truth | Required? | Validation & Notes |
| --- | --- | --- | --- |
| `planId` | `FictionPlan` | Yes | Must exist + match backlog item; reject otherwise. |
| `backlogItemId` | `FictionPlanBacklogItem` | Yes | Status transitions enforced (Pending -> InProgress -> Complete). |
| `provider` | Console request | Yes | Must be in allowed provider list per environment. |
| `model` | Console request | Yes | Must map to provider; persisted for telemetry. |
| `resumeToken` | Hangfire | Optional (Resume only) | Required when resuming mid-phase; tied to checkpoint id. |
| `authorPersonaId` | Console/Project | Optional (writing phases) | Validated against project registry; ensures memory hydration. |

- API updates: `POST /api/fiction/plans/{id}/tasks` accepts the full payload and responds with the derived backlog state.
- Job queue guard: `FictionWeaverJobs` fails fast if any required field missing/mismatched and emits `FictionBacklogContractMissing`.
- Console UX: disable run/resume buttons until client gathers metadata; show inline errors sourced from the new API response.
- Telemetry: dashboard tile comparing backlog item counts vs Hangfire queue, plus alert when drift >1 item for >5 minutes.

### CharacterLifecycleService & Lore Requirement Kickoff
1. **EF migrations**
   - Add `FictionCharacter` (PersonaId, AgentId, PlanId, WorldBibleEntryId, FirstSceneId, CreatedInPass, Slug, ProvenanceJson).
   - Add `FictionLoreRequirement` (PlanId, ScrollId/SceneId nullable, RequirementSlug, Status enum, WorldBibleEntryId, CreatedByPass, Notes).
   - Seed statuses (`Planned`, `Blocked`, `Ready`), indexes on PlanId + Slug.
2. **Service contract**
   - `CharacterLifecycleService.ProcessAsync(FictionPlan plan, IEnumerable<CharacterPayload> characters, LifecycleContext context)`.
   - Responsibilities: slug normalization, persona lookup/creation, agent binding, PersonaMemory bootstrap, telemetry emit.
3. **Runner integrations**
   - Vision + WorldBible: call service immediately after payload validation.
   - Scroll/Scene: query `FictionLoreRequirement`; if any `Blocked`, raise actionable exception and/or auto-trigger lore tool.
4. **API/Console surfacing**
   - `/api/fiction/projects/{id}/characters` + `/lore-requirements` endpoints returning lineage + status.
   - Console tabs consume these endpoints; highlight pending requirements required for Scenario #4.
5. **Telemetry & testing**
   - Emit `FictionCharacterPromoted`, `FictionCharacterUpdated`, `FictionLoreRequirementBlocked/Ready` events.
   - Deterministic tests verifying persona creation + lore gating before enabling feature flag.

## Milestones & Tracks (Pre-Alpha gating)

### Milestone 0 ? Snapshot & Guardrails _(Complete)_
1. ? arc snapshot + git tag recorded for Phase 001 baseline.
2. ? Feature flags (`Fiction.Weaver.*`) defaulted off; rollout doc in `planning_the_planner_rollout_recipe.md`.
3. ? Persona-based fallbacks catalogued; migration plan tracked in scope-token refactor doc.
4. ? Operational guardrails (retry budgets, token caps, escalation contacts) captured alongside Hangfire job sheet.

### Milestone A ? Baseline Capture _(Complete)_
- Prompts, schema validators, retry rules, attentional gates, metrics, and python artifacts inventoried (`phase-001_step_20250926_2327_inventory.md`).

### Milestone B ? Data Model & Persistence _(Complete)_
1. ? ERD + EF POCOs (`FictionPlan*`, world bible, checkpoints, transcripts, story metrics).
2. ? Migrations deployed (plan graph + checkpoint resume/cancel).
3. ? Transcript storage + validation diagnostics wired through `FictionWeaverJobs`.
4. ? Optional `_context` importer (backlog / nice-to-have).
5. ? Legacy fiction tables reconciliation (PlotOutline, Draft, WorldAsset) still pending decision.

### Milestone C ? Weaver Orchestration Service _(In progress)_
1. ? Hangfire jobs + `IFictionWeaverJobClient` enqueued per phase with provider/model metadata.
2. ? Phase runners audit prompts + validations; transcripts stored per attempt.
3. ? Checkpoint resume/cancel path operational; branch metadata recorded.
4. ? Persona/lore lifecycle hooks still missing; see Milestone H.
5. ? SceneWeaver prose validation + telemetry gating (token spend, retries) still open.
6. ? Backlog-driven `ConversationTask` scheduling + front-end resume metadata capture so UI progress mirrors plan backlog state (supports Immediate Priority #1; blocked until backlog + UI contracts land).

### Milestone D ? Tool Alignment & Workflow Glue _(Blocked by Milestone H)_
1. Update Outliner/Chapter tools to read/write `FictionChapterBlueprint` beats.
2. Update SceneDraftTool to consume scroll metadata and emit `FictionStoryMetric`.
3. Wire LoreKeeper/Worldbuilder to `FictionWorldBible` entries (with lineage UI).
4. Introduce FactChecker/NPC pipelines that annotate canon and feed timelines.
5. Surface admin/branch operations in API + console tooling, including the character/lore dashboards called out under Immediate Priority #4.

### Milestone E ? Knowledge, Reflection, Branching _(Not started)_
1. Persist chapter memory summaries + persona memories with windowing helpers.
2. Structured world-bible diffing per chapter.
3. Branch heuristics + automatic fork/merge helpers.
4. Timeline synchronization tied to scenes/scrolls.
5. Mandatory rewrite/consolidation phase that cleans duplicated beats.

### Milestone F ? Retrieval Guardrails & Observability _(Ongoing)_
1. Enforce ScopeToken usage across all runners/tools (agent + conversation isolation).
2. Policy toggles for agent fallback + default behavior documented.
3. Remove residual persona-ID routes in API (Image Lab done; others pending).
4. Build dashboards for phase duration, retries, schema failures, embeddings, token spend.
5. Deterministic integration tests (LLM stubs) covering each phase.

### Milestone G ? Prompt Playbook & Templating _(Not started)_
1. Populate `Cognition.Data.Relational.Modules.Prompts` with versioned templates and plays.
2. Wire templating engine so runners pull prompts by ID + version.
3. Provide tooling to diff/promote prompt versions.

### Milestone H ? Character & Lore Lifecycle _(New)_
- Reference: `plans/fiction/phase-001/character_persona_lifecycle.md`.
1. Vision planner emits detailed character + lore payloads with track flags.
2. CharacterLifecycleService mints/upgrades personas, agents, persona memories, and world-bible entries with provenance (Immediate Priority #2).
3. Lore requirements tracked per scroll/scene; writers block until satisfied and expose actionable errors/auto-fill options to the author consoles (Immediate Priority #2 + #4).
4. Author persona registry + memory hydration wired into scroll/scene prompts, including automatic memory append + surfacing in the console UI (Immediate Priority #3).
5. Tests assert persona/lore creation + author memory updates.

## Open Issues & Checklist
- [ ] `_context` importer + reconciliation report.
- [ ] Legacy fiction table reconciliation / migration or retirement plan.
- [ ] Persona/lore lifecycle (Milestone H) implemented and hooked into Vision/WorldBible runners (vision/scroll/scene hooks landed; world-bible + roster surfacing still open).
- [ ] SceneWeaver prose validation + telemetry gating (token budgets, retries, metrics).
- [ ] Tool alignment (Milestone D) ? Outliner, SceneDraft, LoreKeeper, Worldbuilder wired to new tables.
- [ ] Knowledge/branching intelligence (Milestone E) ? memories, diffing, rewrite pass, timeline mapping.
- [ ] Retrieval guardrails ? ensure ScopeToken usage + remaining persona routes removed.
- [ ] Prompt playbook/templating landed with versioned prompts + promotion pipeline.
- [ ] Backlog-driven orchestration + resume UX polish so user-facing progress, resumes, and dashboards stay authoritative (server-side guardrails landed; UI/console + telemetry still pending).
- [x] Author persona context hydration + automatic memory append validated end-to-end (Immediate Priority #3 / Milestone H).
- [ ] Console/API surfacing for tracked characters + lore requirements with branch lineage (Immediate Priority #4 / Milestone D).
- [ ] Hangfire + front-end backlog metadata contract signed off, telemetry hooks live, and Scenario #1 rehearsal recorded.
- [ ] CharacterLifecycleService + lore requirement migrations landed with telemetry (`FictionCharacterPromoted`, `FictionLoreRequirementBlocked`) and Scenarios #2/#4 ready to run.
- [ ] Acceptance scenarios 1-4 rehearsed with artifacts (videos/logs) attached to the Phase 001 completion report.
- [ ] ruthlessly drop/defer tasks that do not unblock personas/lore/planners/writer UX (no more 'migrate empty table' work).
