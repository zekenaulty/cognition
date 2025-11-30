Goal
- Replace the sandbox noop with an OOP runner stub (Job Object on Windows), integrate dispatcher enforcement with queue/worker, add sandbox queue API, and add abuse/rate-limit e2e with test host.

Context
- Plan: `plans/alpha_security_sandbox_closeout.md`.

Commands Executed
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "AbuseHeadersAndRateLimitE2ETests"
- dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "ToolDispatcher|Sandbox"

Files Changed
- src/Cognition.Clients/Tools/Sandbox/ProcessSandboxWorker.cs (OOP runner stub: process + Job Object on Windows; best-effort kill)
- src/Cognition.Clients/Tools/Sandbox/ToolSandboxOptions.cs (EnqueueOnDeny)
- src/Cognition.Clients/Tools/Sandbox/ToolSandboxOptionsSetup.cs
- src/Cognition.Clients/Tools/Sandbox/InMemorySandboxApprovalQueue.cs (dequeue/snapshot)
- src/Cognition.Clients/Tools/Sandbox/IToolSandboxApprovalQueue.cs (dequeue/snapshot contract)
- src/Cognition.Clients/Tools/Sandbox/LoggerSandboxAlertPublisher.cs (webhook POST when configured)
- src/Cognition.Clients/Tools/Sandbox/IToolSandboxWorker.cs (ProcessSandboxWorker registered)
- src/Cognition.Clients/ServiceCollectionExtensions.cs (register OOP worker, queue, options setup)
- src/Cognition.Clients/Tools/ToolDispatcher.cs (enqueue-on-deny; invokes worker/queue/alerts)
- src/Cognition.Api/Controllers/SandboxController.cs (admin API to inspect/approve queue)
- src/Cognition.Api/Program.cs (partial Program for tests)
- tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj (add Microsoft.AspNetCore.Mvc.Testing)
- tests/Cognition.Api.Tests/Controllers/AbuseHeadersAndRateLimitE2ETests.cs (test host harness)
- tests/Cognition.Clients.Tests/Tools/ToolDispatcherScopeTests.cs (updated dependencies/stubs)

Tests / Results
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "AbuseHeadersAndRateLimitE2ETests" (pass)
- dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "ToolDispatcher|Sandbox" (pass; nullable warnings unchanged)

Issues
- Sandbox worker is still a minimal OOP stub (runs `cmd /c exit 0` or `/bin/sh -c "exit 0"` with Job Object on Windows). No actual tool execution yet.
- Queue API is admin-only and returns/approves the in-memory queue; no console view yet.

Decision
- Dispatcher enqueues denied executions when `Sandbox:EnqueueOnDeny` is enabled; alerts publish via logger/webhook; queue API added for inspection/approval.
- Abuse/rate-limit e2e now runs under test host with correlation header assertions.

Completion
- ✅

Next Actions
- Replace the stub runner with real tool execution + resource limits; add webhook alert tests.
- Add a console view for the sandbox queue/approvals and consider persisting queue state.
