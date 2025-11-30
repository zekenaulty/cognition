# Agent Cutover & Scope Cleanup — Step 2025-11-30 04:10 — UI agent labels

## Goal
Ensure console surfaces show agent labels when personaId is absent on agent-first events.

## Commands Executed
- None (UI edits)

## Files Changed
- `src/Cognition.Console/src/pages/ChatPage.tsx` (assistant messages, plan/tool events now resolve agent label when persona is missing)

## Tests / Results
- Not run (UI-only change)

## Issues
- Other components (outside ChatPage) may still assume personaId; further audit may be needed if new agent-only events appear.

## Decision
- Agent labels now flow into chat tool/plan event strings; continue UI audit as needed.

## Completion
- ✅

## Next Actions
- Optional: add agent labels to any remaining components that render persona-only data.
