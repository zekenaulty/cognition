Goal
- Broader UI agent adoption: replace persona-based flows with agent-centric ones, add Agents navigation and detail, switch new chat creation to AgentId, add scope chips, and add Remember button.

Changes
- Chat (client):
  - New conversation now maps personaId -> agentId via GET /api/agents and calls POST /api/conversations with { agentId }.
  - REST fallback sends to POST /api/chat/ask-chat (conversationId-only) and regenerate uses POST /api/chat/ask with AgentId.
  - Scope chips (Agent, conv short id) added next to conversation title.
  - “Remember this” button posts last assistant message to POST /api/chat/remember with { conversationId }.
  - Files: src/Cognition.Console/src/pages/ChatPage.tsx, src/Cognition.Console/src/components/chat/ChatLayout.tsx
- Agents UI:
  - New Agents page: /agents lists agents with linked Persona names and links to detail.
  - Agent detail page: /agents/:agentId shows Agent info + ToolBindings.
  - Nav link in PrimaryDrawer under admin.
  - Files: src/Cognition.Console/src/pages/AgentsPage.tsx, src/Cognition.Console/src/pages/AgentDetailPage.tsx, src/Cognition.Console/src/components/navigation/PrimaryDrawer.tsx, src/Cognition.Console/src/App.tsx
- API support:
  - AgentsController: added GET /api/agents/{id} returning agent + tool bindings (ScopeType=='Agent' & ScopeId match).

Notes
- PersonaPicker is not actively used in current Chat layout; selection remains implicit via route/left drawer. Future work: add Agent picker in chat header.
- Conversation left drawer remains persona-driven for now; future task to reorganize by agent.

Verification
- dotnet build (solution) OK.

Next
- Replace persona pickers with agent pickers across the console and routes.
- Add Agent detail expansions (model/safety profiles) and memory browsing.
- Rework conversation list filters to be agent-aware.

Plan Updates/Pivots
- Updated plan’s API Surface to fully agent-centric without -v2. DB wipe recorded.

Completion
- ✅

