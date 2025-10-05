# Fiction Iteration Phase 001

## Objective
- Port and expand the `iterate-book` orchestration into Cognition so fiction projects can plan, branch, and write inside the shared toolchain without losing existing capability.
- Stand up a durable, multi-tier chain-of-thought graph (vision -> blueprint -> scroll -> woven prose) that supports branching, replay, and audit across assistants.
- Move world knowledge, checkpoints, metrics, and transcripts from ad hoc files into first-class relational entities with tooling hooks.

## Current State Insights
- **Python Weaver** (`reference/iterate-book.py`) already expresses the end-to-end flow: checkpointed phases, blueprint refinement, recursive scene writing, and world grimoire updates. Strengths include schema validation, attentional gates, retry policies, and context caching, but persistence is file based and concurrency unsafe.
- **Cognition Fiction Tools** (Outliner, SceneDraft, LoreKeeper, Worldbuilder, FactChecker) are data-first; they depend on `CognitionDbContext`, capture annotations/metrics, and expect minified JSON responses from LLMs. They currently operate independently and do not share a unified planning graph or checkpoints.
- **Data Layer** provides outline nodes, draft segments, canon rules, glossary, timelines, and world assets. Missing are plan tiers, iterative passes, scroll structures, knowledge updates tied to writing, transcript storage, and a managed prompt catalog (the Prompts module is empty).
- **Agent platform** has shipped agent-centric IDs and tooling, but residual persona fallbacks still exist outside Image Lab and retrieval policies need guardrails to prevent cross-agent bleed.
- **Generated manuscripts** (e.g., reference/grimoire-of-the-fragmented.md) retain redundant draft passes and duplicated scene transitions; entire beats repeat because prompt orchestration never selects a canonical take nor enforces cleanup.
- **Legacy fiction data/tooling** (OutlineNode, Draft, PlotOutline, WorldAsset, LoreKeeper, etc.) remain prototypes; we either polish them against the new plan graph or retire them once replacements ship.

- **Prompts module** within `Cognition.Data.Relational.Modules.Prompts` is empty; we need it to host versioned prompt templates and plays once templating lands.

## Success Criteria
- All five Python phases (vision, world grimoires, iterative passes, blueprint, refine, write) run inside Cognition with parity and ergonomic branching.
- Book projects persist CoT nodes, locks, metrics, and transcripts transactionally so assistants can resume reliably.
- Fiction tools consume and update the shared plan graph (e.g., SceneDraft uses scroll metadata, LoreKeeper writes directly into world bible tables).

## Milestones & Tracks

### Milestone 0 - Snapshot & Guardrails
1. Take a fresh arc snapshot + git tag of the current state; record feature flags and environment knobs.
2. Default fiction weaver feature flags to off; design rollout plan for staged enablement.
3. Catalog residual persona-based flows and schedule their removal or shims before fiction runs.
4. Define operational guardrails (retry budgets, max token spend) and document escalation contacts.

### Milestone A - Baseline Capture and Invariants
_Status: Complete â€” see step note `phase-001_step_20250926_2327_inventory.md` for follow-ups._
1. Inventory Python prompts, schema validators, retry rules, attentional gates, and file outputs (`_context`, `story.json`, markdown scenes).
2. Document current metrics (metrics.csv), checkpoints (iterate_state.json), and lock semantics; flag gaps vs. transactional needs.
3. Produce sample project archive (arc snapshot + Git tag) for regression and migration dry runs.
4. Identify reusable prompt scaffolds (focus Phase-001 on OpenAI + Gemini); log Ollama-only spicy rewrites as deferred, narrow-scope plays.

### Milestone B - Data Model & Persistence Evolution
_Current status: EF POCOs drafted for plan graph/world bible tables; configuration scaffolding under review._
1. Design ERD for multi-tier planning: `FictionPlan`, `FictionPlanPass`, `FictionChapterBlueprint`, `FictionChapterScroll`, `FictionChapterSection`, `FictionChapterScene`, `FictionPlanCheckpoint`, `FictionPlanTranscript`, `FictionStoryMetric`.
2. Define world bible expansion tables (`FictionWorldBible`, `FictionWorldBibleEntry`) mapped to existing glossary/canon concepts; plan migration paths.
3. Draft EF Core migrations, indexes, and constraints (slug uniqueness, lineage references, branch versioning metadata). _(ðŸš§ `20250928173755_AddFictionPlanGraph` covers core tables; checkpoint resume/cancel migration still pending rollout.)_
4. Specify storage for LLM call transcripts and schema validation errors (for audit and replay). (All agent/tool LLM calls will be treated as conversations and traced we will need audit/xref metadata table to link the agent conversations to the phase/step data of the plan/process.)
5. Author importer spec for `_context` artifacts -> relational tables (idempotent, resumable, produces discrepancy logs). (Nice-to-have; parity fallback, lowest priority.)
6. Generate seed data for a sample project and validate referential integrity + branching metadata.
7. Reconcile existing fiction tables (PlotOutline, Draft, WorldAsset, etc.) and tool scaffolding with the plan graph; capture keep/polish vs retire decisions in the legacy action list.

