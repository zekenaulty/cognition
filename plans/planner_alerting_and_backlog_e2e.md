# Planner Alerting & Backlog E2E

## Objective
- Close planner alerting/backlog resume gaps and prove fiction pipeline alert fanout end-to-end in pre-alpha.

## Scope
- Planner alert publishers, backlog resume/obligation flows, console telemetry/widgets, Ops webhook routing, and supporting docs/runbooks.
- Pre-alpha only (single dev environment; no staging/prod choreography).

## Definition of Done
- Fiction planner alert scenarios are covered by end-to-end tests: pipeline emits alerts, Ops webhook receives them, and payloads include expected metadata (plan/backlog/task identifiers, severity).
- Backlog resume and obligation flows validated via API and console tests (agent-first; persona ownership preserved where relevant).
- PlannerHealth dashboards/widgets include alert routes and basic SLO signals (latency/queue/critique where available).
- Ops routing config samples and runbooks documented for pre-alpha use; alert handlers implemented and wired.

## Deliverables
- Additional planner alert tests (unit/integration/e2e) covering pipeline + Ops webhook fanout.
- Backlog resume/obligation contracts and tests (API + console).
- Console telemetry widgets (PlannerHealth + backlog/obligation alerts) with agent-first context.
- Ops routing config samples (webhook), alert handler implementations, and runbooks.

## Migration / Rollout Order (pre-alpha)
1) Map current alert publishers to backlog/obligation events (tool dispatcher, planner runners, backlog scheduler).
2) Add failing tests for missing alert routes and webhook fanout.
3) Implement/extend alert handlers and wire Ops webhook routing.
4) Update console panels/widgets to surface alerts + SLO signals; validate agent-first flows.
5) Document runbooks/config; rerun e2e to confirm.

## Testing / Verification
- E2E: pipeline alert emission → Ops webhook receipt with expected payload; backlog resume/obligation flows via API/console.
- API/Service: alert publisher routing, PlannerHealth endpoints, backlog resume/obligation contracts.
- UI: console telemetry widgets show alerts/SLOs; agent-first context respected.
- Docs: runbook steps validated in pre-alpha environment.

## Risks / Mitigations
- Missing alert coverage: start with failing tests to drive routes.
- Payload drift: assert schema in tests.
- Console regressions: gate via e2e UI/API checks in pre-alpha.

## Worklog Protocol
- Follow `plans/README.md`: each step in `plans/planner_alerting_and_backlog_e2e_step_YYYYMMDD_HHMM_<topic>.md` with goal/context/commands/files/tests/issues/decision/completion/next actions.

## Initial Steps
1) Map current alert publishers to backlog/obligation events.
2) Add failing tests for missing routes and webhook fanout.
3) Implement alert handlers and update console panels/widgets.
