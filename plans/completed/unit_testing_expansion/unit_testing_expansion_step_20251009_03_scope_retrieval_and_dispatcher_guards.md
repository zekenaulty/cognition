Goal
- Lock in scope-aware behaviour with regression tests for canonical paths, retrieval fallback, and tool dispatcher propagation.

Changes
- Retrieval helpers
  - Extended `RetrievalServiceHelperTests` with path-aware hashing expectations.
  - Hardened `RetrievalService` search tests and added an `AgentRememberTool` dual-write assertion.
- Tool dispatcher
  - Added a dispatcher test harness (`RecordingRetrievalService`) to ensure scope context reaches dependent tools without mutation.
- Scope primitives
  - Introduced unit coverage for `ScopeToken.ToScopePath` principal selection and segment normalization.

Commands Executed
- dotnet build

Files Changed
- plans/unit_testing_expansion_step_20251009_03_scope_retrieval_and_dispatcher_guards.md (this log)
- tests/Cognition.Clients.Tests/Retrieval/RetrievalServiceHelperTests.cs
- tests/Cognition.Clients.Tests/Retrieval/RetrievalServiceSearchTests.cs
- tests/Cognition.Clients.Tests/Retrieval/RetrievalServiceSearchTests_ConversationThenAgent.cs
- tests/Cognition.Clients.Tests/Tools/AgentRememberToolTests.cs
- tests/Cognition.Clients.Tests/Tools/ToolDispatcherScopeTests.cs
- tests/Cognition.Clients.Tests/Scopes/ScopePathTests.cs
- tests/Cognition.Testing/Retrieval/InMemoryVectorStore.cs

Tests / Results
- dotnet build (warnings only: NU1603 for Microsoft.NET.Test.Sdk version drift)

Next Actions
- Add OpenSearch guard tests (step 07) to cover scoped filters and KNN dimensions with path metadata present.
- Once planner telemetry lands, extend dispatcher tests to assert redaction + scoped logging.

Completion
- DONE 2025-10-12
