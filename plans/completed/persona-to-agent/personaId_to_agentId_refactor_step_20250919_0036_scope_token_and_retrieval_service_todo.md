Goal
- Introduce ScopeToken and IRetrievalService to enforce read/write scoping: default reads Conversation → fallback Agent; writes default to Conversation; promote to Agent on explicit remember.

TODOs
- [ ] Define `ScopeToken` in `Cognition.Contracts` with fields: tenantId?, appId?, personaId?, agentId?, conversationId?, projectId?, worldId? (nullable, immutable record).
- [ ] Add `IRetrievalService` interface in `Cognition.Clients` (or `Cognition.Contracts` if shared):
  - [ ] Methods: `GetContextAsync(ScopeToken)`, `SearchAsync(ScopeToken, query, k, filters?)`, `WriteAsync(ScopeToken, content, metadata)`.
  - [ ] Enforce: read filter chain ConversationId → AgentId; write default ConversationId; optional promote to AgentId.
- [ ] Update `ToolDispatcher` to require `ScopeToken` (or derive from current `ToolContext` fields) and pass to tools consistently.
- [ ] Stamp full scope metadata on embedding writes (TenantId, AppId, AgentId, ConversationId, etc.).
- [ ] Add unit tests for retrieval ordering and isolation.

Commands (to be adapted)
- dotnet new interface/records as needed
- dotnet build

Files Expected to Change
- src/Cognition.Contracts (new ScopeToken)
- src/Cognition.Clients/Tools (ToolDispatcher signature enforcement)
- src/Cognition.Clients/* (where retrieval is used)

Completion
- ❌

