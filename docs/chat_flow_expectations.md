# Chat Flow Expectations (Pre‑alpha stabilization draft)

This document captures the intended end‑to‑end chat behavior so we can compare implementation to expectations and close gaps quickly.

## Goals
- Agent‑first routing: the URL and sidebar drive which agent is active. No hidden local overrides.
- Deterministic conversation selection: route chooses conversation; otherwise “no selection” until user sends or picks one.
- Stable UI state: no message drops/flicker when switching agents or conversations.
- Provider/model persistence: stored per conversation; defaults resolved once per new conversation from server/agent/global defaults.

## Expected Behaviors
1) **Navigation**
   - Sidebar “New chat” for agent *A* navigates to `/chat/{agentId}` (no conversation id).
   - Sidebar conversation click navigates to `/chat/{agentId}/{conversationId}` for that agent.
   - URL is the source of truth for agent/conversation; navigating away/into routes should not reuse stale in‑memory IDs.

2) **Agent selection**
   - There is no agent picker inside the chat page. Agent is set only by route/sidebar.
   - When the route agent changes, clear in‑memory conversation/messages unless a route conversation id is present.

3) **Conversation selection**
   - When route includes `conversationId`, load that conversation’s messages and metadata; do not clear them mid‑load.
   - When route omits `conversationId`, do not auto‑select; remain in “new chat” until user sends first message.
   - Switching agents clears any prior conversation selection and messages (unless a conversation id is in the route).

4) **Message load & rendering**
   - On selecting a conversation, fetch messages (and images) and replace the message list once data arrives; do not append stale pending data from another conversation.
   - No intermediate clearing that permanently drops already‑fetched messages; only clear on deliberate agent/conversation change.

5) **Sending a message**
   - If no conversation exists, POST `/api/conversations` with the current route agent, then join hub, then send.
   - Use the conversation’s stored provider/model if present; otherwise resolve defaults (agent profile → global defaults → heuristic fallback).
   - Persist provider/model to the conversation immediately after creation/change.

6) **Provider/Model display**
   - Header/menu should always reflect the conversation’s stored provider/model; if absent, show the resolved defaults for this session.
   - Changing provider/model in a conversation updates it server‑side and is sticky across reloads for that conversation.

7) **Hub behavior**
   - Join the hub only for the selected conversation; leave when switching or when no conversation is selected.
   - Handle hub disconnect gracefully without clearing messages.

8) **State persistence**
   - Do not persist `chat.agentId` or `chat.conversationId` in local storage.
   - It is acceptable to persist non‑authoritative UX prefs (e.g., image style) separately.

## Edge Cases
- Navigating directly to an invalid conversation id should redirect to home or clear selection without crashes.
- Concurrent tabs: each tab follows its own route; no cross‑tab local storage coupling for agent/conversation.
- Rate‑limited or failed send should leave the user message visible with an error marker; should not clear the conversation.

## Verification Checklist
- New chat from sidebar shows correct agent in header, no flicker, route updates to `/chat/{agentId}`.
- Selecting an existing conversation loads its messages and keeps the header agent fixed to that conversation’s agent.
- Switching agents while on a conversation without route conversation id clears messages and selection.
- Provider/model in header matches conversation settings after reload.
- No messages disappear when loading an existing conversation; no duplication across agent switches.
