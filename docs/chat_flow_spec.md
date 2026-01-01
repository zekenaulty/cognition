# Chat Flow – Expected End-to-End Behavior (Detailed)

This is the step-by-step reference for how chat must behave in the UI and API. Use it to validate fixes and to spot regressions (messages not showing, names not set, sidebar not updating, etc.).

## 0) Entry + Navigation
1. Sidebar “New chat” for agent **A** navigates to `/chat/{agentId}` (no conversation id).
2. Sidebar existing conversation click navigates to `/chat/{agentId}/{conversationId}` (agent from that conversation).
3. The URL is authoritative: agent = route agent; conversation = route conversation id (if present).
4. When navigating to a different agent, in-memory selection resets unless a conversation id is provided.

## 1) Initial Load (No conversation selected)
1. Page shows “New Chat” title, agent label from route, provider/model defaults resolved (agent profile -> global defaults -> heuristic).
2. No messages are loaded. Input is enabled. “New conversation on first send” hint is shown.

## 2) Opening an Existing Conversation
1. Route includes conversation id. Client fetches conversation + messages (and images).
2. Message list is fully populated (user + assistant + versions + images) in chronological order.
3. Header shows the conversation title (or “New Chat” if null), agent from the conversation, and provider/model stored on that conversation.
4. Hub joins that conversation; subsequent deltas/messages append in real time.
5. Sidebar reflects the selection (accordion expanded, conversation highlighted).

## 3) Starting a New Conversation (first send)
1. User types a message and presses send while no conversation id exists.
2. Client creates conversation via POST `/api/conversations` with current route agent and participant persona.
3. Server responds with `conversationId`; client:
   - Joins hub for that conversation.
   - Stores provider/model for the conversation (resolved defaults if none chosen).
   - Adds the new conversation to in-memory list and updates the header to “New Chat” (until the server titles it).
   - Pushes route to `/chat/{agentId}/{conversationId}`.
4. The user message is shown immediately; the assistant placeholder appears; when the assistant reply arrives, it replaces the placeholder.
5. Server-side titling (if present) updates the conversation title; header/sidebar reflect it after refresh/hub event.

## 4) Sending Messages in an Existing Conversation
1. User message is appended immediately to the list.
2. Assistant reply arrives via hub (deltas then final) or REST fallback; list updates in place.
3. No messages disappear when switching agent/provider/model; only an explicit agent/conversation change clears the list.

## 5) Provider / Model Behavior
1. Each conversation stores providerId/modelId; on load, header/menu show these.
2. Changing provider/model in a conversation persists it (PATCH) and is sticky across reloads.
3. If missing, defaults resolve once per conversation: agent profile -> global defaults -> heuristic (e.g., Gemini/Flash).
4. Header always reflects the effective provider/model for the active conversation.

## 6) Sidebar Expectations
1. Agents list expands to show conversations for that agent.
2. Clicking a conversation navigates and selects it; clicking trash deletes and removes it from the list.
3. Recent list mirrors latest conversations; selecting one navigates with its agent id.
4. After a new conversation is created, it appears under its agent and in “Recent.”

## 7) Naming / Titles
1. New conversations start as “New Chat.”
2. After the first assistant reply, the server may assign a title; header and sidebar update to that title.
3. Titles persist across reloads.

## 8) State Rules / No-Stale Behavior
1. Do NOT persist agentId or conversationId in local storage; only the route controls selection.
2. On agent change without a route conversation id: clear selection and messages.
3. On route conversation id change: load that conversation; do not clear mid-load.
4. No flicker: agent in header stays the route agent; conversation stays the route conversation.

## 9) Error Handling
1. If conversation fetch fails: show an error and/or redirect to home; do not leave partial state.
2. If hub is down: sending falls back to REST; UI still shows user message and eventual reply if available; connection badge shows degraded status.
3. Rate-limit or send failure: user message remains with an error marker; no silent drops.

## 10) Images
1. Images tied to a conversation appear in the message list with correct ordering by timestamp.
2. Image generation uses the conversation’s provider/model (or overrides) and appends image messages when available.

## 11) Real-Time Events (Hub/Bus)
- Subscriptions (ChatHub events currently handled in UI):
  - `assistant-delta` → append streaming delta to pending assistant message.
  - `assistant-message` → finalize assistant reply (id/content/versions).
  - `assistant-version-appended` / `assistant-version-activated` → update versions of a specific message.
  - `plan-ready` → add planner steps to plan timeline.
  - `tool-requested` / `tool-completed` → add tool actions to tool trace.
  - `conversation-created` / `conversation-joined` → set active conversation id (should also add to list).
  - `conversation-updated` → update title in list/header.
  - `connection-state` → update badge.
- Expected sidebar/list updates:
  - When `conversation-created` fires, new conversation is added to in-memory list and reflects in sidebar “Recent” and the agent’s accordion (after fetch or push).
  - When `conversation-updated` (title) or provider/model changes are saved, the current conversation entry is updated so the header/sidebar reflect the change.
- Bus-triggered refreshes (server events surfaced to UI via hub):
  - Planner/tool events (`plan-ready`, `tool-*`) should not reset conversations; only append metadata.
- Not currently handled but desired:
  - Push “conversation deleted” to prune lists without a full refetch.
  - Push “image created” to append image messages without polling.

## 12) Minimal Regression Checklist
- Navigate to `/chat/{agent}` → shows “New Chat,” correct agent, defaults, empty messages.
- Create new conversation → route updates to `/chat/{agent}/{conversationId}`; user + assistant messages appear; title updates after server naming.
- Open existing conversation → messages load fully; header shows correct agent and provider/model; sidebar highlights it.
- Switch agents via sidebar new-chat → header agent switches, messages cleared, route updates; no flicker or reversion to previous agent.
- Change provider/model → persists and survives refresh.
- Sidebar “Recent” and agent accordions update when new conversations are added.
