Prompt for next session (2025-11-02)
-----------------------------------

Status recap
- World-bible telemetry now emits warnings/alerts and Ops payloads thread correlation IDs end-to-end (planning_the_planner_step_20251102_1500_world_bible_alerts_impl.md).
- ScopePath factory enforcement is guarded by analyzers; correlation IDs propagate across API, jobs, and planner telemetry (TRACK-482).
- Chat and Users DTOs gained attribute-based validation + unit coverage; remaining controllers still need pass (see lpha_security_observability_hardening_step_20251102_2100_dto_validation.md).
- Nullable reference types enabled solution-wide; in-memory vector store scores and sorts by cosine before trimming 	opK.

Next targets
1. Finish DTO validation coverage (Config, Tools, Personas, Conversations) and align error responses per security plan.
2. Close remaining alpha security P0s: rate-limit regressions, Ops webhook multi-channel routing, explicit admin policies.
3. Catalogue/plan non-fiction planner migrations and retire legacy runner scaffolding.
4. Implement OpenSearch schema guard + template self-test before broadening planner rollout.
5. Extend planner health dashboards + Ops routing to surface new world-bible alerts downstream.

Getting started
- Review plans/alpha_security_observability_hardening.md and latest step notes for outstanding security tasks.
- Sweep controllers still lacking validation attributes; decide if FluentValidation integration is resurrected or stick with DataAnnotations.
- Inventory non-fiction planners/job orchestrators under src/Cognition.Clients/Tools and src/Cognition.Jobs for migration matrix.
- Sketch Ops multi-channel routing (Slack/PagerDuty) leveraging the new world-bible alert ids.
