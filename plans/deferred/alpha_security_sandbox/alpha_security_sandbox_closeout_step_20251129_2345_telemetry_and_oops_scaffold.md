Goal
- Surface sandbox decisions in telemetry/alerts, add correlation propagation coverage, and start OOPS lane scaffolding per spec.

Context
- Plan: `plans/alpha_security_sandbox_closeout.md`.
- Building on sandbox policy enforcement added earlier today.

Commands Executed
- dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "ToolDispatcher|Sandbox"
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "RequestCorrelationMiddlewareTests"

Files Changed
- src/Cognition.Clients/Tools/ToolDispatcher.cs
- src/Cognition.Clients/Tools/Sandbox/ISandboxTelemetry.cs
- src/Cognition.Clients/Tools/Sandbox/LoggerSandboxTelemetry.cs
- src/Cognition.Clients/Tools/Sandbox/SandboxDecision.cs
- src/Cognition.Clients/Tools/Sandbox/SandboxPolicyEvaluator.cs
- src/Cognition.Clients/Tools/Sandbox/ToolSandboxWorkRequest.cs
- src/Cognition.Clients/Tools/Sandbox/IToolSandboxWorker.cs
- src/Cognition.Clients/Tools/Sandbox/ToolSandboxResult.cs
- src/Cognition.Clients/Tools/Sandbox/IToolSandboxApprovalQueue.cs
- src/Cognition.Clients/ServiceCollectionExtensions.cs
- tests/Cognition.Clients.Tests/Tools/ToolDispatcherScopeTests.cs
- tests/Cognition.Clients.Tests/Tools/SandboxPolicyEvaluatorTests.cs
- tests/Cognition.Api.Tests/Infrastructure/RequestCorrelationMiddlewareTests.cs

Tests / Results
- dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "ToolDispatcher|Sandbox" (pass; CS8604 warnings remain, same as before)
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "RequestCorrelationMiddlewareTests" (pass)

Issues
- None blocking; dispatcher still warns about possible null classPath (existing warning).

Decision
- Added sandbox telemetry hook and logging; dispatcher now records sandbox decisions (allow/audit/deny).
- Added initial OOPS lane scaffolding types (work request/result, worker, approval queue) for upcoming implementation.
- Added correlation middleware coverage to ensure headers/trace IDs propagate.

Completion
- ✅

Next Actions
- Wire sandbox violations to Ops alerts/telemetry stream (use alert publisher).
- Add abuse-header/rate-limit e2e coverage.
- Implement OOPS worker/approval jobs per spec and integrate dispatcher to route to sandbox runner.
