Goal
- Port the Chapter Architect phase onto `PlannerBase` (template-backed prompts, telemetry, transcripts) and wire planner backlog heuristics into the Ops paging hook so stale/flapping items raise tickets automatically.

Context
- The previous step delivered the backlog dashboard + template seeding; today finishes the chapter planner migration plus alerting integration called out in the Fast Follow list.

Commands Executed
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj`
- `dotnet test tests/Cognition.Jobs.Tests/Cognition.Jobs.Tests.csproj`
- `npm run build`

Files Changed
- `src/Cognition.Clients/Tools/Planning/Fiction/ChapterArchitectPlannerTool.cs` (new planner + parameters)
- `src/Cognition.Clients/Tools/Fiction/Weaver/ChapterArchitectRunner.cs`
- `tests/Cognition.Jobs.Tests/Fiction/FictionPlannerPipelineTests.cs`
- `src/Cognition.Api/Infrastructure/Planning/PlannerHealthService.cs`
- `src/Cognition.Api/Infrastructure/Planning/IPlannerAlertPublisher.cs`
- `src/Cognition.Api/Infrastructure/Alerts/*`
- `src/Cognition.Api/Program.cs`
- `src/Cognition.Api/appsettings.json`
- `README.md`, `plans/hot_targeted_todo.md`
- `src/Cognition.Console/src/**/*(PlannerTelemetryPage|diagnostics.ts|api/client.ts)`

Tests / Results
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj` ✅
- `dotnet test tests/Cognition.Jobs.Tests/Cognition.Jobs.Tests.csproj` ✅
- `npm run build` ✅ (vite warns about stripping `/*#__PURE__*/` annotations in `@microsoft/signalr`; same as prior builds)

Issues
- None blocking; noted the recurring Vite warning so it doesn't surprise the next run.

Decision
- Chapter Architect now uses `PlannerBase`, seeded template `planner.fiction.chapterArchitect`, and scripted coverage via `FictionPlannerPipelineTests`. Planner health now emits structured `alerts` that the console consumes and the new Ops webhook sender posts (debounced, severity-filtered) so stale/flapping items auto-page ops.

Completion
- ✅

Next Actions
- Migrate the next fiction planner (scroll refiner or scene weaver) onto `PlannerBase`, then extend Ops alert coverage to non-fiction orchestrators once their planners land.
