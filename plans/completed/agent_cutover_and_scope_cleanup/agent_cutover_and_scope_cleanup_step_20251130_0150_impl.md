# Agent Cutover & Scope Cleanup — Step 2025-11-30 01:50 — Agent-first impl/migration

## Goal
Begin agent-first runtime cutover with nullable persona columns on chat records and switch chat APIs/services to agent IDs by default.

## Context
- Pre-alpha, single environment.
- Persona data must remain for ownership/images; columns now nullable to allow agent-only flows.

## Commands Executed
- `dotnet ef migrations add AgentCutoverPersonaRelax --project src/Cognition.Data.Relational --startup-project src/Cognition.Api`
- `dotnet ef migrations remove --project src/Cognition.Data.Relational --startup-project src/Cognition.Api` (to fix empty migration before entity tweaks)
- `dotnet ef migrations add AgentCutoverPersonaRelax --project src/Cognition.Data.Relational --startup-project src/Cognition.Api`
- `dotnet build`

## Files Changed
- `src/Cognition.Data.Relational/Modules/Conversations/ConversationMessage.cs` (FromPersonaId nullable)
- `src/Cognition.Data.Relational/Modules/Conversations/ConversationPlan.cs` (PersonaId nullable)
- `src/Cognition.Data.Relational/Migrations/20251130074153_AgentCutoverPersonaRelax.cs` (+backfill SQL, nullability changes/fks; snapshot/designer auto-updated)
- `src/Cognition.Clients/Agents/AgentService.cs` (Ask/AskWithPlan now agent-first; resolve persona internally)
- `src/Cognition.Api/Controllers/ChatController.cs` (ask/ask-with-tools agent-first; chat uses agent id, publishes events, relies on service persistence)

## Tests / Results
- `dotnet build` (pass; existing nullable warnings in ToolDispatcher unchanged)

## Issues
- ChatHub/SignalR still persona-based; consider aligning with agent-first later.
- Rate-limit partition still persona query; revisit once controller/plumbing settled.
- No new API/regression tests yet for agent-only chat flow.

## Decision
- Agent-first service/controller paths in place; schema relaxed for personas on messages/plans with backfill safeguards.

## Completion
- ✅

## Next Actions
- Wire tests for agent-only chat/plan flow and PersonaPersonas invariants.
- Update ChatHub/UI filters to agent-first; keep persona only for image/ownership surfaces.
- Enable ScopePath dual-write/hash flags in dev and add small regression to assert agent-only scope path.
