# Agent Cutover & Scope Cleanup

## Objective
- Remove remaining persona dependencies from runtime chat/planner paths while preserving legitimate persona-first assets (e.g., image generation) and formalizing scope enforcement.
- Ensure agent-centric flows are the default for conversations, messages, and plans; isolate or retire persona fallbacks.

## Scope
- In scope: data/model migrations, API/service/controller updates, tool/retrieval scope enforcement, console updates, regression tests, and configuration flags suitable for pre-alpha (single dev environment; no staging/prod).
- Persona graph constraints: all users have a persona; all agents have a persona; not all personas are agents; personas can own personas (PersonaPersonas supports user→persona and agent/author persona → character persona ownership). This ownership model must remain intact.
- Image generation remains persona-scoped by design (persona-as-author hook) even after agent cutover.
- Out of scope: HGTF/sandbox, non-fiction planner work, production rollout beyond agreed environments.

## Definition of Done
- Conversations/messages/plans no longer require persona IDs for runtime flows; persona fallbacks are removed or isolated behind a well-documented adapter.
- ChatController and related APIs are agent-first; persona usage limited to supported ownership/image paths and encapsulated.
- ScopePath dual-write and path-aware hashing are enabled in non-prod with guardrails; rollout plan + rollback documented.
- Persona ownership graph (PersonaPersonas) remains intact for user/agent/author → character ownership; tests cover these invariants.
- Console/UI updated to reflect agent-first flows while keeping persona-scoped image tooling.
- Migrations/backfill scripts + rollback steps are captured; regression/API tests cover agent-only flows and persona-owned assets.

## Deliverables
- Data migrations to drop/relax persona requirements on conversations/messages/plans; backfill scripts and rollback notes.
- Service/controller updates (ChatController, retrieval, tool dispatcher) enforcing agent-first scope.
- ScopePath flags enabled with environment guards + monitoring.
- Console updates for agent-first flows; explicit persona-scoped image surfaces retained.
- Tests: API/regression for agent-only chat/plan flows; ownership invariants; ScopePath flag behavior; persona-scoped image access.
- Documentation: rollout/rollback, config flags, persona ownership and image-scope rationale.

## Data / API / UI Changes
- Data: new migration to remove/relax persona columns where redundant; preserve PersonaPersonas; add any needed ownership constraints/tests.
- API: ChatController/tool dispatch/runners use agent IDs; persona adapters isolated; image endpoints remain persona-scoped.
- UI: agent pickers as primary; persona selectors only where required (image generation/ownership views); guard tooltips/docs.

## Migration / Rollout Order (pre-alpha)
1) Inventory + design: catalog persona usages (controllers/entities/UI) and classify keep/remove/isolate.
2) Migration design: plan backfill and schema changes; define rollback (dev-only).
3) Implement migrations + service/controller updates; add regression tests.
4) Enable ScopePath dual-write/path-aware hashing in the pre-alpha/dev environment with lightweight monitoring; prep toggle + rollback notes.
5) Console updates and validation; ship docs.

## Testing / Verification
- API: agent-only chat/plan happy/negative paths; persona fallback adapter (if retained) covered explicitly.
- Data: migration backfill tests; ownership invariants (PersonaPersonas) and image-scope tests.
- Scope: ScopePath flag behavior tests in pre-alpha/dev mode; tool dispatcher/retrieval scope enforcement.
- UI: agent-first flows; persona-scoped image generation access.
- Rollback drills (schema + flags) appropriate for single-environment pre-alpha.

## Risks / Mitigations
- Breaking persona-owned assets: keep persona-scoped image path; add tests/docs.
- Ownership regressions: enforce PersonaPersonas invariants in tests.
- ScopePath flag rollout: stage in non-prod with toggle and monitoring.
- Data backfill errors: dry-run scripts + rollback captured.

## Worklog Protocol
- Follow `plans/README.md`: each step gets `plans/agent_cutover_and_scope_cleanup_step_YYYYMMDD_HHMM_<topic>.md` with goal/context/commands/files/tests/issues/decision/completion/next actions.

## Initial Steps
1) Inventory persona usages across controllers (e.g., Chat), entities (Conversation*, Plan, Message), services, UI; classify keep/remove/isolate (persona-only image flows, PersonaPersonas).
2) Design migrations/backfill for persona removal/relaxation; define rollback.
3) Plan ScopePath flag rollout in non-prod with guardrails; add regression tests for agent-only flows and persona-scoped image access.
