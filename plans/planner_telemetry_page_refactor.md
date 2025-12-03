# Planner Telemetry Page Refactor

## Objective
Reduce `src/Cognition.Console/src/pages/PlannerTelemetryPage.tsx` from a monolith into testable, maintainable pieces (data hooks + presentational components), while fixing existing errors and keeping current behavior.

## Scope
- In: Split data fetching/transform/filters into hooks; move charts/tables/cards into components; resolve current TS/logic errors; preserve UX.
- Out: New features, redesign of metrics, backend changes.

## Deliverables
- Data hooks (e.g., `usePlannerTelemetryData`, `usePlannerFilters`).
- Presentational components for charts/tables/cards.
- Page container wiring the hooks/components.
- Fixed type/runtime errors; passing build.

## Migration/Rollout Order
1) Inventory errors and current props/state; define target component/hook boundaries.
2) Extract data hooks; keep page wiring intact.
3) Extract presentational components; remove inline logic.
4) Clean types, fix errors, and verify page loads.

## Testing/Verification
- Type checks/build passes.
- Manual: load Planner Telemetry page, verify charts/tables render and filters work as before.

## Risk/Rollback
- Risk: regressions in charts/filters; mitigate by incremental extraction and manual check.
- Rollback: keep page on branch; revert to prior version if needed.

## Worklog Protocol
- Step notes per `plans/README.md` with commands, files, tests, decisions, completion status.

## Checklist
- [ ] Data hooks extracted
- [ ] Presentational components extracted
- [ ] Page container simplified
- [ ] Errors fixed; build succeeds
