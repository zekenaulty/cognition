Goal
- Enforce agent-centric API, wipe conversation data (dev), and remove v1/-v2 routes.

Actions
- DB wipe: added migration 20250919061255_WipeConversations with TRUNCATE for conversation tables; applied via `dotnet ef database update`.
- API controllers:
  - ChatController: unified routes (ask, ask-with-tools, ask-chat) to new DTOs (AgentId for ask/ask-with-tools; ConversationId only for ask-chat). Removed v1 deprecations and -v2 routes.
  - ConversationsController: POST /api/conversations now accepts AgentId; removed /v2 route.
- UI console:
  - New Agents page scaffold at /agents and navigation link.
  - Chat fallback switched to /api/chat/ask-chat (new semantics).
  - On new conversation, map personaId→agentId via GET /api/agents and POST /api/conversations with AgentId.
  - Regenerate uses /api/chat/ask with AgentId (resolved via mapping).
  - Scope chips and “Remember this” button present in chat header.

Commands Executed
- dotnet ef migrations add WipeConversations -p src/Cognition.Data.Relational
- Edited migration to TRUNCATE conversation tables; fixed namespace/location
- dotnet ef database update -p src/Cognition.Data.Relational
- API/Console code changes; dotnet build (solution) OK

Files Changed (high-level)
- src/Cognition.Data.Relational/Migrations/20250919061255_WipeConversations.*
- src/Cognition.Api/Controllers/ChatController.cs
- src/Cognition.Api/Controllers/ConversationsController.cs
- src/Cognition.Console/src/pages/ChatPage.tsx
- src/Cognition.Console/src/components/chat/ChatLayout.tsx
- src/Cognition.Console/src/pages/AgentsPage.tsx
- src/Cognition.Console/src/components/navigation/PrimaryDrawer.tsx
- plans/personaId_to_agentId_refactor.md (plan updated)

Completion
- ✅

Next
- Replace persona pickers with agent pickers across the console.
- Use /api/conversations (AgentId) everywhere for new chats.
- Add Agent detail view with tool/model/safety and memory browsing.
- Remove any remaining v1 client calls and legacy DTOs.

