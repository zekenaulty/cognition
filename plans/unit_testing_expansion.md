# Unit Testing Expansion

Objective
- Establish repeatable unit testing coverage across Cognition backend projects starting with the lowest-risk layers.
- Prioritize quick wins in the data layer before expanding to service, client, jobs, and API surfaces.
- Deliver a sustainable workflow (projects, fixtures, tooling) so future stories can add tests without friction.

Scope
- New test projects targeting `Cognition.Data.Relational`, `Cognition.Data.Vectors`, `Cognition.Clients`, `Cognition.Jobs`, and `Cognition.Api` infrastructure.
- Test harness utilities (DbContext factories, fake HTTP clients, stubs) required for unit coverage.
- Documentation inside `plans/` and step notes capturing progress and open questions.

Out of Scope
- UI/console end-to-end testing for `src/Cognition.Console`.
- Browser automation, load testing, or external dependency integration suites.
- Fiction plan management lifecycle (tracked separately; fiction Phase 001 remains in-flight and stays under `plans/fiction/`).

Deliverables
- `tests/` folder with per-project xUnit test assemblies added to `Cognition.sln`.
- Baseline coverage for data layer invariants, utilities, and helpers ("easy wins"), extended iteratively to higher-level components.
- Shared testing utilities (factory methods, test data builders, mocks) documented for reuse.
- Updated developer guidance on running tests (`dotnet test`, CI hook) once suites exist.

Strategy
- Phase 0 - Infrastructure & Conventions
  - Create `tests/Directory.Build.props` (if needed) to pin common analyzer/test settings.
  - Scaffold initial projects: `Cognition.Data.Relational.Tests`, `Cognition.Data.Vectors.Tests`, `Cognition.Clients.Tests`, `Cognition.Jobs.Tests`, `Cognition.Api.Tests` (net9.0, xUnit, FluentAssertions, NSubstitute or Moq).
  - Wire projects into `Cognition.sln` and ensure `dotnet test` succeeds with zero tests (guards pipeline).
  - Add base fixtures: in-memory EF Core context factory, stub `IHttpClientFactory`, helper for environment variables.
  - Add shared test doubles in `tests/Cognition.Testing` (`FakeClock`, `FakeTokenizer`, `ScriptedLLM`) to keep time, token, and LLM behaviours deterministic across suites.

- Phase 1 - Data & Utility Layer (quick wins)
  - Cover `ValidateBeforeSaveAsync` invariants in `src/Cognition.Data.Relational/CognitionDbContext.cs` (dimension mismatch, normalized vectors, duplicate detection with in-memory DB).
  - Validate default connection string selection in `src/Cognition.Data.Relational/ServiceCollectionExtensions.cs` via configuration stubs.
  - Unit test metadata builders re-used elsewhere:
    - `KnowledgeIndexingService.BuildMetadata` (`src/Cognition.Jobs/KnowledgeIndexingService.cs`).
    - `KnowledgeIndexController.BuildMetadata` (`src/Cognition.Api/Controllers/KnowledgeIndexController.cs`) using controller-level test double to ensure consistency.
  - Add guard/utility coverage: `src/Cognition.Data.Vectors/OpenSearch/Utils/Guard.cs`, `ScoreUtils.NormalizeCosine`.
  - Verify hashing helpers: `src/Cognition.Api/Infrastructure/PasswordHasher.cs` (hash + verify), `JwtTokenHelper` hash/secret fallback (resolve secret + `IssueAccessToken` claim set using deterministic secret). Extend coverage to include refresh rotation in `JwtTokenHelper.RotateRefreshAsync` (rotate token id, prevent reuse, respect expiry bump).
  - Test retrieval helpers in `src/Cognition.Clients/Retrieval/RetrievalService.cs` (`ToFilterDictionary`, `ResolveTenantKey`, `BuildScopeMetadata`, `ComputeContentHash`).

- Phase 2 - Vectors & Retrieval Integrations (moderate)
  - Cover `QueryDslBuilder.BuildKnnQuery` (including scoped agent/conversation filters) and `OpenSearchVectorStore.BuildDoc/ResolveIndex` behaviours with pure-object assertions.
  - Exercise `BulkHelper.IndexChunksAsync` chunking/pipeline logic using a fake `IOpenSearchClient` (verify chunk size, pipeline propagation).
  - Add tests for `RetrievalService.SearchAsync` success paths via faked `IEmbeddingsClient` and `IVectorStore` (conversation-first, agent fallback, dedupe logic).
    - Create in-memory vector store plus scripted embedding client to exercise `ScopeToken` order guarantees (`tests/Cognition.Clients.Tests/Retrieval/RetrievalServiceSearchTests_ConversationThenAgent.cs`).
    - Assert empty conversation scope falls back to agent root items without bleeding into sibling conversations.
  - Ensure `OpenSearchVectorStore.UpsertAsync` enforces dimension guard when pipeline disabled (mocked options) and surfaces error messages; add scoped filter assertions for `QueryDslBuilder.BuildKnnQuery` and `BuildFilters`.

