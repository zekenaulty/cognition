Prompt for next session (2025-10-26)
-----------------------------------

Status recap
- PlannerBase now drives vision, iterative, chapter architect, scroll refiner, **and scene weaver** planners; templates are seeded (`StartupDataSeeder`) and the scripted pipeline test verifies the full vision -> scene flow.
- Planner health emits structured `alerts`, Ops webhook routing supports per-alert overrides + SLO metadata, and configuration validation fails fast if webhooks/SLO thresholds are misconfigured. All work remains alpha-only; no production rollout pipeline exists yet.
- Latest step notes: `plans/completed/planning_the_planner/planning_the_planner_step_20251026_07_chapter_architect_planner_and_ops_alerting.md`, `plans/completed/planning_the_planner/planning_the_planner_step_20251026_08_scroll_refiner_planner_and_ops_routes.md`, `plans/completed/planning_the_planner/planning_the_planner_step_20251026_09_scene_weaver_planner_ops_validation_and_rollout_recipe.md`.

Next targets
1. Close alpha security/observability P0s (API rate limits + quotas, ScopePath factory lock, authorization policies, planner token budgets, correlation logging) per `plans/alpha_security_observability_hardening.md`.
2. Catalogue non-fiction planners (and other orchestrators) that should adopt `PlannerBase`; outline prerequisites and template needs.
3. Deprecate legacy runner scaffolding and tighten CI/lint gates around planner templates + scripted pipeline coverage now that fiction migrations are complete.
4. Explore multi-channel Ops publishing (Slack, PagerDuty) and acknowledgement workflows once webhook payloads stabilise.
5. Extend planner health dashboards with deeper alert drill-downs / SLA visualisations to support upcoming planners.

Getting started
- Review `plans/planning_the_planner.md` for the refreshed status/next steps, plus `plans/hot_targeted_todo.md` to sync priorities.
- Inventory non-fiction planner candidates under `src/Cognition.Clients/Tools` and `src/Cognition.Jobs` to draft the migration matrix.
- Identify remaining legacy runner scaffolding/tests that can be retired now that fiction planners share `PlannerBase`.
- Sketch multi-channel Ops routing requirements (Slack/PagerDuty) using the validated webhook payloads as the contract baseline.
