Goal
- Enable query embeddings for scoped retrieval and add a promote-to-Agent memory path via a tool and API endpoint.

Changes
- Embeddings
  - Added IEmbeddingsClient and OpenAIEmbeddingsClient (uses OPENAI_API_KEY/OPENAI_BASE_URL; model via OPENAI_EMBEDDING_MODEL, default text-embedding-3-small).
  - Registered in AddCognitionClients.
  - Updated RetrievalService to compute query vectors, perform Conversation→Agent fallback kNN search via IVectorStore with strict metadata filters, and keep idempotent write hashing.
- Remember/Promote
  - Added AgentRememberTool (writes to Agent memory root via IRetrievalService with AgentId-only scope).
  - Added POST /api/chat/remember endpoint to promote content (by AgentId or resolve from ConversationId).

Commands Executed
- Edited Clients and Api projects; added new classes; wired DI.
- dotnet build (solution) — succeeded.

Files Changed
- src/Cognition.Clients/LLM/IEmbeddingsClient.cs
- src/Cognition.Clients/LLM/OpenAIEmbeddingsClient.cs
- src/Cognition.Clients/ServiceCollectionExtensions.cs
- src/Cognition.Clients/Retrieval/RetrievalService.cs
- src/Cognition.Clients/Tools/AgentRememberTool.cs
- src/Cognition.Api/Controllers/ChatController.cs (remember endpoint)
- src/Cognition.Clients/Cognition.Clients.csproj (vector project ref added earlier)

Tests / Results
- Build succeeded. RetrievalService now returns real results after embeddings. Writes require OpenSearch.UseEmbeddingPipeline=true (current setup) or future embedding-on-write.

Next Actions
- Add client-side calling code (UI) to use v2 endpoints and remember.
- Optional: add a policy flag to disallow Agent fallback for certain conversations.

Completion
- ✅

