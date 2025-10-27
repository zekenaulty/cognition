# Hot Targeted TODO

## Cross-Plan Backbone
- **Order of execution:** finish scope principal/path refactor -> stand up planner framework skeleton -> pilot migration -> expand unit tests guarding the new surfaces. This dependency chain lives across the scope token, planner, and unit testing plans and remains the critical path (plans/scope_token_path_refactor.md, plans/planning_the_planner.md, plans/unit_testing_expansion.md, plans/scope_testing_planner_notes.md).

## Active Blockers
- WILL NOT DO **Dual-write rollout** - migrations/backfill tooling will not be pursued; record kept for historical context (plans/scope_token_path_refactor.md, plans/scope_testing_planner_notes.md).
- **Planner base contracts** - `IPlannerTool`, `PlannerBase`, telemetry contracts, and template repository hooks are still design-only; fiction tooling cannot migrate until they exist (plans/planning_the_planner.md, plans/scope_testing_planner_notes.md).
- **Template & telemetry infrastructure** - shared template repo + planner telemetry events need implementation ahead of pilots to avoid rework (plans/planning_the_planner.md, plans/scope_testing_planner_notes.md).

## Priority Action Stack
1. DONE **Define canonical schema + hashing flag** - landed canonical scope primitives and feature flag support (scope_token_path_refactor_step_20251009_01).
2. DONE **Dual-write persistence + diagnostics** - scope metadata now dual-written with diagnostics/backfill tooling (scope_token_path_refactor_step_20251009_02).
3. DONE **Guarding tests for scope & retrieval** - regression tests cover canonicalisation, fallback, and dispatcher propagation (unit_testing_expansion_step_20251009_03).
4. WILL NOT DO **Apply dual-write schema + reindex vectors** - deferred permanently; leave doc references for posterity (plans/scope_token_path_refactor.md, plans/scope_testing_planner_notes.md).
5. DONE **Planner contracts + transcript logger** - landed `IPlannerTool`, `PlannerBase<TParams>`, `PlannerContext`, logging hooks; Vision planner now routes through the base (planning_the_planner_step_20251012_04).
6. DONE **ToolRegistry capability index + PlanAsync helper** - catalog + dispatcher telemetry wired for planner discovery (planning_the_planner_step_20251012_05).
7. DONE **Vectors/OpenSearch regression tests** - scoped KNN filters asserted, Query DSL promoted to path metadata (unit_testing_expansion_step_20251012_07).
8. WILL NOT DO **Flip path hashing flag (lower env)** - no further action; hash flip and associated soak/backfill steps are abandoned (scope_token_path_refactor_step_20251009_08).
9. DONE **ScopePath retrieval/tooling adoption** - retrieval helpers, ToolDispatcher, and fiction runners now consume the shared builders; regression tests cover planner + dispatcher propagation (plans/scope_token_path_refactor.md, plans/scope_testing_planner_notes.md, plans/unit_testing_expansion.md).
10. DONE **Planner backlog plumbing** - dispatcher + fiction jobs propagate/consume `backlogItemId`, non-vision runners echo it into results/telemetry, and regression coverage exercises chapter/scroll/scene flows; next follow-up shifts to backlog telemetry surfacing (plans/planning_the_planner.md, plans/fiction/phase-001_step_20250926_2327_inventory.md).
11. **Planner migrations & seeding rollout** - push `20251013181358_PlannerExecutions` + `AddFictionPlanBacklog` through lower environments, run the seeder, and capture rollout evidence in a fresh step note (planning_the_planner_step_20251012_04).
12. DONE **Planner health & critique guard rails** - shipped `/api/diagnostics/planner`, structured `planner.*` telemetry, and persona-aware critique budgets defaulting to off per planner/persona (plans/planning_the_planner.md, plans/scope_testing_planner_notes.md).
13. DONE **Planner telemetry + diagnostics surfaces** - console route `Operations → Backlog Telemetry` now renders planner health + OpenSearch diagnostics with backlog coverage, stale/orphaned alerts, critique budget warnings, and flapping detection powered by the new endpoints (plans/planning_the_planner.md, plans/scope_testing_planner_notes.md, plans/planning_the_planner_step_20251026_06_backlog_dashboards_and_chapter_architect_prep.md).
14. DONE **Planner pipeline audit/fixes** - harness now scripts the full vision -> scene flow via `FictionPlannerPipelineTests`, asserting backlog transitions, transcripts, and checkpoints (plans/planning_the_planner.md, plans/fiction/phase-001_step_20250926_2327_inventory.md).
15. DONE **Planner pilot migration** - Chapter Architect now rides `PlannerBase`, scripted parity is covered by `FictionPlannerPipelineTests`, and backlog/telemetry alerts feed the Ops webhook so flapping/stale items page automatically (planning_the_planner_step_20251026_07_chapter_architect_planner_and_ops_alerting.md, plans/scope_testing_planner_notes.md).
16. **Planner rollout guidance & README refresh** - publish the developer recipe (prompts, backlog usage, telemetry expectations) and refresh README once the above foundations land (planning_the_planner_step_20251009_09, shared checklist items).

## Fast Follow Improvements
- Centralised `ScopePath` builders shared across services to ban ad-hoc path construction (plans/scope_testing_planner_notes.md, plans/scope_token_path_refactor.md).
- Planner telemetry contract + exposure (planner.* events, transcript repository surfacing) before wider adoption (plans/planning_the_planner.md, plans/scope_testing_planner_notes.md).
- Template repository for planner prompts keyed by `StepDescriptor` ids (plans/planning_the_planner.md, plans/scope_testing_planner_notes.md).
- Health/diagnostics endpoint exposing scope feature flags + metrics (plans/scope_testing_planner_notes.md, plans/scope_token_path_refactor.md).
- Ensure deterministic fakes (ScriptedLLM, ScriptedEmbeddingsClient, InMemoryVectorStore) are default in tests to unblock planner pilots (plans/unit_testing_expansion.md, plans/scope_testing_planner_notes.md).
- Backlog telemetry & dashboards for fiction runners (Vision â†’ Iterative â†’ Architect â†’ Scroll â†’ Scene) now power the console view and Ops webhook; next layers focus on additional planner migrations and downstream consumers (plans/fiction/phase-001_step_20250926_2327_inventory.md, plans/planning_the_planner.md).

## Watch Items / Risks
- Token cost blow-ups if self-critique defaults stay on; make it opt-in per planner/persona (plans/planning_the_planner.md, plans/scope_testing_planner_notes.md).
- Performance hit from new principal/path indexes; benchmark before production flag flip (plans/scope_token_path_refactor.md, plans/scope_testing_planner_notes.md).
- Persona-only assets (Image Lab) remain intentionally persona-scoped; document exception to avoid accidental refactors (plans/scope_testing_planner_notes.md, plans/scope_token_path_refactor.md).
- Enforce step-note + snapshot workflow via PR template to prevent drift (plans/README.md referenced in plans/scope_testing_planner_notes.md).



