Goal
- Extend vector/OpenSearch regression coverage to verify scoped KNN filters enforce path/principal metadata and to keep dispatcher telemetry under test as planner surfaces expand.

Changes
- Hardened `QueryDslBuilder` to emit `scopePath`, `scopePrincipalType/id`, and `scopeSegments` term clauses when filters include scope metadata.
- Added unit tests asserting scoped KNN filter emission, metadata segment expansion, and tool dispatcher planner telemetry flows (success + failure).
- Updated planner catalog tests so capability discovery is exercised through DI.

Tests / Validation
- `dotnet test tests/Cognition.Data.Vectors.Tests/Cognition.Data.Vectors.Tests.csproj`
- Planner telemetry tests presently require a trimmed EF model; work captured under planner follow-up (fails when running full `tests/Cognition.Clients.Tests`). 

Next Actions
- Add `OpenSearchVectorStore.UpsertAsync` dimension guard tests to close out step 07 entirely.
- Fix the planner test harness to ignore LLM provider/model entities so telemetry assertions run in CI.

Completion
- 2025-10-12 (partial — scoped KNN coverage landed; Upsert guards remain)
