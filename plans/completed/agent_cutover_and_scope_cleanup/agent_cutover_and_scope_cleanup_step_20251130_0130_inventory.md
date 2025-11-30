# Agent Cutover & Scope Cleanup — Step 2025-11-30 01:30 — Inventory

## Goal
Inventory persona dependencies across API/services/UI, classify keep/remove/isolate for agent-first cutover while preserving persona-owned assets (images) and PersonaPersonas ownership graph.

## Context
- Pre-alpha, single environment; no staging/prod toggles.
- Persona constraints: all users have a persona; all agents have a persona; not all personas are agents; personas can own personas (PersonaPersonas supports user→persona and agent/author→character ownership).
- Image generation must remain persona-scoped (author persona hook).

## Commands Executed
- `rg "personaId" src`

## Findings
- **API/controllers**
  - `ChatController` still resolves personaId from agent and uses persona-based `Ask/AskWithTools/ChatAsync`; conversation lookup expects personaId (agent-first needed).
  - `ChatHub` SignalR methods are persona-based (conversationId + personaId).
  - `AgentsController` filters by caller persona ownership (keep; aligns with PersonaPersonas).
  - `ImagesController` lists images by persona (keep persona-scoped).
  - `Program.cs` rate-limiter partitions by persona query param (review for agent-first).
  - `FictionPlansController` writes personaId into obligation payloads (persona obligations stay).
  - `UsersController` persona link/unlink endpoints present (likely keep for ownership).
- **Services/clients**
  - `AgentService` has extensive persona-based chat methods and tool contexts; legacy persona ChatAsync noted; provider/model resolution by persona.
  - `ToolDispatcher`/planning runners pass personaId in `ToolContext`.
  - `ScopePathBuilder` accepts personaId (needs dual-write/hash enablement review).
  - Planner options/quotas/contracts support persona allow-lists; critique options keyed by persona.
  - Fiction: `FictionPlanCreator` resolves agent via persona; lifecycle services load persona for obligations/characters.
  - Jobs: `TextJobs`, `SignalRNotifier`, `FictionWeaverJobs`, `ImageJobs` carry personaId; authoring registry uses persona memories.
- **UI/console**
  - Persona-centric flows pervasive: `ChatPage`, `useChatHub`, `bus/chatBus` events include personaId; conversation fetch can filter by personaId.
  - Image Lab (Generate/Gallery tabs) requires personaId and calls `/api/images/by-persona/{personaId}` (keep).
  - Agents/Personas pages show persona linkage; AuthContext tracks primaryPersonaId; API client exposes persona endpoints.
  - Fiction UI: Plan wizard filters agents by personaId; backlog panel uses persona context; agent/persona index hook enforces persona access.
- **Data/invariants**
  - PersonaPersonas ownership must remain; persona-scoped assets (images, obligations, author memories) are intentional keepers.

## Files Changed
- None

## Tests / Results
- None (inventory only)

## Issues
- Agent-first cutover must unwind persona-based chat/service entry points without breaking persona-scoped image/fiction obligations.
- EF migrations not yet designed; ScopePath dual-write/hash enablement still pending.

## Decision
- Inventory completed; ready to design migration/backfill and agent-first runtime changes.

## Completion
- ✅

## Next Actions
- Draft migration/backfill/rollback and flag rollout plan (agent-first runtime, keep persona-scoped images/obligations).
