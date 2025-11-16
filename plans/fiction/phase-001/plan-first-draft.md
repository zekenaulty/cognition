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
- FictionWeaver jobs enforce backlog metadata contracts (conversationPlanId, provider/model IDs, backlog task IDs) and fail early if clients omit them, keeping Hangfire + UI in sync. The API exposes backlog listings + resume endpoints so console resumes can hydrate `ConversationTask` metadata and trigger `FictionBacklogScheduler` without manual DB edits.
- Console UX now surfaces branch-aware rosters, blocked-vs-ready lore dashboards, and author persona memories/world notes, with lore fulfillment actions invoking the new API in real time.
- Remaining gaps sit in tooling (console still needs backlog/action cards + alerts), world-bible provenance wiring, and ~~surfacing of tracked characters/lore inside author dashboards~~ **live roster/telemetry visibility shipped via new API + console views** (branch-aware rosters, lore fulfillment dashboards, author persona memory panes).

## Success Criteria
- Vision/iterative planners emit detailed `characters[]` and `lore[]` payloads, flag importance, and automatically spin up personas/agents/memories when needed.
- Every fiction phase records its context (prompt template, instruction set, scope path) plus branch lineage so branches can resume/fork safely.
- Scroll/scene writers refuse to run unless required lore + character assets exist (or they create them via the lifecycle service).
- Author personas always speak with their stored memories; new prose automatically appends memories, world bible diffs, and canon obligations.
- Consoles/agents can inspect characters, lore, checkpoints, branches, and transcripts via API.

## Immediate User-Facing Priorities (Nov 2025)
1. **Backlog-driven orchestration + resume UX:** Guardrails now reject missing `conversationPlanId`/provider/model/task IDs, the fiction API exposes backlog listings/resume endpoints, and resuming a backlog item rewrites the `ConversationTask` metadata before handing it back to `FictionBacklogScheduler`.
   - _User impact:_ Authors see phase cards progress exactly in the order they kicked off, resumes land in the right scene, transcripts stop drifting from backlog truth, and admins can restart blocked backlog items without DB spelunking.
   - _Work to land:_ Wire console resume buttons to the new API, add backlog widgets/alerts to telemetry pages, migrate legacy backlog rows to carry branch + provider/model metadata, and publish drift counters to Ops dashboards.
   - _Proof/telemetry:_ Dashboards comparing backlog status vs `FictionPhaseProgressed` events, resume-success counters, backlog-action audit feed, and `fiction.backlog.telemetry` workflow events highlighting API usage.
2. **Character & lore lifecycle enforcement:** Structured vision output, lifecycle service hooks, scroll/scene gating, and branch-aware rosters/dashboards are live; next is closing the fulfillment loop so console users can mark requirements Ready (with provenance) and so world-bible flows auto-generate missing entries (`character_persona_lifecycle.md`).
   - _User impact:_ Character sheets, lore pillars, and drafting prerequisites remain visible before prose starts, while lore fulfillment no longer requires raw SQL.
   - _Work to land:_ Automate world-bible fulfillment (tool prompts + API), add audit history for each requirement, surface fulfillment state in Hangfire/workflow telemetry, and keep deterministic tests proving branch-lineage propagation + roster math.
   - _Proof/telemetry:_ Console roster + lore summary staying in sync with Hangfire attempts, lifecycle logs per character, and schema tests covering fulfillments/resumptions.
3. **Author persona context hydration:** Author persona registry + prompt hooks are live, persona memories append after each pass, and console pages show the newest memories/world notes. Remaining work focuses on obligations workflow + personas without memory debt.
   - _User impact:_ Writers experience a stable tone/voice per author persona; edits carry previous obligations and world-bible reminders without manual copy/paste; admins can inspect memory growth in the console.
   - _Work to land:_ Add obligation tagging/triage UI, expose persona-memory diff feeds via API, and attach authorship metadata to backlog runs so Ops can see who triggered each pass.
   - _Proof/telemetry:_ Persona-memory diff logs per SceneWeaver run, regression tests swapping personas, UX validation clips showing tone consistency, and console analytics on the new author persona panes.
4. **Console/API surfacing for canon assets:** `/api/fiction/plans/{id}/roster`, `/lore/summary`, `/lore/{id}/fulfill`, `/backlog`, and `/author-persona` endpoints plus console dashboards are live (branch-aware characters, blocked-lore groupings, author persona memories). Next is wiring backlog resume/fill actions + alerting hooks so admins can drive the entire lifecycle from the console.
   - _User impact:_ Authors can inspect who is tracked, whether lore is missing, which branch each asset belongs to, and restart blocked backlog tasks without digging through raw transcripts.
   - _Work to land:_ Backlog cards in the console, persona/lore change history panels, alert banners for blocked fulfillment, and guardrails that prompt users when they attempt to resume without providing provider/model/task metadata.
   - _Proof/telemetry:_ Usage metrics on the new panels, backlog-resume success counters, console feedback citing the workflow, and `fiction.backlog.telemetry` events linked back to UI actions.

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
  - [x] Console/API surfacing for tracked characters + lore requirements with branch lineage (Immediate Priority #4 / Milestone D). _Fiction plan roster endpoint + console projects page are live._
- [ ] Hangfire + front-end backlog metadata contract signed off, telemetry hooks live, and Scenario #1 rehearsal recorded.
- [ ] CharacterLifecycleService + lore requirement migrations landed with telemetry (`FictionCharacterPromoted`, `FictionLoreRequirementBlocked`) and Scenarios #2/#4 ready to run.
- [ ] Acceptance scenarios 1-4 rehearsed with artifacts (videos/logs) attached to the Phase 001 completion report.
- [ ] ruthlessly drop/defer tasks that do not unblock personas/lore/planners/writer UX (no more 'migrate empty table' work).
