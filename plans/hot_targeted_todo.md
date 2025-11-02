# Hot Targeted TODO

## Cross-Plan Backbone

- **Alpha focus:** work remains seed-data only; no production rollout choreography is required (plans/planning_the_planner.md:8-13).
- **Execution chain:** 1) land alpha security/observability hardening P0s -> 2) catalogue non-fiction planners/orchestrators for PlannerBase -> 3) retire legacy runner scaffolding and harden CI/lint gates -> 4) prototype multi-channel Ops routing/ack flows -> 5) deepen planner health dashboards for SLA drill-downs (plans/alpha_security_observability_hardening.md, plans/planning_the_planner.md:184-187, plans/_next_session_prompt.md:4-19).

## Active Blockers

- None currently. Re-evaluate after the non-fiction inventory surfaces migration gaps or CI debt.

## Priority Action Stack

1. Make the fiction backlog authoritative: drop hard-coded phase invocations, route `ConversationTask` scheduling off backlog state, and extend regression coverage so planners/telemetry align (plans/fiction/phase-001_step_20250926_2327_inventory.md).
2. Normalize ScopePath usage post-review: audit for lingering `ScopePath.Parse`/direct constructors, add analyzer baselines, and document the factory-only contract (plans/alpha_security_observability_hardening.md, plans/scope_token_path_refactor.md:22-52).
3. Replace the in-memory vector score with cosine similarity so offline tests mirror OpenSearch behavior (tests/Cognition.Data.Vectors.Tests/*, plans/alpha_security_observability_hardening.md).
4. Land the sandbox + foundry missing pieces: implement the OOPS worker, queue/approval Hangfire jobs, and exercise HGTF end-to-end with integration tests (docs/specs/human_gated_tool_foundry.md, plans/alpha_security_observability_hardening.md).
5. Wire structured correlation logging now that planner quotas and authorization policies are in place (plans/alpha_security_observability_hardening.md, plans/planning_the_planner.md:183-205).
6. Catalogue non-fiction planners and adjacent orchestrators that should adopt PlannerBase; capture template prerequisites and scope expectations (plans/planning_the_planner.md:184, plans/_next_session_prompt.md:10-13, plans/scope_token_path_refactor.md:25-45).
7. Draft the non-fiction migration matrix and seed checklist updates in plans/planning_the_planner_rollout_recipe.md to cover non-fiction specifics (plans/planning_the_planner_rollout_recipe.md, plans/planning_the_planner.md:189-195).
8. Identify and remove remaining legacy runner scaffolding/tests that duplicate PlannerBase functionality; update CI/lint gates to enforce scripted pipeline coverage (plans/_next_session_prompt.md:11-13, plans/planning_the_planner.md:185).
9. Prototype multi-channel Ops routing (Slack, PagerDuty) using the validated webhook payloads; define acknowledgement semantics and Ops override metadata (plans/_next_session_prompt.md:14-16, plans/planning_the_planner.md:186).
10. Extend planner health dashboards with alert drill-downs, SLA visualisations, and recent transcript surfacing for upcoming planners (plans/_next_session_prompt.md:17-19, plans/planning_the_planner.md:187, plans/planning_the_planner.md:205-206).

## Fast Follow Improvements

- Capture alpha-only simplifications in the scope refactor docs to guide future data migrations once real data arrives (plans/scope_token_path_refactor.md:66-90).
- Produce a standing non-fiction planner adoption cadence review in plans/planning_the_planner.md once the migration matrix is drafted (plans/planning_the_planner.md:189-195).
- Keep backlog telemetry guidance current as dashboard work evolves; align console widgets with planner health SLA instrumentation (plans/planning_the_planner.md:197-205).
- Refresh developer onboarding/write-ups when non-fiction migrations begin so new planners inherit the rollout recipe without drift (plans/planning_the_planner_rollout_recipe.md).

## Watch Items / Risks

- Token cost blow-ups if self-critique defaults stay on; keep it opt-in per planner/persona (plans/planning_the_planner.md:203-205, plans/scope_testing_planner_notes.md).
- Performance ripple from path indexes once real workloads arrive; benchmark before broad rollout (plans/scope_token_path_refactor.md:41-52).
- Ensure persona-only assets (Image Lab) remain intentionally persona-scoped to avoid accidental refactors (plans/scope_testing_planner_notes.md, plans/scope_token_path_refactor.md).
- Stay aligned with the step-note + snapshot workflow so completed work lands under plans/completed/... promptly (plans/README.md via plans/scope_testing_planner_notes.md).
