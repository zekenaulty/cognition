Goal
- Add initial sandbox policy enforcement at the dispatcher layer (configurable) and cover with tests as a first concrete step toward the security closeout plan.

Context
- Plan: `plans/alpha_security_sandbox_closeout.md`.
- We need a guardrail so tools/planners no longer run unsandboxed silently; introduce policy options and enforcement before building the full OOPS lane.

Commands Executed
- Get-Date -Format "yyyyMMdd_HHmm"
- dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "ToolDispatcher|Sandbox"

Files Changed
- src/Cognition.Clients/Tools/Sandbox/SandboxMode.cs
- src/Cognition.Clients/Tools/Sandbox/ToolSandboxOptions.cs
- src/Cognition.Clients/Tools/Sandbox/SandboxDecision.cs
- src/Cognition.Clients/Tools/Sandbox/ISandboxPolicyEvaluator.cs
- src/Cognition.Clients/Tools/Sandbox/SandboxPolicyEvaluator.cs
- src/Cognition.Clients/Tools/ToolDispatcher.cs
- src/Cognition.Clients/ServiceCollectionExtensions.cs
- tests/Cognition.Clients.Tests/Tools/ToolDispatcherScopeTests.cs
- tests/Cognition.Clients.Tests/Tools/SandboxPolicyEvaluatorTests.cs

Tests / Results
- dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "ToolDispatcher|Sandbox" (pass)

Issues
- None; added a local OptionsMonitor stub for tests.

Decision
- Landed configurable sandbox policy evaluation (Disabled/Audit/Enforce + allowlists) and wired ToolDispatcher to deny/allow with audit logging. Default options registered; enforcement will start once configured.

Completion
- ✅

Next Actions
- Wire abuse-header/rate-limit e2e and correlation propagation tests (plan item).
- Add sandbox policy telemetry and surface violations in Ops alerts.
- Start scaffolding the OOPS lane/approval jobs per spec (worker/queue).
