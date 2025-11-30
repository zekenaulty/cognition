# Planner Alerting & Backlog E2E (Deferred)

Status: **Deferred**

Reason: Wrapped minimal fanout validation; remaining work needs real alert/webhook plumbing and more UI time. Current state:
- Added `PlannerHealthAlertFanoutTests` proving backlog stale alerts publish via `IPlannerAlertPublisher`.
- Planner health console already renders alerts; backlog/obligation alert routes exist in code.
- Ops webhook publisher is present and unit-tested; sandbox/queue admin API exists in admin-only surface.

Deferred scope:
- End-to-end webhook capture test (real receiver) and console alert assertions.
- Backlog/obligation alert handling wired through UI panels with runbook/docs.
- SLO/dashboard wiring for planner alert routes once telemetry strategy stabilizes.
