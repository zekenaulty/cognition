# Agent Cutover & Scope Cleanup — Step 2025-11-30 03:50 — Console agent-first alignment

## Goal
Align console ChatHub clients to agent-first payloads and loosen persona requirements for chat sends.

## Context
- ChatHub emits agentId + optional personaId; persona stays for images/ownership.

## Commands Executed
- None (UI edits)

## Files Changed
- `src/Cognition.Console/src/bus/chatBus.ts` (agentId/personaId optional event shapes)
- `src/Cognition.Console/src/types/events.ts` (agentId/personaId optional)
- `src/Cognition.Console/src/hooks/useChatHub.ts` (AppendUserMessage signature includes agentId; deltas carry agentId)
- `src/Cognition.Console/src/pages/ChatPage.tsx` (sendUserMessage now passes agentId; persona optional; removed hard block when persona missing)

## Tests / Results
- Not run (UI-only change)

## Issues
- UI components may still display blanks when personaId is missing; need follow-up rendering for agent labels.
- No frontend build executed here.

## Decision
- Keep agent-first client wiring; plan follow-up for display polish.

## Completion
- ✅

## Next Actions
- Update chat UI to render agent badge/label when persona absent; ensure other components consuming events handle optional persona.
