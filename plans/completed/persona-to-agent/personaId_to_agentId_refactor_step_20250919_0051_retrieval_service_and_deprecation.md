Goal
- Implement a RetrievalService backed by the vector store with strict scope enforcement; start deprecating personaId-based API endpoints and document v2 usage.

Changes
- Retrieval service implementation:
  - Added `src/Cognition.Clients/Retrieval/RetrievalService.cs` using `IVectorStore` (OpenSearch) with metadata stamping and idempotent ContentHash.
  - Enforces default write to Conversation scope; supports Agent root when ConversationId absent.
  - Search currently returns empty (guard) until an embedding provider is wired; scope filters are applied to prevent bleed.
  - Registered the service in DI, replacing Noop.
- Project references:
  - `Cognition.Clients` now references `Cognition.Data.Vectors`.
- API v1 deprecation headers:
  - ChatController: `ask`, `ask-with-tools`, `ask-chat`, `ask-with-plan` set `Deprecation`, `Sunset`, and `Link` to v2.
- Docs:
  - README updated with API v2 (agent-centric) endpoints and deprecation note.

Commands Executed
- Edited Program.cs to register RetrievalService
- Added new classes and updated project references
- Updated ChatController and README
- dotnet build (solution) — succeeded

Files Changed
- src/Cognition.Clients/Retrieval/RetrievalService.cs (new)
- src/Cognition.Clients/Cognition.Clients.csproj
- src/Cognition.Api/Program.cs
- src/Cognition.Api/Controllers/ChatController.cs
- README.md

Tests / Results
- Build succeeded across solution.
- RetrievalService writes require OpenSearch.UseEmbeddingPipeline=true until an embedding provider is added.

Next Actions
- Wire an embedding provider for query vectors (OpenAI embeddings or model-native), then enable SimilaritySearchAsync with Conversation → Agent fallback filters.
- Add a MemoryWrite/Remember tool to promote conversation-level items to Agent root scope.
- Update UI to call v2 endpoints.

Completion
- ✅

