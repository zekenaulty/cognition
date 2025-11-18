Goal
- Ship the last missing server pieces for the plan creation wizard: real project CRUD, a backend plan factory that seeds backlog + conversation metadata, and coverage so we can wire the console without CLI helpers.

Context
- Hot targeted TODO item #2 (`plans/hot_targeted_todo.md`) calls out the author-facing plan wizard as the next user-visible slice.
- `plans/fiction/phase-001/plan-first-draft.md` and the 2025-11-16 session notes both flag this as the current blocker between console-only admin flows and real author usage.

Commands Executed
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj`
- `git status -sb`

Files Changed
- `src/Cognition.Api/Infrastructure/Planning/FictionPlanCreator.cs` (new service + backlog seeds)
- `src/Cognition.Api/Controllers/FictionPlansController.cs`
- `src/Cognition.Api/Controllers/FictionProjectsController.cs`
- `src/Cognition.Api/Program.cs`
- `tests/Cognition.Api.Tests/Controllers/FictionPlansControllerTests.cs`
- `tests/Cognition.Api.Tests/Controllers/FictionProjectsControllerTests.cs`
- `tests/Cognition.Api.Tests/Infrastructure/FictionPlanCreatorTests.cs`

Tests / Results
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj` ✅ (75 tests, 0 failed)

Issues
- In-memory EF choked on `Conversation.Metadata` during the new service tests; resolved by ignoring that property inside `FictionPlansTestDbContext`.

Decision
- Land API + service scaffolding before touching the console so subsequent slices can assume `/api/fiction/projects` + `POST /api/fiction/plans` exist and return real backlog metadata.

Completion
- ✅

Next Actions
- Wire the console wizard (project picker/creator + persona selection) against the new endpoints and relax the resume UI so provider/model can be supplied at run time.
