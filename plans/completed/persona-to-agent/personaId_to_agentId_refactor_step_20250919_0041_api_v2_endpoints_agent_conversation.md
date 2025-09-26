Goal
- Introduce v2 API endpoints that accept agentId (instead of personaId) and conversationId-only chat to align with agent-bound conversations.

Changes
- ChatController:
  - Added AskV2Request and AskWithToolsV2 endpoints (`/api/chat/ask-v2`, `/api/chat/ask-with-tools-v2`) that resolve personaId from agentId.
  - Added ChatV2Request endpoint (`/api/chat/ask-chat-v2`) that accepts `conversationId` only and resolves the agent’s persona.
- ConversationsController:
  - Added CreateConversationV2Request with `AgentId` and endpoint `/api/conversations/v2` to create conversations bound to the agent.

Notes
- v1 endpoints remain for back-compat; v2 endpoints align to the new agent-centric flow.
- Controllers currently stamp FromAgentId on message writes to satisfy DB non-null constraints and scope.

Commands Executed
- Edited ChatController.cs and ConversationsController.cs

Files Changed
- src/Cognition.Api/Controllers/ChatController.cs
- src/Cognition.Api/Controllers/ConversationsController.cs

Tests / Results
- Build not run for Api due to file locks; Data/Clients compile.

Next Actions
- Update UI and clients to call v2 endpoints.
- Begin deprecation of personaId-based routes (log warnings/headers).
- Continue with RetrievalService implementation and scope stamping in vectors.

Completion
- ✅ (code in place; verify by building Api after closing running processes)

