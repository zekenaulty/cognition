Cohesion checks (what lines up / what’s missing)

Planner framework depends on scope-path work
Your “Planning the Planner Framework” explicitly calls out finishing scope-path first, then skeleton + pilots; the scope-path plan’s checklist is still open. So the global order of operations is: Scope Path → PlannerBase → Pilot migration → Unit tests expansion. Lock that in as the backbone. 

Plans workflow is sound; keep using step notes + snapshot
The Plans README establishes a consistent protocol (snapshot with arc.py, git tag, dated step notes). Use it to gate every phase flip and keep human-auditable diffs. 

Unit testing plan overlaps nicely with both refactors
Unit Testing Expansion already targets retrieval scope, vector DSL guards, dispatcher scope propagation, JWT/infra, and job flows—exactly what the scope-path and planner base will stress. Keep those “Phase 1–3” items prioritized to prevent regressions during cutover. 

Edge cases to harden (before coding)

Scope identity & hashing

Collisions/bleed when switching to path-aware hashing; verify legacy vs path hashes don’t diverge mid-window. Add regression tests around canonicalization and prefix/ancestor queries. 

Dual-write windows

Partial backfills leaving mixed legacy/path scopes; ensure readers always choose the correct branch and don’t broaden the filter. Add soak metrics. 

Conversation-first → agent fallback

Empty or deleted conversation IDs must not leak sibling conversation content. You already planned tests; prioritize them. 

Planner retries & self-critique

Infinite/expensive iteration loops; enforce max-iteration guards and make critique configurable per planner/persona to control token spend. 

ToolDispatcher scope propagation

Every tool call from planner steps must preserve the exact ScopePath; add a failing test first, then fix. 

OpenSearch vector invariants

Dimension guards when pipelines are disabled; validate KNN + scoped filters with/without path fields present. 

Legacy persona touchpoints

Image Lab remains persona-scoped; document that as intentionally out of scope during planner + path rollout to avoid accidental “cleanup” breaks. (Cross-ref the persona→agent notes under completed/) 

Improvements to make now (small changes, big payoff)

Pin a single source of truth for ScopePath construction
Expose ScopePath builders in Clients and use them everywhere (Jobs, API, Tools). Ban ad-hoc object creation in code review. 

Planner telemetry contract
Standardize planner.* events and store transcripts separately; keep step hierarchy for the UI later, but the contract should be locked now. 

Prompt/template lookup
Move step prompts into a TemplateRepo and reference by StepDescriptor IDs; you already call for metadata-driven behaviour—make it concrete before migrations so pilots don’t code prompts inline. 

Fail-fast feature flags
Two flags: ScopePathDualWrite and PathAwareHashing. Add a Health/Diagnostics endpoint to echo current flag state and recent collision metrics. 

Deterministic fakes first
Finish ScriptedLLM, ScriptedEmbeddingsClient, InMemoryVectorStore adoption across tests so planner pilots don’t need live calls. 

Cascading changes (integration & refactor steps you’ll need)
A) Scope Token Path Refactor (Phase ordering + integration)

Model & hashing behind flags
Implement ScopeSegment/ScopePath, canonical renderer, and path-aware hashing (flagged). Add EF/Vector columns with dual write. 

Query surfaces
Ship LINQ helpers: ForAgent, UnderPath, WalkUp. Update RetrievalService & QueryDslBuilder to require a ScopePath (compile-time). 

Backfill + monitors
Opportunistic write-through backfill, then flip hashing flag. Track collision/bleed metrics for a soak period. 

B) Planner Framework (binds to ScopePath)

Contracts + base
Land IPlannerTool, PlannerBase<TParams>, PlannerContext (with ScopePath, retrieval delegates), PlannerResult + transcript logger. 

Registry/Dispatcher
Capability index (“planning”) in ToolRegistry; ToolDispatcher overload for PlanAsync<TPlanner> ensuring transcript/metrics hooks. 

Pilot migration
Wrap the smallest fiction planner via adapters → derive from PlannerBase, map parameters, points at template repo, and verify parity with ScriptedLLM. 

C) Unit Testing Expansion (protects both refactors)

Scope & retrieval
Conversation-first/fallback tests, path canonicalization, dispatcher scope propagation. (These are already listed as high-value tests—promote to “blocking.”) 

Vectors / OpenSearch
Scoped KNN filters, UpsertAsync dimension guards, and index routing. 

Jobs & API smoke
FictionWeaverJobs transcript events (deterministic), Chat controller agent-centric DTO stamping. 

D) Frontend guardrails (minimal, but important)

Keep persona-scoped Image Lab as is; document the exception in the plan and in code comments to prevent “helpful” refactors. 

Actionable checklist (create these step notes before coding)

Create step notes per Plans README pattern; below are the exact titles to add under plans/:

scope_token_path_refactor_step_20251009_01_canonical_schema_and_hash_flag.md
Goal: Define canonical segment schema; land ScopePath structs; add hashing flag; unit tests for normalization/hash. Blocks: PlannerBase. 

scope_token_path_refactor_step_20251009_02_dual_write_persistence_and_backfill_runner.md
Goal: EF + vector dual fields; opportunistic backfill; add metrics + diagnostics endpoint. 

unit_testing_expansion_step_20251009_03_scope_retrieval_and_dispatcher_guards.md
Goal: Tests for conversation→agent fallback, ScopePath canonicalization, ToolDispatcher scope propagation. Make blocking. 

planning_the_planner_step_20251009_04_contracts_and_transcript_logger.md
Goal: Ship IPlannerTool, PlannerBase, PlannerContext, transcript/metrics hooks under feature flag; no UI. 

planning_the_planner_step_20251009_05_toolregistry_capability_index_and_plan_async.md
Goal: Capability index + DI helper; integrate with existing ToolDispatcher. 

planning_the_planner_step_20251009_06_pilot_migration_scripted_scene_planner.md
Goal: Adapter → derived class; templates referenced by StepDescriptor ID; parity via ScriptedLLM. 

unit_testing_expansion_step_20251009_07_vectors_ql_dsl_and_upsert_guards.md
Goal: KNN scoped filters + UpsertAsync dimension guard tests; fake client. 

scope_token_path_refactor_step_20251009_08_flip_path_hashing_flag_and_monitor.md
Goal: Enable path hashing in lower env; monitor collisions/bleed; rollback toggle documented. 

planning_the_planner_step_20251009_09_rollout_guidance_and_dev_recipe.md
Goal: Write the short “dev recipe” for planners (how to implement a new planner using templates, critique, retries). 

Final quick risks & mitigations

Token cost blowups in planner evaluation → keep self-critique optional + per-planner budgets. 

Perf regressions on new path columns → index strategy + benchmarks before flag flip. 

Developer drift from the plan protocol → enforce snapshot + step notes at PR template level per Plans README.