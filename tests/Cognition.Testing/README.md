# Cognition.Testing

Shared fixtures and fakes consumed by Cognition test projects.

## Available helpers
- `EntityFramework/TestDbContextFactory` builds EF Core contexts backed by SQLite or InMemory providers.
- `Http/HttpClientFactoryStub` returns canned `HttpClient` instances for transport-level tests.
- `Utilities/EnvironmentVariableScope` safely scopes environment variable overrides.
- `Time/FakeClock` implements `Cognition.Contracts.Time.IClock` with a mutable `Now` and `Advance` helper for deterministic expiry checks.
- `Tokens/FakeTokenizer` implements `Cognition.Contracts.Tokens.ITokenizer` and estimates token counts by chunking text length into four-character windows (always at least one token).
- `LLM/ScriptedLLM` implements `Cognition.Clients.LLM.ILLMClient`, letting tests register `When...` clauses to return canned strings or streams for `Generate*` and `Chat*` calls, with fallback defaults of "noop" and an empty stream.
- `LLM/ScriptedEmbeddingsClient` implements `Cognition.Clients.LLM.IEmbeddingsClient` with `When` rules so tests can supply deterministic vectors (defaults to `[1f]`).
- `Retrieval/InMemoryVectorStore` is a lightweight `IVectorStore` implementation that applies metadata filters and returns deterministic clones for retrieval tests.

These helpers live in a dedicated project so other test assemblies can add a single `ProjectReference` and reuse them without copy/paste.
