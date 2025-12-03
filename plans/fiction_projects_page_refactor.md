# Fiction Projects Page Refactor

## Objective
Break down `src/Cognition.Console/src/pages/FictionProjectsPage.tsx` into focused hooks and components, fix existing errors, and keep current UX/behavior while making the code maintainable.

## Scope
- In: Extract data fetching/commands/filters into hooks; split UI (lists/cards/dialogs) into presentational components; resolve current TS/logic errors; maintain existing features.
- Out: New fiction features or schema changes; major UX redesign.

## Deliverables
- Data/command hooks for fiction projects (load, filter, mutate).
- Presentational components for project list/cards and dialogs.
- Simplified page container wiring hooks to components.
- Clean types; build passing.

## Migration/Rollout Order
1) Inventory errors/state/props; define hook/component boundaries.
2) Extract data hooks; keep page behavior intact.
3) Extract presentational components; remove inline logic.
4) Fix types/errors; verify page loads and actions work.

## Testing/Verification
- Type checks/build passes.
- Manual: load Fiction Projects page, ensure list/details/actions work as before.

## Risk/Rollback
- Risk: regressions in list/actions; mitigate via incremental extraction and manual check.
- Rollback: revert to prior version if needed.

## Worklog Protocol
- Step notes per `plans/README.md` with commands, files, tests, decisions, completion status.

## Checklist
- [ ] Data hooks extracted
- [ ] Presentational components extracted
- [ ] Page container simplified
- [ ] Errors fixed; build succeeds
