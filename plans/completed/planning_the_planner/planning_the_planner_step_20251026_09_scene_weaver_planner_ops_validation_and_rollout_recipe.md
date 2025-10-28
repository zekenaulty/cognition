Goal
- Migrate the Scene Weaver phase to `PlannerBase`, harden Ops alerting with startup validation, and publish the rollout recipe covering planner migrations and lower-environment guidance.

Context
- Scroll Refiner completed the previous step; Scene Weaver was the final fiction planner on bespoke scaffolding. Ops requested stronger guard rails so misconfigured webhooks fail fast, and the team needed a documented checklist before moving on to non-fiction planners.

Commands Executed
- `dotnet test tests/Cognition.Jobs.Tests/Cognition.Jobs.Tests.csproj`
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj`

Files Changed
- `src/Cognition.Clients/Tools/Planning/Fiction/SceneWeaverPlannerTool.cs` (new planner tool + prompt helpers)
- `src/Cognition.Clients/Tools/Fiction/Weaver/SceneWeaverRunner.cs`
- `tests/Cognition.Jobs.Tests/Fiction/FictionPlannerPipelineTests.cs`
- `src/Cognition.Api/Infrastructure/StartupDataSeeder.cs`
- `src/Cognition.Api/Infrastructure/Alerts/OpsAlertingOptionsValidator.cs` (new configuration validator)
- `src/Cognition.Api/Program.cs`
- `tests/Cognition.Api.Tests/Infrastructure/OpsAlertingOptionsValidatorTests.cs`
- `README.md`
- `plans/planning_the_planner.md`
- `plans/planning_the_planner_rollout_recipe.md` (new runbook)
- `plans/hot_targeted_todo.md` (status notes)*

\*No unrelated changes were modified.

Tests / Results
- `dotnet test tests/Cognition.Jobs.Tests/Cognition.Jobs.Tests.csproj` ✔️
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj` ✔️

Issues
- None encountered; validator caught misconfiguration scenarios during unit tests.

Decision
- Scene Weaver now rides `PlannerBase` with seeded template `planner.fiction.sceneWeaver`; pipeline tests assert scene transcript metadata and backlog closure.
- Ops alerting validates configuration at startup (`OpsAlertingOptionsValidator` + `ValidateOnStart`) so missing webhooks or non-positive SLO thresholds block deployment.
- README and plan docs document the routing/SLO examples and reference the new rollout recipe capturing migration + lower-env steps.

Completion
- ✅

Next Actions
- Draft the detailed planner rollout recipe & README guidance (completed here); next focus shifts to non-fiction planner prerequisites and exploring multi-channel Ops publishers / ACK workflows.
- Monitor planner health dashboards post-rollout to ensure Scene Weaver parity holds and alert routing behaves as expected.
