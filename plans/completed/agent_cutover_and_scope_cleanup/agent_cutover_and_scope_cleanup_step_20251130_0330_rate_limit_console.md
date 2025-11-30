# Agent Cutover & Scope Cleanup — Step 2025-11-30 03:30 — Rate limit swap + console event shape

## Goal
Swap rate-limit partitioning to agent-first with fallbacks and align console event contracts to the new agent-first ChatHub payloads.

## Context
- Agent-first hub/service already in place; needed consistent rate-limit key and console payload updates.

## Commands Executed
- `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "AbuseHeadersAndRateLimitE2ETests"`

## Files Changed
- `src/Cognition.Api/Program.cs` (agent policy now agentId→conversationId→personaId→userId)
- `src/Cognition.Api/Infrastructure/Security/ApiRateLimiterPartitionKeys.cs` (conversationId resolver)
- `tests/Cognition.Api.Tests/Controllers/AbuseHeadersAndRateLimitE2ETests.cs` (fallback regression + config tweaks)
- `src/Cognition.Console/src/bus/chatBus.ts` (agentId/personaId optional in events)
- `src/Cognition.Console/src/types/events.ts` (agent-first optional fields)
- `src/Cognition.Console/src/hooks/useChatHub.ts` (AppendUserMessage signature updated to agentId, persona optional; deltas carry agentId)

## Tests / Results
- ✅ `dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj --filter "AbuseHeadersAndRateLimitE2ETests"`

## Issues
- Console components that read personaId may need follow-up to display agent labels when persona missing (not updated yet).
- No UI smoke run; TS consumers should compile with optional fields but may render blanks if not handled.

## Decision
- Keep agent-first payloads; follow up with UI handling for missing personaId where needed.

## Completion
- ✅

## Next Actions
- Pass through agentId-aware rendering in console chat/telemetry panels.
- Optional: add API-level smoke for agent-only chat/plan flows.
