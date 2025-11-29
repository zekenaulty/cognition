# Hot Targeted TODO

## Status
- Backlog resume + persona obligation UX now ships inside the console, so alpha users can actually unblock plans without CLI/editor help. API guardrails + controller regression prove the loop works.
- Tooling debt remains, but it’s scoped: the open gaps are all forward-looking user flows (persona resolution modal polish, telemetry cards, planner orchestration for actual authors) rather than admin-only dashboards.
- Treat the remaining tasks below as “what’s left before an author can reliably run a plan” instead of another engineering checklist.

## Definition of Done
- Alpha fiction backlog loop (API + console) resumes tasks with normalized status gates, required provider/model metadata, and action log/obligation UX fixes verified in staging.
- ScopePath enforcement + cosine scoring + sandbox/foundry orchestration deliverables from linked plans are merged and demoed in staging without open P0 bugs.
- Planner health dashboards expose SLA/backlog/resume metrics and Ops routing hooks (Slack/PagerDuty) with alerting playbooks captured in `planning_the_planner` artifacts.
- Non-fiction planner inventory + migration matrix exist in `planning_the_planner` docs, and legacy runner scaffolding/CI gaps identified here are either removed or tracked as scoped follow-ups.

## Cross-Plan Backbone

- **Alpha focus:** work remains seed-data only; no production rollout choreography is required (plans/planning_the_planner.md:8-13).
- **Execution chain:** 1) land alpha security/observability hardening P0s -> 2) catalogue non-fiction planners/orchestrators for PlannerBase -> 3) retire legacy runner scaffolding and harden CI/lint gates -> 4) prototype multi-channel Ops routing/ack flows -> 5) deepen planner health dashboards for SLA drill-downs (plans/alpha_security_observability_hardening.md, plans/planning_the_planner.md:184-187, plans/_next_session_prompt.md:4-19).

## Active Blockers

- None currently. Re-evaluate after the non-fiction inventory surfaces migration gaps or CI debt.

## Latest Findings

- Fiction backlog console review uncovered critical gaps: Resume ignores required agent/provider metadata and lets `complete`/numeric statuses re-run; persona obligation lists hide open items beyond the first six, omit descriptions/notes, and allow resolve/dismiss without audit logs; action log rows drop the description/context payload. Address these inside `FictionBacklogPanel` plus the backing API before calling the alpha loop “done.”
- Recent progress: backlog panel now surfaces contract drift alerts/warnings, obligation list shows inline resolution history, and plan wizard/resume dialogs persist provider/model/branch defaults locally to reduce mis-resume risk.

## Priority Action Stack

1. Ship the persona-obligation modal follow-ups: inline note history + resolve/dismiss CTA parity across Fiction Projects + Planner Telemetry pages (plans/fiction/phase-001/session_20251116_action_plan.md).
2. Let authors actually kick off a plan from the console (new plan wizard, persona selection, initial backlog seed) so we test the loop with humans, not seed data (plans/fiction/phase-001/plan-first-draft.md). _(Wizard now captures provider/model/branch defaults and feeds resume prefill; still need full end-to-end validation with live tokens.)_
3. Expand backlog telemetry widgets to include end-user alerts (stale backlog card, blocked lore popover) rather than admin-only metrics (plans/_next_session_prompt.md).
4. [Done] Normalize ScopePath usage post-review: audit for lingering `ScopePath.Parse`/direct constructors, add analyzer baselines, and document the factory-only contract (plans/alpha_security_observability_hardening.md, plans/scope_token_path_refactor.md:22-52).
5. Replace the in-memory vector score with cosine similarity so offline tests mirror OpenSearch behavior (tests/Cognition.Data.Vectors.Tests/*, plans/alpha_security_observability_hardening.md).
6. Land the sandbox + foundry missing pieces: implement the OOPS worker, queue/approval Hangfire jobs, and exercise HGTF end-to-end with integration tests (docs/specs/human_gated_tool_foundry.md, plans/alpha_security_observability_hardening.md).
7. Wire structured correlation logging now that planner quotas and authorization policies are in place (plans/alpha_security_observability_hardening.md, plans/planning_the_planner.md:183-205).
8. Catalogue non-fiction planners and adjacent orchestrators that should adopt PlannerBase; capture template prerequisites and scope expectations (plans/planning_the_planner.md:184, plans/_next_session_prompt.md, plans/scope_token_path_refactor.md:25-45).
9. Draft the non-fiction migration matrix and seed checklist updates in plans/planning_the_planner_rollout_recipe.md to cover non-fiction specifics (plans/planning_the_planner_rollout_recipe.md, plans/planning_the_planner.md:189-195).
10. Identify and remove remaining legacy runner scaffolding/tests that duplicate PlannerBase functionality; update CI/lint gates to enforce scripted pipeline coverage (plans/_next_session_prompt.md, plans/planning_the_planner.md:185).

## Fast Follow Improvements

- Capture alpha-only simplifications in the scope refactor docs to guide future data migrations once real data arrives (plans/scope_token_path_refactor.md:66-90).
- Produce a standing non-fiction planner adoption cadence review in plans/planning_the_planner.md once the migration matrix is drafted (plans/planning_the_planner.md:189-195).
- Keep backlog + lore telemetry guidance current as dashboard work evolves; align console widgets with planner health and SLA instrumentation (plans/planning_the_planner.md:197-205, plans/fiction/phase-001/plan-first-draft.md).
- Refresh developer onboarding/write-ups when non-fiction migrations begin so new planners inherit the rollout recipe without drift (plans/planning_the_planner_rollout_recipe.md).

## Watch Items / Risks

- Token cost blow-ups if self-critique defaults stay on; keep it opt-in per planner/persona (plans/planning_the_planner.md:203-205, plans/scope_testing_planner_notes.md).
- Performance ripple from path indexes once real workloads arrive; benchmark before broad rollout (plans/scope_token_path_refactor.md:41-52).
- Ensure persona-only assets (Image Lab) remain intentionally persona-scoped to avoid accidental refactors (plans/scope_testing_planner_notes.md, plans/scope_token_path_refactor.md).
- Stay aligned with the step-note + snapshot workflow so completed work lands under plans/completed/... promptly (plans/README.md via plans/scope_testing_planner_notes.md).
- Monitor lore fulfillment + backlog resume metrics for drift; alert Ops if the API/console usage drops or blocked items linger beyond SLA (plans/fiction/phase-001/plan-first-draft.md, plans/_next_session_prompt.md).