### Milestone C - Weaver Orchestration Service (Hangfire + Agent conversations)
1. Host phase runners as Hangfire jobs; coordinate progress/events via bus and AgentService conversations (initial wrappers exist in `Cognition.Jobs/FictionWeaverJobs` for checkpoint-aware execution, and `IFictionWeaverJobClient` now passes provider/model metadata so jobs resolve LLM bindings deterministically).
2. Model per-phase runners mirroring Python functions (`VisionPlanner`, `WorldBibleManager`, `IterativePlanner`, `ChapterArchitect`, `ScrollRefiner`, `SceneWeaver`).
3. Implement checkpoint/lock manager backed by `FictionPlanCheckpoint` (phase status, progress counters, timestamps, resume metadata). Initial Hangfire wrapper (`FictionWeaverJobs`) now drives `Cancelled` state + branch resume/cancel hooks, but an EF migration is still required.
4. Port schema validation, slug sanitization, attentional gates, and retry backoff helpers to shared libraries.
5. Build transcript logger capturing system/user prompts, responses, validation diagnostics, metrics, and AgentService conversation IDs. FictionWeaverJobs now stores `FictionPlanTranscript` rows, publishes `FictionPhaseProgressed` events, and all phase runners call AgentService to capture real prompts + replies (token accounting still pending).
6. Provide branching API (clone/fork tiers, compare/merge alternates, mark active branch) callable from jobs and UI.

### Milestone D - Tool Alignment & Workflow Glue
1. Update OutlinerTool to read/write `FictionChapterBlueprint` beats and expose branch selection.
2. Update SceneDraftTool to pull section/scene metadata from `FictionChapterScroll` and report metrics back to `FictionStoryMetric`.
3. Extend LoreKeeperTool and WorldbuilderTool to operate on `FictionWorldBible` entries with lineage to chapters/sections.
4. Introduce FactChecker/NPC pipelines that consume plan nodes, annotate results, and feed timeline/canon updates.
5. Provide administrative commands (start phase, resume, branch, lock override) exposed via tool interfaces or API endpoints.

### Milestone E - Knowledge, Reflection, and Branching Intelligence
1. Port chapter memory summaries into relational storage; expose helpers for fetching recent memories (with windowing instead of truncated arrays).
2. Implement world grimoire update loops using structured diffing so we track additions/changes per chapter.
3. Encode branching heuristics (e.g., create alternate blueprint when attentional gate fails or metrics regress) and enable manual branch creation.
4. Plan for timeline synchronization: map scene metadata to `TimelineEvent` entries with bidirectional references.
5. Add a mandatory focused rewriting/consolidation phase that consumes the refined scroll, de-duplicates repeated beats, and emits the post-processed manuscript.

### Milestone F - Retrieval Guardrails & Observability
1. Enforce ScopeToken usage across phase runners and tools; ensure all retrieval queries filter by `AgentId` + `ConversationId` (and project/world identifiers when present).
2. Provide a policy toggle to disallow Agent-level fallback for isolation-critical conversations; document defaults per environment.
3. Remove remaining persona-based adapters outside Image Lab and update events/payloads to be Agent-first.
4. Build metrics dashboards for phase durations, retries, schema failures, embedding writes/searches, and token consumption.
5. Create integration tests using deterministic LLM stubs to exercise each phase end-to-end.
6. Add contract tests for tool I/O schemas plus isolation tests verifying no cross-conversation bleed without promotion.
7. Document operational runbooks (feature flags, rollback, recovery) and share with assistants + humans.

### Milestone G - Prompt Playbook & Templating
### Legacy Fiction Integration Action List
- Audit existing relational POCOs (`PlotOutline`, `Draft`, `WorldAsset`, `Timeline`, `StyleGuide`) for overlap with new plan entities; mark keep/polish vs retire.
- Map current tools (Outliner, SceneDraft, LoreKeeper, Worldbuilder, FactChecker, Rewriter) to plan graph responsibilities and note required refactors.
- Decide whether to repurpose `PlotOutline` as long-form meta attached to `FictionPlan` or archive it once blueprints/scrolls ship.
- Ensure the Prompts module hosts shared templates for all retained tools, versioned with play identifiers.
- Record outcomes in milestone step notes and feed follow-up tasks into the backlog.

