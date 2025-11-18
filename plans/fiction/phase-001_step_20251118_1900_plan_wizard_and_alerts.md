Goal
- Deliver the author-facing console wizard and backlog alert cards so writers can spin up plans without DB help and immediately see what needs attention.

Context
- Hot Targeted TODO item #2 and the Phase-001 plan both called for a real plan creation flow plus stale/backlog alerting once the backend factory landed.
- The API work was finished earlier today (step `_1730_plan_creation_backend`), so this slice focuses on console UX + telemetry polish.

Commands Executed
- `npm run build` (from `src/Cognition.Console`)

Files Changed
- `src/Cognition.Console/src/api/client.ts`
- `src/Cognition.Console/src/components/fiction/FictionPlanWizardDialog.tsx` (new)
- `src/Cognition.Console/src/components/fiction/FictionBacklogPanel.tsx`
- `src/Cognition.Console/src/pages/FictionProjectsPage.tsx`
- `src/Cognition.Console/src/pages/PlannerTelemetryPage.tsx`
- `src/Cognition.Console/src/types/fiction.ts`
- `plans/fiction/phase-001/plan-first-draft.md`

Tests / Results
- `npm run build` ✅ (Vite build green; artifacts reverted afterward)

Issues
- Vite build failed twice due to stray non-ASCII bullets introduced while adding alert copy; replaced them with ASCII strings before the final build.
- Planner Telemetry JSX needed some restructuring when the new obligations card and dialogs were wired in (caught by esbuild during the same run).

Decision
- Keep the wizard self-contained (new dialog component) so it can be reused later, and surface alert cards on both Fiction Projects + Planner Telemetry with CTA hooks into the resume/obligation dialogs to match the plan’s DoD.

Completion
- ✅

Next Actions
- Wire the remaining persona obligation polishing tasks (inline highlighting + telemetry reuse) called out in Goal #4 once backlog cards soak.
