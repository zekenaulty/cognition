# Agent Cutover & Scope Cleanup — Step 2025-11-30 02:10 — Config + remaining tests TODO

## Goal
Advance towards agent-first verification: enable scope path hashing in dev and list pending API/regression tests and ChatHub/UI alignment work.

## Context
- Pre-alpha dev environment only; enabling hashing is safe.
- Tests and UI alignment still pending; this step captures config + TODOs.

## Commands Executed
- None (config edit only)

## Files Changed
- `src/Cognition.Api/appsettings.Development.json` (PathAwareHashingEnabled set to true)

## Tests / Results
- Not run (config-only change)

## Issues
- API/regression tests for agent-only chat/plan and PersonaPersonas invariants still to be added.
- ChatHub/UI remain persona-first; need agent-first alignment (keep persona for images/ownership).
- Rate-limit partition still persona-based; revisit after plumbing.

## Decision
- Enable scope path path-aware hashing in dev to exercise dual-write/hash behavior now.

## Completion
- ✅

## Next Actions
- Add tests: agent-only chat/plan flows, PersonaPersonas ownership invariants, scope path hashing regression.
- Align ChatHub/UI filters to agent-first; preserve persona-only surfaces (images/authoring).
- Revisit rate-limit partition key to agent-first after plumbing/tests stabilize.
