Goal
- Move planner contracts from proposal to implementation, wire telemetry/logging, and expose capability discovery so tooling can execute planners through the shared base.

Changes
- Introduced `PlannerContracts.cs` with concrete `PlannerMetadata`, `PlannerResult`, telemetry context helpers, and artifact utilities.
- Extended `PlannerBase<TParameters>` logging/telemetry flow and added scoped context builder for Vision planner runner.
- Added `PlannerCatalog` to surface capability-driven discovery and registered it via `ServiceCollectionExtensions`.
- Migrated `VisionPlannerRunner` to the new base and ensured planner telemetry/logging flows through ToolDispatcher.
- Augmented tests: capability lookup (`PlannerCatalogTests`), dispatcher telemetry (`ToolDispatcherScopeTests`), and planner orchestration via scripted fakes.

Tests / Validation
- `dotnet test tests/Cognition.Data.Vectors.Tests/Cognition.Data.Vectors.Tests.csproj`
- Planner tests in `tests/Cognition.Clients.Tests` currently fail because the existing in-memory `ToolDispatcherDbContext` still maps LLM provider/model metadata; tracked as follow-up (see Next Actions).

Next Actions
- Trim the test `ToolDispatcherDbContext` model (ignore provider/model metadata) so the planner telemetry tests run clean.
- Persist planner transcripts/metrics via a repository hook and surface planner diagnostics alongside scope dual-write flags.
- Migrate the next fiction planner (iterative/story) through the shared base to validate real-world prompts/templates.

Completion
- 2025-10-12
