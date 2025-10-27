Goal
- Migrate the Scroll Refiner phase onto the shared planner framework and extend Ops alerting to support per-alert routing/SLO metadata so ops channels can differentiate stale vs breached conditions.

Context
- Chapter Architect conversion landed last step; Scroll Refiner was the next fiction planner still on bespoke logic. Ops wanted channel-specific paging tied to alert heuristics.

Commands Executed
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj`
- `dotnet test tests/Cognition.Jobs.Tests/Cognition.Jobs.Tests.csproj`
- `npm run build`

Files Changed
- `src/Cognition.Clients/Tools/Planning/Fiction/ScrollRefinerPlannerTool.cs` (new planner + prompt helpers)
- `src/Cognition.Clients/Tools/Fiction/Weaver/ScrollRefinerRunner.cs`
- `tests/Cognition.Jobs.Tests/Fiction/FictionPlannerPipelineTests.cs`
- `src/Cognition.Api/Infrastructure/StartupDataSeeder.cs`
- `src/Cognition.Api/Infrastructure/Alerts/OpsAlertingOptions.cs`
- `src/Cognition.Api/Infrastructure/Alerts/OpsWebhookAlertPublisher.cs`
- `tests/Cognition.Api.Tests/Infrastructure/OpsWebhookAlertPublisherTests.cs`
- `src/Cognition.Api/appsettings.json`
- `src/Cognition.Console/src/types/diagnostics.ts`, `src/Cognition.Console/src/pages/PlannerTelemetryPage.tsx`
- `README.md`, `plans/hot_targeted_todo.md`

Tests / Results
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj` ✅
- `dotnet test tests/Cognition.Jobs.Tests/Cognition.Jobs.Tests.csproj` ✅
- `npm run build` ✅ (Vite still strips `/*#__PURE__*/` comments from `@microsoft/signalr`; harmless warning)

Issues
- None beyond the known Vite warning above.

Decision
- Scroll Refiner now uses `PlannerBase` with a seeded `planner.fiction.scrollRefiner` template, and pipeline tests prove parity. Ops alerting supports per-alert/per-severity routing plus SLO thresholds and publishes the additional metadata consumed by the console.

Completion
- ✅

Next Actions
- Port Scene Weaver onto `PlannerBase`, then look at non-fiction planner migrations and richer Ops escalations (multi-channel webhooks, ACK workflows).
