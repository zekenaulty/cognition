Goal
- Extract diagnostics loading (planner/open search) into a reusable hook and wire the page to it.

Context
- Plan: `plans/planner_telemetry_page_refactor.md`.
- Target: `src/Cognition.Console/src/pages/PlannerTelemetryPage.tsx`.

Commands Executed
- Created diagnostics hook and rewired page (no build run yet).

Files Changed
- Added `src/Cognition.Console/src/hooks/usePlannerTelemetryData.ts`.
- Updated `src/Cognition.Console/src/pages/PlannerTelemetryPage.tsx` to consume the hook and remove inline diagnostics state/effects.

Tests / Results
- Not run (pending further refactor).

Issues
- Hook returns early when token is missing, leaving `loading` true if unauthenticated; prior code behaved similarly (assumes auth present).
- Fiction data still inline; further extraction needed.

Decision
- Proceed with hook extraction; keep behavior parity for authenticated flow.

Completion
- ✅

Next Actions
- Extract fiction plan context (roster/backlog/obligations) into a hook.
- Begin separating presentational components (status/alerts, fiction panels).
