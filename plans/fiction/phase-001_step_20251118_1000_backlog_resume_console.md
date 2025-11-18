Goal
- Close the backlog console gaps called out in `plans/hot_targeted_todo.md`: block unsafe resumes (missing provider/agent metadata or completed statuses), enrich backlog action logs, and surface persona obligation context so the alpha DoD is testable.

Context
- Work performed locally on main; staging is not available.
- Focused on `/api/fiction/plans/{id}/backlog` + console panel integrations.

Commands Executed
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj`

Files Changed
- `src/Cognition.Api/Controllers/FictionPlansController.cs`
- `tests/Cognition.Api.Tests/Controllers/FictionPlansControllerTests.cs`
- `src/Cognition.Console/src/components/fiction/FictionBacklogPanel.tsx`
- `plans/fiction/phase-001_step_20251118_1000_backlog_resume_console.md`

Tests / Results
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj` (pass, 66 tests)

Issues
- Cannot validate the updated resume workflow against a staging environment; relying on unit coverage + UI review.

Decision
- API now rejects resume requests lacking agent/provider IDs, and the console disables resume buttons until required metadata is present.
- Backlog action logs and persona obligations show descriptions, resolution notes, and metadata so operators have enough context to act.

Completion
- ✅

Next Actions
- Extend console forms to capture provider/model overrides where admin metadata is missing.
- Add persona obligation resolution note capture in the UI so operators can log context before resolving/dismissing.
