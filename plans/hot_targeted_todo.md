# Hot Targeted TODO

## Cross-Plan Backbone
- **Order of execution:** finish scope principal/path refactor -> stand up planner framework skeleton -> pilot migration -> expand unit tests guarding the new surfaces. This dependency chain lives across the scope token, planner, and unit testing plans and remains the critical path (plans/scope_token_path_refactor.md, plans/planning_the_planner.md, plans/unit_testing_expansion.md, plans/scope_testing_planner_notes.md).

## Active Blockers
- **Dual-write rollout** - migrations/backfill tooling are ready, but we still need to apply them in lower environments before we can flip hashing (plans/scope_token_path_refactor.md, plans/scope_testing_planner_notes.md).
- **Planner base contracts** - `IPlannerTool`, `PlannerBase`, telemetry contracts, and template repository hooks are still design-only; fiction tooling cannot migrate until they exist (plans/planning_the_planner.md, plans/scope_testing_planner_notes.md).
- **Template & telemetry infrastructure** - shared template repo + planner telemetry events need implementation ahead of pilots to avoid rework (plans/planning_the_planner.md, plans/scope_testing_planner_notes.md).

## Priority Action Stack
1. DONE **Define canonical schema + hashing flag** - landed canonical scope primitives and feature flag support (scope_token_path_refactor_step_20251009_01).
2. DONE **Dual-write persistence + diagnostics** - scope metadata now dual-written with diagnostics/backfill tooling (scope_token_path_refactor_step_20251009_02).
3. DONE **Guarding tests for scope & retrieval** - regression tests cover canonicalisation, fallback, and dispatcher propagation (unit_testing_expansion_step_20251009_03).
4. **Apply dual-write schema + reindex vectors** - roll out migration 20251012031808, update OpenSearch mappings, and run the initial backfill (plans/scope_token_path_refactor.md, plans/scope_testing_planner_notes.md).
5. DONE **Planner contracts + transcript logger** - landed `IPlannerTool`, `PlannerBase<TParams>`, `PlannerContext`, logging hooks; Vision planner now routes through the base (planning_the_planner_step_20251012_04).
6. DONE **ToolRegistry capability index + PlanAsync helper** - catalog + dispatcher telemetry wired for planner discovery (planning_the_planner_step_20251012_05).
7. DONE **Vectors/OpenSearch regression tests** - scoped KNN filters asserted, Query DSL promoted to path metadata (unit_testing_expansion_step_20251012_07).
8. **Flip path hashing flag (lower env)** - after backfill soak, enable `PathAwareHashing`, monitor collisions, document rollback (scope_token_path_refactor_step_20251009_08).
9. **Planner template & migration rollout** - template seeding + transcript/backlog store landed; coordinate `20251013181358_PlannerExecutions`/`AddFictionPlanBacklog` rollout and run the seeder in lower envs (planning_the_planner_step_20251012_04).
10. **Planner backlog plumbing** - force dispatcher + fiction jobs to propagate/consume `backlogItemId`, close the backlog loop Vision → Iterative → Architect → Scroll → Scene (plans/planning_the_planner.md, plans/fiction/phase-001_step_20250926_2327_inventory.md).
11. **Planner health & critique guard rails** - add planner health endpoint + template guard + self-critique budget controls before wider pilot (planning_the_planner.md, plans/scope_testing_planner_notes.md).
12. **Planner pilot migration** - adapt the smallest fiction planner to `PlannerBase`, validate with scripted fakes (planning_the_planner_step_20251009_06).
13. **Planner rollout guidance** - write developer recipe covering prompts, backlog usage, telemetry expectations (planning_the_planner_step_20251009_09).
14. **Documentation & README refresh** - update developer guidance post-cutover (shared checklist items).
15. **Scope diagnostics endpoint** - expose feature-flag + hashing health for ops (scope_token_path_refactor_step_20251012_11).

## Fast Follow Improvements
- Centralised `ScopePath` builders shared across services to ban ad-hoc path construction (plans/scope_testing_planner_notes.md, plans/scope_token_path_refactor.md).
- Planner telemetry contract + exposure (planner.* events, transcript repository surfacing) before wider adoption (plans/planning_the_planner.md, plans/scope_testing_planner_notes.md).
- Template repository for planner prompts keyed by `StepDescriptor` ids (plans/planning_the_planner.md, plans/scope_testing_planner_notes.md).
- Health/diagnostics endpoint exposing scope feature flags + metrics (plans/scope_testing_planner_notes.md, plans/scope_token_path_refactor.md).
- Ensure deterministic fakes (ScriptedLLM, ScriptedEmbeddingsClient, InMemoryVectorStore) are default in tests to unblock planner pilots (plans/unit_testing_expansion.md, plans/scope_testing_planner_notes.md).
- Backlog integration tests for fiction runners (Vision → Iterative → Architect → Scroll → Scene) to guarantee backlog items close out as phases progress (plans/fiction/phase-001_step_20250926_2327_inventory.md).

## Watch Items / Risks
- Token cost blow-ups if self-critique defaults stay on; make it opt-in per planner/persona (plans/planning_the_planner.md, plans/scope_testing_planner_notes.md).
- Performance hit from new principal/path indexes; benchmark before production flag flip (plans/scope_token_path_refactor.md, plans/scope_testing_planner_notes.md).
- Persona-only assets (Image Lab) remain intentionally persona-scoped; document exception to avoid accidental refactors (plans/scope_testing_planner_notes.md, plans/scope_token_path_refactor.md).
- Enforce step-note + snapshot workflow via PR template to prevent drift (plans/README.md referenced in plans/scope_testing_planner_notes.md).