1. Audit Python and tool prompts; extract them into PromptTemplate records (versioned, tagged, provider-aware).
2. Design "Prompt Play" schema (e.g., Play, PlayStep, PlayBinding) linking templates to orchestrator phases and agent roles.
3. Implement templating engine adapters (start with Razor/RazorLight; allow fallback string interpolation) with token resolution, partials, and environment overrides.
4. Encode scene-transition discipline inside plays (explicit guidance on recap vs. advance, track last-output hashes) to prevent duplicated openings/endings.
5. Build prompt simulation and preview tooling (render with sample tokens, diff against legacy prompts, capture token counts).
6. Wire plays into Weaver phase runners and Fiction tools so each agent call selects a play + template and logs usage.
7. Establish prompt governance workflows (status, approvals, experiments) and telemetry on prompt effectiveness.

## Deliverables
- Post-processing pipeline that collapses duplicate scenes and enforces scene-transition QA.
- Updated docs/runbooks covering agent-centric APIs, weaver operations, and recovery procedures.
- Detailed architecture spec and sequence diagrams for the Fiction Weaver service and branching model.
- EF Core migrations + seed/import scripts covering plan tiers, world bible, metrics, transcripts.
- Shared C# libraries for schema validation, attentional gates, slug/prompt utilities.
- Updated Fiction tools aligned with the shared graph and metrics reporting.
- (Optional) Importer utility for legacy `_context` projects with reconciliation report (nice-to-have; only needed if additional Python-era books surface).
- Prompt playbook catalog with templated prompts, plays, and preview tooling.
- Regression test suite and telemetry dashboards.

## Migration & Rollout Strategy
1. Run importer against archived sample projects, reconcile discrepancies, iterate until clean.
2. Deploy Weaver service behind feature flag; run parallel with Python pipeline using clone projects.
3. Validate parity (plan structures, world bible content, prose output) and capture anomalies.
4. Incrementally onboard active projects, providing rollback via snapshot + Git tag.
5. Decommission Python pipeline after parity sign-off; archive legacy artifacts.

## Risks & Mitigations
- **LLM schema drift**: enforce strict validators, maintain canned fixtures, add continuous prompt tests.
- **Data migration gaps**: dry runs with checksum reports, maintain reversible scripts, keep `_context` copies until acceptance.
- **Concurrency/race conditions**: design checkpoint manager with row-level locking and optimistic concurrency tokens.
- **Cost/performance regressions**: cache persona/style summaries, reuse system prompts, monitor token usage.
- **Tool contract breakage**: introduce interface-level tests and staged rollout with feature flags.

## Worklog Protocol
- Step notes live at `plans/fiction/phase-001_step_YYYYMMDD_HHMM_<slug>.md` and capture goal, context, commands, files touched, tests, outcome, next actions (use `O` for open, `X` for done).
- Record ad hoc thoughts in `plans/_scratch/` with timestamp headers; promote decisions into step notes before closing.
- Each milestone completion should reference supporting commits, migrations, and validation outputs.

## Checklist
- [ ] Retrieval policies enforce ScopeToken + Agent isolation with environment toggles recorded.
- [ ] Agent-centric APIs free of persona fallbacks (except intentional legacy cases) with documented runbooks.
- [ ] Rewriting/consolidation phase eliminates duplicate beats in generated manuscripts.
- [ ] Address ASP0023 route conflicts in `ClientProfilesController` (run API analyzers and align route templates).
- [ ] Resolve CS8619/CS8620 nullability warnings in RetrievalService/KnowledgeIndexingService (decide harden vs suppression).
- [ ] Baseline Python prompts, schemas, checkpoints, metrics, and sample artifacts documented.
- [ ] ERD + migrations for multi-tier plan, world bible, checkpoints, transcripts authored.
- [ ] Weaver service scaffolding with phase runners, checkpoints, and transcript logging implemented.
- [ ] Branching model (clone/fork/merge) operational with lineage tracking.
- [ ] (Optional) Legacy `_context` importer produces reconciliation report (nice-to-have; very low priority).
- [ ] Prompt catalog populated, templating engine in place, and plays wired into orchestration.
- [ ] Fiction tools updated to consume shared plan graph and emit telemetry.
- [ ] Regression harness + observability dashboards live, with parity sign-off for first migrated project.





















