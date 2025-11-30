Goal
- Add abuse-header/rate-limit e2e coverage with a proper test host, route sandbox violations to Ops webhook (logger + HTTP), and scaffold an approval queue/worker path (OOPS lane) integrated into the dispatcher.

Context
- Plan: `plans/alpha_security_sandbox_closeout.md`.
- Builds on prior sandbox policy enforcement.

Commands Executed
- dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "ToolDispatcher|Sandbox"
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "AbuseHeadersAndRateLimitE2ETests"

Files Changed
- src/Cognition.Api/Program.cs (partial Program for test host)
- tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj (add Microsoft.AspNetCore.Mvc.Testing)
- tests/Cognition.Api.Tests/Controllers/AbuseHeadersAndRateLimitE2ETests.cs (WebApplicationFactory harness; correlation + rate-limit behavior)
- src/Cognition.Clients/Tools/ToolDispatcher.cs (enqueue-on-deny path; alert publishing; options/queue/worker injection)
- src/Cognition.Clients/ServiceCollectionExtensions.cs (register sandbox options setup, alert publisher, approval queue, worker)
- src/Cognition.Clients/Tools/Sandbox/ToolSandboxOptions.cs (EnqueueOnDeny)
- src/Cognition.Clients/Tools/Sandbox/LoggerSandboxAlertPublisher.cs (webhook send when configured)
- src/Cognition.Clients/Tools/Sandbox/ToolSandboxOptionsSetup.cs
- src/Cognition.Clients/Tools/Sandbox/InMemorySandboxApprovalQueue.cs
- src/Cognition.Clients/Tools/Sandbox/NoopSandboxWorker.cs
- tests/Cognition.Clients.Tests/Tools/ToolDispatcherScopeTests.cs (constructor updates/stubs)

Tests / Results
- dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "ToolDispatcher|Sandbox" (pass; existing nullable warnings remain)
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "AbuseHeadersAndRateLimitE2ETests" (pass)

Issues
- Rate-limit test asserts correlation headers and that second call is either OK or 429 (limits can vary); no 429 guarantee.
- Sandbox queue currently in-memory; worker is a noop placeholder (to be replaced with real OOPS runner).

Decision
- Sandbox violations now publish alerts via logger and optional webhook; dispatcher enqueues denied executions for approval when configured (options).
- Added test host harness for abuse/rate-limit + correlation coverage.
- OOPS lane interfaces and queue/worker stubs are integrated into dispatcher routing for future implementation.

Completion
- ✅

Next Actions
- Replace noop worker with actual OOPS runner and approval job flow per spec; add webhook alert tests.
- Enhance rate-limit/abuse tests with stricter assertions once limits/headers are finalized.
