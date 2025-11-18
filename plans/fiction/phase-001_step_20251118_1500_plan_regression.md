Goal
- Finish the alpha backlog loop by shipping end-user persona obligation controls, surfacing backlog/action feeds in telemetry, and proving the resume→lore→obligation workflow end-to-end.

Context
- Local dev environment only (no staging). Console served via Vite, API verified through controller tests.
- Builds on the backlog metadata hardening from the prior step note.

Commands Executed
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj`

Files Changed
- `src/Cognition.Console/src/components/fiction/FictionBacklogPanel.tsx`
- `src/Cognition.Console/src/components/fiction/PersonaObligationActionDialog.tsx`
- `src/Cognition.Console/src/components/fiction/backlogUtils.ts`
- `src/Cognition.Console/src/pages/FictionProjectsPage.tsx`
- `src/Cognition.Console/src/pages/PlannerTelemetryPage.tsx`
- `tests/Cognition.Api.Tests/Controllers/FictionPlansControllerTests.cs`
- `plans/fiction/phase-001_step_20251118_1500_plan_regression.md`

Tests / Results
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj` (pass, 67 tests) — includes the new `Plan_workflow_resume_lore_and_obligation_resolution` regression.

Issues
- UI pieces validated locally only; no staging to exercise real Hangfire jobs, so the E2E proof relies on the controller test harness.

Decision
- Added a required-notes modal for persona obligation resolve/dismiss actions, wired to the existing API, so admins must record why an obligation closed.
- Planner telemetry now consumes the richer backlog action feed + obligation metadata to keep Ops in the loop without digging into console devtools.
- Captured the full plan workflow in a single regression test so backlog resumes, lore fulfillment, and obligation resolution all stay guarded in CI.

Completion
- ✅

Next Actions
- None for this slice; backlog/obligation console work now satisfies the alpha DoD.
