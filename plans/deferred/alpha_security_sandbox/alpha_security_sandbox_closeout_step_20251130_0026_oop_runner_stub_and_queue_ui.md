Goal
- Push sandbox toward enforceable OOP execution, surface queue inspection, and add webhook alert coverage.

Context
- Plan: `plans/alpha_security_sandbox_closeout.md`.
- OOP runner is still a stub process but now invoked in enforce mode; queue has admin API and console view.

Commands Executed
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "AbuseHeadersAndRateLimitE2ETests"
- dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "Sandbox"

Files Changed
- src/Cognition.Clients/Tools/Sandbox/ProcessSandboxWorker.cs (OOP runner stub executes external process with Job Object on Windows)
- src/Cognition.Clients/Tools/ToolDispatcher.cs (enforce mode routes to worker; enqueue-on-deny preserved)
- src/Cognition.Clients/Tools/Sandbox/IToolSandboxApprovalQueue.cs + InMemorySandboxApprovalQueue.cs (dequeue/snapshot)
- src/Cognition.Clients/Tools/Sandbox/LoggerSandboxAlertPublisher.cs (webhook alert test target)
- src/Cognition.Clients/Tools/Sandbox/ToolSandboxOptions.cs (EnqueueOnDeny)
- src/Cognition.Clients/ServiceCollectionExtensions.cs (register ProcessSandboxWorker)
- tests/Cognition.Clients.Tests/Tools/SandboxAlertPublisherTests.cs (webhook POST coverage)
- src/Cognition.Api/Controllers/SandboxController.cs (admin API queue/approve)
- src/Cognition.Console/src/pages/AdminSandboxQueuePage.tsx (console view for queue)

Tests / Results
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "AbuseHeadersAndRateLimitE2ETests" (pass)
- dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "Sandbox" (pass; existing nullable warnings remain)

Issues
- Sandbox worker still stub (runs trivial process; not executing tools yet).
- Queue remains in-memory; console view added but not linked into navigation.
- Nullable warnings in dispatcher untouched.

Decision
- Dispatcher now invokes OOP worker when sandbox mode is Enforce; denied requests can enqueue; webhook alerts covered by test.
- Admin API + console page to inspect/approve in-memory queue.

Completion
- ✅

Next Actions
- Replace stub runner with real tool execution + resource limits; wire queue view into console nav; consider persistence.
