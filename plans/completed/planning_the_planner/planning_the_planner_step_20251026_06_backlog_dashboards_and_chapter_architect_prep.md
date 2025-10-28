Goal
- Surface backlog telemetry dashboards/alerts fed by `/api/diagnostics/planner` + `/api/diagnostics/opensearch`, and seed the Chapter Architect planner template so the next migration can start immediately.

Context
- Planner health + OpenSearch endpoints landed last session but had no UI/alerting hooks.
- Chapter Architect is the next pilot; it needs a template id before we move the runner onto `PlannerBase`.

Commands Executed
- `npm run build`
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj`

Files Changed
- `src/Cognition.Api/Infrastructure/StartupDataSeeder.cs`
- `src/Cognition.Console/src/pages/PlannerTelemetryPage.tsx`
- `src/Cognition.Console/src/api/client.ts`
- `src/Cognition.Console/src/types/diagnostics.ts`
- `src/Cognition.Console/src/components/navigation/PrimaryDrawer.tsx`
- `src/Cognition.Console/src/App.tsx`
- `README.md`
- `plans/hot_targeted_todo.md`

Tests / Results
- `npm run build` (passes, Vite stripped a couple of `/*#__PURE__*/` annotations from signalR during bundling)
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj` (18 tests, all passing)

Issues
- Vite warns that it removes two Rollup annotations inside `@microsoft/signalr` when bundling; harmless but noted.

Decision
- Seeded `planner.fiction.chapterArchitect` in the startup seeder so the next migration won’t immediately trip health alerts.
- Added a dedicated “Backlog Telemetry” console view that blends planner health, backlog KPIs, alert heuristics (stale/orphaned/flapping/critique exhaustion), and OpenSearch diagnostics.

Completion
- ✅

Next Actions
- Migrate `ChapterArchitectRunner` to a `PlannerBase`-derived tool that consumes the new template and emits backlog metadata.
- Extend `FictionPlannerPipelineTests` with scripted Chapter Architect responses that exercise the new planner path.
- Wire backlog alerting into notifications/ops paging once the UI heuristics soak.*** End Patch
