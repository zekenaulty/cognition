# Agent Cutover & Scope Cleanup — Step 2025-11-30 01:40 — Migration/Runtime Plan

## Goal
Design the schema/backfill/rollback and flag strategy to make chat/planner flows agent-first while keeping persona-scoped image/ownership paths intact.

## Context
- Pre-alpha single environment; dev-only toggles are sufficient.
- Persona rules: all users have a persona; all agents have a persona; not all personas are agents; personas can own personas (PersonaPersonas) and own assets (images, obligations, author memories).
- Agent-first flows must not break persona-scoped images or fiction obligations; persona data remains authoritative for ownership.

## Commands Executed
- None (design-only)

## Design / Plan
- **Schema/migrations (dev-only)**
  - Conversations: already agent-bound; keep agentId. Make conversation participants optional (persona rows allowed but not required for runtime). No schema change needed there.
  - ConversationMessage: make `FromPersonaId`/`ToPersonaId` nullable; keep `FromAgentId` required. Backfill existing rows to copy agent’s persona into `FromPersonaId` when null; allow null going forward to support agent-only posts.
  - ConversationPlan: make `PersonaId` nullable (agent-owned plans); keep if supplied for persona-scoped plans.
  - PlannerExecution: ensure AgentId/PrimaryAgentId present; leave persona metadata out.
  - Personas/PersonaPersonas: no change (ownership graph preserved).
  - Images: keep persona-scoped tables/endpoints unchanged.
  - Add migration with rollback notes: nullable persona cols, backfill script to set personaId = agent.PersonaId where applicable; rollback by re-enforcing NOT NULL (dev only).
- **Runtime/service updates**
  - ChatController/ChatHub: resolve agentId first; persona optional. If persona provided, validate ownership via PersonaPersonas. ToolContext should carry agentId; personaId optional (only for image/author features). Prefer `AgentService.Ask` variants by agentId; mark persona-only methods obsolete/internal.
  - AgentService: promote agent-first entry points; guard legacy persona methods behind adapter layer for image flows; ensure provider/model resolution works via agent->persona mapping.
  - Retrieval/ToolDispatcher: enforce scope with agentId (persona optional). ScopePathBuilder uses persona when provided but defaults to agent-only scope; enable dual-write/path-hash flags in dev.
  - Planner/Fiction: `FictionPlanCreator` should take agentId and derive persona only when needed for author obligations; backlog/obligation personas remain.
- **UI/console**
  - Default to agent pickers; persona selectors only where required (image lab, author persona contexts, obligations). Conversation list should filter by agentId primarily; persona filters optional.
- **Flags/rollout**
  - ScopePath dual-write/path-aware hashing: turn on in dev; keep toggle in config with rollback to legacy path. Add simple log/metric to validate key shapes.
  - Feature flag: `Chat:AllowPersonaFallback` (dev default true) to gate remaining persona-based adapters; plan to remove once endpoints agent-only.
- **Backfill/validation**
  - Backfill personaId on messages/plans from linked agent.PersonaId, then relax null constraints.
  - Add validation tests: agent-only chat/plan works without persona; persona-scoped image access still enforced; PersonaPersonas ownership invariants stay intact; scope path flag produces hashed path.
- **Rollback**
  - Migration rollback re-enables NOT NULL persona columns (dev only).
  - Config rollback: disable ScopePath dual-write/hash; re-enable persona fallback flag if needed.

## Files Changed
- None

## Tests / Results
- None (design-only)

## Issues
- Need to audit JS bundle persona usage after controller changes (built assets may lag).

## Decision
- Proceed with nullable persona columns on messages/plans, agent-first services/controllers, keep persona for images/ownership, enable ScopePath dual-write/hash in dev with toggle.

## Completion
- ✅

## Next Actions
- Implement migration + runtime changes + regression tests per design, then update UI/console and docs.