- Phase 3 - Client & Tool Services (higher complexity)
  - Unit test `LLMClientFactory.CreateAsync` (`src/Cognition.Clients/LLM/LLMClientFactory.cs`) using in-memory DbContext and fake `IHttpClientFactory` to cover provider selection, credential resolution (env var), base URL overrides, and unsupported provider handling.
  - Cover `ToolRegistry` mapping behaviour and `ToolDispatcher` helper methods (`CoerceToType`, `FlattenValues`, `RedactValue`, `EnsureProviderModelArgs`) with targeted fixtures using lightweight seeded DbContext and fake DI container. Add scope propagation regression via `tests/Cognition.Clients.Tests/Tools/ToolDispatcherScopeTests.cs` to ensure `ScopeToken` is preserved end-to-end.
  - Verify `AgentRememberTool` promotes conversation content to agent root and deduplicates via content hash (`tests/Cognition.Clients.Tests/Tools/AgentRememberToolTests.cs`).
  - Ensure `OpenAIEmbeddingsClient` builds requests and parses responses correctly using `HttpClientFactoryStub` and shared tokenizer fakes (`tests/Cognition.Clients.Tests/LLM/OpenAIEmbeddingsClientTests.cs`).
  - Add focused tests for `AgentService` static helpers (`BuildSystemMessage`, `TryExtractJson`) and plan JSON parsing utilities without requiring live LLM calls.

- Phase 4 - Background Jobs & Messaging
  - Validate `PlanHandler.BuildPlanSteps/DescribeTool/MergeMetadata` sequences using deterministic `PlanRequested` inputs (`src/Cognition.Jobs/PlanHandler.cs`).
  - Add tests for `SignalRNotifier`, `PlanReadyHandler`, and other responders where logic is deterministic (mock `IHubContext` or `IBus`).
  - Exercise `KnowledgeIndexingService.IndexAllAsync` with stubbed DbContext plus vector store to ensure batching and logging thresholds.
  - Build deterministic `FictionWeaverJobs.RunVisionPlannerAsync` coverage using `ScriptedLLM`, in-memory DB, and notifier stubs to assert transcript rows and progress events (`tests/Cognition.Jobs.Tests/FictionWeaverJobs_VisionPlannerTests.cs`).

- Phase 5 - API Surface & Regression Nets (advanced)
  - Introduce minimal `WebApplicationFactory`-based tests for select controllers (for example `ClientProfilesController` force logic, `KnowledgeIndexController` response codes) using in-memory Postgres.
  - Add regression tests for JWT auth middleware (token rejection when dev secret used in Production config) and Hangfire dashboard filter.
  - Evaluate tracing or contract tests for SignalR hubs once lower layers are stable.
  - Add lightweight controller tests (for example `tests/Cognition.Api.Tests/Controllers/ChatControllerTests.cs`) to verify agent-centric inputs: `ask` requires `AgentId`, `ask-chat` accepts conversation-only scope, and both stamp scope metadata correctly.

Shared Testing Fakes (add once)
- `tests/Cognition.Testing/Time/FakeClock.cs`: `IClock` implementation with mutable `Now` for deterministic expiry checks.
- `tests/Cognition.Testing/Tokens/FakeTokenizer.cs`: simple token counter to mimic planner budget checks.
- `tests/Cognition.Testing/LLM/ScriptedLLM.cs`: rule-based `ILLMClient` returning canned `LLMResponse` instances to drive planners, tools, and job flows without external calls.

High-Value Tests To Author Now (cognition.20251007_232521)
- `tests/Cognition.Clients.Tests/Retrieval/RetrievalServiceSearchTests_ConversationThenAgent.cs`: conversation-first search order with agent fallback only when needed.
- `tests/Cognition.Clients.Tests/Tools/AgentRememberToolTests.cs`: promotion to agent scope and idempotent writes via content hash.
- `tests/Cognition.Clients.Tests/LLM/OpenAIEmbeddingsClientTests.cs`: request building, auth headers, vector length parsing through `HttpClientFactoryStub`.
- `tests/Cognition.Api.Tests/Controllers/ChatControllerTests.cs`: agent-centric DTO stamping for `ask` and `ask-chat` flows.
- `tests/Cognition.Clients.Tests/Tools/ToolDispatcherScopeTests.cs`: ensure every tool invocation preserves the original `ScopeToken`.
- `tests/Cognition.Jobs.Tests/FictionWeaverJobs_VisionPlannerTests.cs`: deterministic transcript and progress events from `FictionWeaverJobs`.
- `tests/Cognition.Api.Tests/Security/JwtTokenHelperRotateRefreshTests.cs`: rotation success, reuse rejection, and expiry updates for refresh tokens.

