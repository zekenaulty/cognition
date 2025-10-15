Goal
- Introduce canonical scope primitives and path-aware hashing so downstream planners/tests can rely on a single representation without breaking legacy behavior.

Changes
- Scope primitives
  - Added `ScopePrincipal`, `ScopeSegment`, and `ScopePath` in `Cognition.Contracts` with canonical rendering + normalization helpers.
  - Extended `ScopeToken` with `ResolvePrincipal()` and `ToScopePath()` to build canonical paths from existing IDs.
- Hashing & feature flag
  - Created `ScopePathOptions` (`ScopePath.PathAwareHashingEnabled`) and wired DI binding in API startup.
  - Updated `RetrievalService` hashing to include canonical path when the flag is on, retaining legacy hashing otherwise.
- Tests
  - Added unit coverage for scope canonicalisation and the new path-aware hashing branch.
  - Updated retrieval tests to pass the new options dependency.

Commands Executed
- dotnet build

Files Changed
- plans/scope_token_path_refactor_step_20251009_01_canonical_schema_and_hash_flag.md (this log)
- src/Cognition.Contracts/ScopeToken.cs
- src/Cognition.Contracts/Scopes/ScopePrincipal.cs
- src/Cognition.Contracts/Scopes/ScopeSegment.cs
- src/Cognition.Contracts/Scopes/ScopePath.cs
- src/Cognition.Clients/Configuration/ScopePathOptions.cs
- src/Cognition.Clients/Retrieval/RetrievalService.cs
- src/Cognition.Api/Program.cs
- tests/Cognition.Clients.Tests/Retrieval/RetrievalServiceHelperTests.cs
- tests/Cognition.Clients.Tests/Retrieval/RetrievalServiceSearchTests.cs
- tests/Cognition.Clients.Tests/Retrieval/RetrievalServiceSearchTests_ConversationThenAgent.cs
- tests/Cognition.Clients.Tests/Tools/AgentRememberToolTests.cs
- tests/Cognition.Clients.Tests/Scopes/ScopePathTests.cs

Tests / Results
- dotnet build (warnings only: NU1603 across test projects due to newer Microsoft.NET.Test.Sdk resolved)

Next Actions
- Follow-on adoption now tracked under the dual-write step; no additional work required for this slice.

Completion
- DONE 2025-10-12
