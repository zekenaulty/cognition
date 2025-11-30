Goal
- Lock hotspots and scope for testing/CI gap close; note waivers for `unit_testing_expansion` items.

Context
- Plan: `plans/testing_and_ci_gap_close.md`.

Decisions / Scope
- Hotspots to cover now: ToolDispatcher enforce/deny/queue/alerts; sandbox policy/alerts; scope path flags (dual-write/path-aware hashing); rate-limit/abuse API net; planner telemetry/alerts on failure/quota.
- Waive for now (documented): broader `unit_testing_expansion` checklist beyond these hotspots; HGTF until implemented.

Next Actions
- Add targeted tests for hotspots and API net.
- Add mutation/coverage config + CI workflow; document commands/thresholds.
