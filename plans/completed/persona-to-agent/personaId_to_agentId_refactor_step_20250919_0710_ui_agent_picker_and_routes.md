Goal
- Replace persona picker with agent picker in chat header; update routes and state to use agentId for new chats. Add agent-aware conversation list filter server-side.

Changes
- API
  - ConversationsController.List now accepts optional `agentId` query param to filter by conversation AgentId.
- UI Chat
  - ChatMenu: added Agent submenu (lists agents, allows selection).
  - ChatLayout: accepts and passes agents/agentId/onAgentChange to ChatMenu.
  - ChatPage: fetches agents, tracks agentId state; maps personaId<->agentId; new chats post /api/conversations with { agentId }.
  - Kept persona-based rendering for fromName temporarily; agent selection syncs personaId for compatibility.
- UI Navigation (agents)
  - Agents page and detail wired earlier remain available from drawer.

Notes
- Left drawer is still persona-organized; next step will add agent-organized drawers using `?agentId=` filter and scope chips.

Build
- Solution builds clean.

Next
- Rework left drawer to agent grouping with scope chips.
- Expand Agent detail with client profile switching (uses PATCH /api/agents/{id}/client-profile) and profile lists from ClientProfilesController.

Completion
- ✅

