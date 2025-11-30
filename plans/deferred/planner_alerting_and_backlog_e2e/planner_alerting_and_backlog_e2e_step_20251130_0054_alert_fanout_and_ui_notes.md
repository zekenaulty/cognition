Goal
- Add targeted alert fanout coverage to reduce ambiguity before deferring.

Context
- Plan: `plans/planner_alerting_and_backlog_e2e.md`.
- Focus: ensure backlog alerts publish via planner health service and note remaining gaps.

Commands Executed
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "PlannerHealthAlertFanoutTests"

Files Changed
- tests/Cognition.Api.Tests/Infrastructure/PlannerHealthAlertFanoutTests.cs

Tests / Results
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "PlannerHealthAlertFanoutTests" (pass)

Remaining Gaps (to defer if needed)
- No end-to-end webhook capture for backlog/obligation alerts (publisher is covered separately).
- Console alert chips not explicitly asserted; nav wiring unchanged.
- Ops routing/runbook samples still to document.
