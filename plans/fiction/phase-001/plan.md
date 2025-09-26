# Fiction Iteration Phase 001

Objective
- Migrate and enrich the `iterate-book` workflow into Cognition's Fiction toolchain without losing capabilities.
- Establish a multi-tier chain-of-thought (CoT) planning graph that can fork or branch across phases (vision -> blueprint -> scroll -> prose).
- Prepare the data and service layers so iterative planning and world knowledge updates persist in the relational store instead of ad hoc files.

Constraints
- Preserve current phase sequencing (vision, world grimoires, planning passes, blueprint, refine, write) while making them modular.
- Keep scripts and prompts portable across providers already supported by Cognition (OpenAI, Gemini, Ollama).
- Support resuming work safely (checkpointing and locking) even when multiple assistants participate.

Scope
- In-scope: translating Python orchestration concepts into C# services and tools, defining database entities and migrations, authoring planner and writer prompt templates, observability and telemetry, migration of existing file outputs where feasible, interface glue for existing Fiction tools.
- Out-of-scope: UI surface polish, final editorial review workflows, long-term model governance, deployment automation (covered in later phases).

Deliverables
- Reference architecture doc for a Fiction Weaver service (phase orchestrator plus CoT planner).
- Database migration(s) introducing plan and chapter blueprint storage, world grimoire tables, metrics, and transcript retention.
- C# orchestration layer (background service or agent pipeline) mirroring Python phases with injectable strategies.
- Updated Fiction tools (Outliner, SceneDraft, Worldbuilder, LoreKeeper, etc.) wired to the shared planning data and CoT graph.
- Migration utilities to import existing `_context` JSON/markdown outputs into the new schema.

Data / API / Service Changes
- New relational entities: `FictionPlan` (book-level vision), `FictionPlanPass` (iterative refinements), `FictionChapterBlueprint`, `FictionChapterScroll` (refined spec), `FictionChapterSection`, `FictionChapterScene`, `FictionWorldBible` and `WorldBibleEntry`, `FictionStoryMetric`, `FictionPlanCheckpoint`, `FictionPlanTranscript` (LLM calls and prompts), linkage to existing `DraftSegment` and `OutlineNode`.
- Service layer: introduce an `IFictionWeaver` domain service with sub-services (`VisionPlanner`, `WorldBibleManager`, `ChapterArchitect`, `ScrollRefiner`, `SceneWeaver`, `MetricsRecorder`).
- Tool alignment: extend current tools to read and write from the shared CoT graph (for example, SceneDraftTool consumes `FictionChapterScene` metadata; LoreKeeper writes back structured knowledge entries).
- API surface: expose orchestrator endpoints or commands (start phase, resume, inspect plan nodes, branch or fork) and admin endpoints for lock management.

Multi-Tier Planning Model
- Tier 0 (Vision Layer): book premise, goals, personas, thematic directives.
- Tier 1 (Blueprint Layer): chapter-level arcs with beat metadata.
- Tier 2 (Scroll Layer): section and scene breakdown with dynamics, carry-forward notes.
- Tier 3 (Weaving Layer): prose generations, reflections, post-write knowledge updates.
- Forking and Branching: support versioned nodes per tier (for example, alternate chapter blueprints or experimental scene rewrites) with lineage references and merge policies.

Implementation Tracks
1. Baseline Capture and Gap Analysis - extract prompts, schemas, lock rules, metrics from Python and document invariants.
2. Data Modeling and Migration - design ERD, author migrations, seed reference data, import existing outputs.
3. Service Orchestration - build Weaver service, implement phase runners with checkpoint and retry semantics.
4. Tool Integration - update Fiction tools to consume the CoT graph, surface metrics or annotations, enable branching operations.
5. Observability and QA - metrics logging, transcript storage, automated validation (schema and attentional gates), regression harness.

Migration Strategy and Rollout
- Snapshot existing book projects via `arc.py` plus Git tag (document in snapshot step note).
- Provide importer for `_context` directory into new relational tables (idempotent, logs mismatches).
- Gradual cutover: run Weaver service in parallel with current scripts; validate parity using sample projects.
- Finalize by locking Python pipeline, switching automation to new service and tools, archiving legacy outputs.

Testing and Verification
- Unit coverage for schema validators, slug builders, attentional gate logic.
- Integration tests for each phase using deterministic LLM stubs.
- Smoke runs with real models for representative projects; diff outputs versus Python baselines.
- Data migration verification: row counts, key constraints, sample content checks.

Risk and Rollback
- Risk: LLM schema drift. Mitigate with stricter validators, prompt tests, fallback stubs.
- Risk: Migration gaps causing data loss. Mitigate with dry runs and reversible scripts.
- Risk: Tool coupling regressions. Mitigate with contract tests against `ITool` interfaces.
- Rollback: restore from Git tag and arc snapshot, disable Weaver service, re-enable Python routine.

Worklog Protocol
- Store step notes as `plans/fiction/phase-001_step_YYYYMMDD_HHMM_<slug>.md`.
- Each note records goal, context, commands, files touched, test evidence, completion symbol (use `O` for pending and `X` for done).
- Keep scratch thoughts in `plans/_scratch/` with timestamps; promote decisions into step notes before closing.

Checklist
- [ ] Produce baseline spec of current Python prompts, schemas, and file artifacts.
- [ ] Draft ERD and migration plan for multi-tier planning graph.
- [ ] Define service interfaces and phase runners in Cognition.Clients.
- [ ] Prototype CoT branching flow on sample project (parallel blueprints).
- [ ] Validate data importer on legacy `_context` folder.
- [ ] Align Fiction tools with new data contracts (Outliner, SceneDraft, LoreKeeper, FactChecker).
- [ ] Establish regression harness and telemetry dashboards.
