Goal
- Store scope principal/path metadata alongside legacy fields and surface diagnostics/backfill tooling so we can flip dual write safely.

Changes
- Persistence & models
  - Added `ScopePrincipalId/Type`, `ScopePath`, and `ScopeSegments` to `KnowledgeEmbedding` plus EF mappings and index.
  - Extended `VectorItem` + OpenSearch mappings to emit canonical path fields, and updated the vector store serializer/deserializer.
- Retrieval service
  - Introduced `ScopePathOptions.DualWriteEnabled` and `IScopePathDiagnostics`; when enabled, writes now capture canonical path metadata and record telemetry.
  - Metadata now carries `ScopePath`/`ScopePrincipal*` keys so relational/vector layers stay in sync during the dual-write window.
- Diagnostics & backfill
  - Added `ScopePathDiagnostics` counters and a new `/api/diagnostics/scope` endpoint exposing flag state + metrics.
  - Implemented `ScopePathBackfillService` with a manual trigger to populate missing path fields from existing metadata.
- Tests
  - Expanded retrieval tests for the new constructor dependency.
  - Added coverage ensuring path-aware hashing input and dual-write metadata populate as expected.

Commands Executed
- dotnet build

Files Changed
- plans/scope_token_path_refactor_step_20251009_02_dual_write_persistence_and_backfill_runner.md (this log)
- src/Cognition.Clients/Configuration/ScopePathOptions.cs
- src/Cognition.Clients/Scope/ScopePathDiagnostics.cs
- src/Cognition.Clients/ServiceCollectionExtensions.cs
- src/Cognition.Clients/Retrieval/RetrievalService.cs
- src/Cognition.Contracts/ScopeToken.cs
- src/Cognition.Data.Relational/Modules/Knowledge/KnowledgeEmbedding.cs
- src/Cognition.Data.Relational/Modules/Knowledge/Config.cs
- src/Cognition.Data.Vectors/OpenSearch/Models/VectorItem.cs
- src/Cognition.Data.Vectors/OpenSearch/Provisioning/Mappings/VectorIndexMappingProvider.cs
- src/Cognition.Data.Vectors/OpenSearch/Store/OpenSearchVectorStore.cs
- src/Cognition.Api/Controllers/ScopeDiagnosticsController.cs
- src/Cognition.Api/Infrastructure/ScopePath/ScopePathBackfillService.cs
- src/Cognition.Api/Program.cs
- tests/Cognition.Clients.Tests/Retrieval/RetrievalServiceHelperTests.cs
- tests/Cognition.Clients.Tests/Retrieval/RetrievalServiceSearchTests.cs
- tests/Cognition.Clients.Tests/Retrieval/RetrievalServiceSearchTests_ConversationThenAgent.cs
- tests/Cognition.Clients.Tests/Tools/AgentRememberToolTests.cs
- tests/Cognition.Clients.Tests/Scopes/ScopePathTests.cs
- tests/Cognition.Testing/Retrieval/InMemoryVectorStore.cs

Tests / Results
- dotnet build (warnings only: NU1603 across test projects due to newer Microsoft.NET.Test.Sdk being resolved)

Next Actions
- Apply `20251012031808_ScopePathDualWrite` in each environment; re-provision the vector index so `scope*` fields exist before enabling dual write.
- Update indexing/jobs (already tracked) to emit scope metadata natively; once deployed, run the `/api/diagnostics/scope/backfill` job in lower envs and verify counters.
- After dual-write soak, decide on the timeline for toggling `ScopePath.PathAwareHashingEnabled` (see step 08).

Completion
- DONE 2025-10-12
