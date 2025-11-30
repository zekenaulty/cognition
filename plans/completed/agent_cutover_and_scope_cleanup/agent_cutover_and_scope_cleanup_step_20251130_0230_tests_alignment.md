# Agent Cutover & Scope Cleanup — Step 2025-11-30 02:30 — Tests & pending alignment

## Goal
Add regression tests for agent-first flows/persona ownership; note remaining UI/ChatHub/rate-limit work.

## Context
- Agent-first service/controller in place; scope path hashing enabled in dev.
- Need proofs around agent-only chat/plan + persona ownership invariants.

## Commands Executed
- `dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "AgentServiceAgentTests|PersonaOwnershipTests"`

## Files Changed
- `tests/Cognition.Clients.Tests/Agents/AgentServiceAgentTests.cs` (agent-first AskAsync regression with stub LLM; in-memory context ignores dict props)
- `tests/Cognition.Data.Relational.Tests/Modules/Personas/PersonaOwnershipTests.cs` (PersonaPersonas ownership invariants)

## Tests / Results
- ✅ `dotnet test tests/Cognition.Clients.Tests/Cognition.Clients.Tests.csproj --filter "AgentServiceAgentTests|PersonaOwnershipTests"`

## Issues
- ChatHub/console still persona-centric; needs agent-first alignment (keep persona for images/ownership).
- Rate-limit partitioning still persona query param; revisit after plumbing.
- No API-level E2E for agent-only chat/plan yet; current coverage is service + DB invariants.

## Decision
- Keep progressing with UI/ChatHub alignment and rate-limit key swap next; current tests cover agent-first service path and persona ownership graph.

## Completion
- ✅

## Next Actions
- Add API/controller-level agent-only chat/plan tests or lightweight WebApplicationFactory smoke.
- Update ChatHub/console filters to agent-first; retain persona for image/author surfaces.
- Swap rate-limit partition to agent-first once UI/ChatHub aligned; add regression for ScopePath hash flag (already enabled in dev).
