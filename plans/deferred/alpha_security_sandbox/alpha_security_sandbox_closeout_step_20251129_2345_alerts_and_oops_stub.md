Goal
- Wire sandbox violations into an alert surface, add correlation coverage, and begin OOPS lane scaffolding toward the sandbox/OOPS plan items.

Context
- Plan: `plans/alpha_security_sandbox_closeout.md`.
- Prior work added sandbox policy enforcement; this builds telemetry/alert hooks and stubs the OOPS worker path. Abuse-header/rate-limit e2e still pending.

Commands Executed
- dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "ToolDispatcher|Sandbox"
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "RequestCorrelationMiddlewareTests"

Files Changed
- src/Cognition.Clients/Tools/ToolDispatcher.cs (sandbox alert publishing)
- src/Cognition.Clients/ServiceCollectionExtensions.cs (wire sandbox alert telemetry/publisher)
- src/Cognition.Clients/Tools/Sandbox/ISandboxAlertPublisher.cs
- src/Cognition.Clients/Tools/Sandbox/LoggerSandboxAlertPublisher.cs
- src/Cognition.Clients/Tools/Sandbox/ToolSandboxAlertOptions.cs
- src/Cognition.Clients/Tools/Sandbox/ISandboxTelemetry.cs
- src/Cognition.Clients/Tools/Sandbox/LoggerSandboxTelemetry.cs
- src/Cognition.Clients/Tools/Sandbox/SandboxDecision.cs
- src/Cognition.Clients/Tools/Sandbox/SandboxPolicyEvaluator.cs
- src/Cognition.Clients/Tools/Sandbox/ToolSandboxWorkRequest.cs
- src/Cognition.Clients/Tools/Sandbox/IToolSandboxWorker.cs
- src/Cognition.Clients/Tools/Sandbox/ToolSandboxResult.cs
- src/Cognition.Clients/Tools/Sandbox/IToolSandboxApprovalQueue.cs
- tests/Cognition.Clients.Tests/Tools/ToolDispatcherScopeTests.cs (dependency updates)
- tests/Cognition.Clients.Tests/Tools/SandboxPolicyEvaluatorTests.cs
- tests/Cognition.Api.Tests/Infrastructure/RequestCorrelationMiddlewareTests.cs (middleware coverage)

Tests / Results
- dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "ToolDispatcher|Sandbox" (pass; preexisting nullable warnings remain)
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "RequestCorrelationMiddlewareTests" (pass)

Issues
- Abuse-header/rate-limit e2e still outstanding.
- CS8604 nullable warnings in ToolDispatcher remain (preexisting).

Decision
- Sandbox violations now go through telemetry and an alert publisher (logger-based for now) to surface denies/audit runs.
- Added OOPS worker/approval queue/result/request stubs for upcoming implementation.
- Correlation middleware now has a unit test.

Completion
- ✅

Next Actions
- Add abuse-header/rate-limit e2e coverage.
- Route sandbox violation alerts to Ops webhook (extend publisher) and integrate OOPS worker execution path.
