Goal
- Scaffold ScopeToken and IRetrievalService, wire DI, and ensure ToolDispatcher propagates scope through ToolContext.

Commands Executed
- Added record struct ScopeToken in Contracts
- Added IRetrievalService + NoopRetrievalService in Clients
- Registered NoopRetrievalService in API Program.cs DI
- Updated AgentService to construct ToolContext with AgentId + ConversationId
- dotnet build (Data/Clients succeed; full solution blocked by file locks in Api)

Files Changed
- src/Cognition.Contracts/ScopeToken.cs
- src/Cognition.Clients/Retrieval/IRetrievalService.cs
- src/Cognition.Clients/Retrieval/NoopRetrievalService.cs
- src/Cognition.Api/Program.cs
- src/Cognition.Clients/Agents/AgentService.cs

Tests / Results
- Cognition.Clients builds successfully. API build blocked by file locks (running processes), unrelated to code changes.

Next Actions
- Replace NoopRetrievalService with real implementation using vector store; stamp scope metadata on writes.
- Begin API DTO updates to accept agentId/conversationId and add deprecation shim for personaId.

Completion
- ✅

