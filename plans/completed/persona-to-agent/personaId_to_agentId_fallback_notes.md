# agentId ↔ personaId Fallback Notes

## Overview
- `useAgentPersonaIndex` centralizes agent/persona lookups for UI flows that still need persona metadata.
- Chat experiences (ChatPage, PrimaryDrawer, AgentsPage) are agent-centric; persona lookups only occur when calling legacy APIs that require personaId.

## Legacy Persona Consumers
- **SignalR + chat APIs**: `appendUserMessage`, regenerate, and image memory calls accept an optional personaId; UI supplies it via `resolvePersonaId(agentId)` to maintain compatibility while the backend dual-writes.
- **Image Lab**: user-generated images remain persona-scoped so that image galleries, prompts, and persona-sharing continue to work. Image Lab UI continues to request `personaId` and is explicitly out of scope for agent-first migration until backend/image contracts change.
- **AuthContext#setPrimaryPersona** and persona management screens remain persona-driven and are untouched.

## Guidelines
- New UI flows should consume `agentId` and only obtain personaId through `useAgentPersonaIndex` when calling legacy endpoints.
- When deprecating persona-based routes, remove the fallback and delete `personaId` handling branches.
- Leave Image Lab persona logic in place until a new agent-aware image scope is defined.
