Goal
- Replace the inline fiction data loader/reset logic in `PlannerTelemetryPage.tsx` with `usePlannerFictionContext`.

Context
- Plan: `plans/planner_telemetry_page_refactor.md`.

Commands Executed
- Edited `src/Cognition.Console/src/pages/PlannerTelemetryPage.tsx` to remove the monolithic fiction data effect and wire to `usePlannerFictionContext` + `refreshPlanData`.
- Updated resume and lore handlers to use hook operations.

Files Changed
- `src/Cognition.Console/src/pages/PlannerTelemetryPage.tsx`

Tests / Results
- Not run (pending cleanup/component extraction).

Issues
- UI still uses legacy loading/error flags; needs follow-up to bind to `fictionLoading`/`fictionError` or remove unused indicators.
- Potential stale references in UI components may need adjustment when we extract presentational parts.

Decision
- Keep hook-driven data flow; finish cleanup in subsequent steps.

Completion
- ✅

Next Actions
- Bind UI loading/error states to hook outputs (fictionLoading/fictionError) and remove any leftover dead flags.
- Begin extracting presentational components (status/alerts, fiction panels) per plan.