## Current Status (2025-10-12)
- Test project scaffolding remains in place (API, Clients, Data Relational/Vectors, Jobs) and builds via `dotnet test`.
- Shared DbContext, HTTP, and environment fixtures exist; new time, token, and LLM fakes are queued to round out deterministic harnesses.
- Phase 1 coverage hits DbContext invariants and password hashing; JWT rotation scenarios are now covered.
- Retrieval service helper methods are tested; full `SearchAsync` conversation -> agent ordering remains outstanding.
- Vector DSL coverage now includes scoped filter assertions; `OpenSearchVectorStore.UpsertAsync` dimension guards still need tests.
- API and Jobs suites are scaffolded but awaiting first controller and job regression coverage.

### Recent Progress (2025-10-12)
- Extended `QueryDslBuilder.BuildKnnQuery` tests to assert scope path/principal/segment filters.
- Added dispatcher telemetry tests covering planner execution success/failure and capability discovery.
- Validated Vision planner execution through new base class with scripted fakes to keep regression harness deterministic.

### Upcoming Focus (High Priority)
1. Add `OpenSearchVectorStore.UpsertAsync` dimension guard and scope metadata assertions.
2. Stand up `ChatController` ask/ask-chat tests to enforce agent-centric routing and `ScopeToken` creation.
3. Capture FictionWeaver job transcript telemetry tests once planner sinks are available.

## Next Steps
1. Draft `OpenSearchVectorStore.UpsertAsync` guard tests (dimension mismatch, null embeddings) on the scripted vector stack.
2. Prepare controller and job regression harnesses (ChatController, FictionWeaverJobs) once shared fakes are in place.
3. Align planner telemetry storage tests with forthcoming sinks to keep tool dispatcher coverage comprehensive.

Tooling & Conventions
- Testing stack: xUnit, FluentAssertions, NSubstitute (or Moq if preferred), AutoFixture for data generation.
- Use EF Core `Sqlite` in-memory mode for DbContext tests to exercise relational behaviours (unique constraints, concurrency).
- Provide `TestEnvGuard` helper to set and restore environment variables when invoking code that reads from `Environment`.
- Adopt `Given_When_Then` naming or similar for clarity; keep tests fast and deterministic.
- `dotnet test` remains the canonical entry point; extend CI (`scripts/` or pipeline) once suites land.

Worklog Protocol
- Create step notes under `plans/unit_testing_expansion_step_YYYYMMDD_HHMM_<slug>.md` for each distinct activity.
- Capture commands, files touched, test output, and follow-ups per existing template in `plans/README.md`.
- Use `_scratchpad.md` if quick TODOs arise between formal steps.

Risks & Mitigations
- Large DbContext tests may become slow -> keep fixtures lean and reset per test class.
- OpenSearch dependencies require fakes -> ensure abstractions allow tests without network calls.
- Retrieval scope bleed is easy to miss -> enforce via new conversation and agent tests and helper assertions.
- Credential and environment variable manipulation must restore global state to avoid cross-test pollution.
- Some legacy code (for example `AgentService`) is lengthy -> start with helper coverage before tackling full method flows.

Checklist
- [x] Establish test project scaffolding and solution wiring (completed 2025-10-05).
- [ ] Land Phase 1 data & utility tests with CI docs.
- [ ] Complete Phase 2 vector/retrieval coverage.
- [ ] Ship Phase 3 client/tool service tests.
- [ ] Implement Phase 4 job/messaging tests.
- [ ] Add Phase 5 API regression nets or document rationale if deferred.
- [ ] Update README/Developer docs with testing instructions.

## Immediate Next Tasks (2025-10-21)
- Bake the builder-backed scope tests into CI (retrieval + tool dispatcher suites) and add documentation snippets summarizing the canonical ScopePath patterns for future contributors.
- Expand vector-query coverage in `tests/Cognition.Data.Vectors.Tests/` to assert canonical path filters across `QueryDslBuilder` + OpenSearch vector store helpers before enabling the flags broader.
- Set deterministic fakes (ScriptedLLM, ScriptedEmbeddingsClient, InMemoryVectorStore) as defaults via `tests/Directory.Build.props` so planner/unit suites run without live dependencies.
- Capture ongoing regression gaps from `FictionPlannerPipelineTests` and add targeted planner/jobbing tests around backlog telemetry + diagnostics endpoints once implemented.





