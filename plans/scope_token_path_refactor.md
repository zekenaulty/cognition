# Scope Token Path Refactor

Objective
- Move scope identity from fixed fields to a canonical, ordered path while keeping steward ownership explicit via a root principal (persona, agent, org, etc.).
- Ensure hashing, retrieval, and dedupe all respect the path so scoped variants never collide or bleed.
- Deliver fluent LINQ/query helpers so RAG, jobs, and tools can target scopes without bespoke wiring.
- Keep migration low-risk via dual-write/backfill phases and transparent rollouts.

Scope
- `Cognition.Clients` scope token models, hash helpers, and retrieval utilities.
- Shared value objects for `ScopeSegment`, `ScopePath`, and canonicalization.
- Persistence updates in `Cognition.Data.Relational` and `Cognition.Data.Vectors` to store both legacy fields and path metadata.
- Tool dispatch / retrieval layers (`ToolDispatcher`, `ToolRegistry`, RAG helpers) to adopt the new path-centric API.
- Test coverage across Clients, Data, Jobs, and API projects validating the new behavior.

Out of Scope
- UI/console changes or front-end consumers.
- Non-scope identity systems (auth, user profiles, multi-tenant routing).
- Full graph relationship modeling beyond path-aware ancestry.
- Production data backfills (document plan only if deferred).

Deliverables
- `ScopePrincipal`, `ScopeSegment`, `ScopePath` value objects plus normalization helpers.
- Updated `ScopeToken` (or successor) requiring exactly one principal segment (RootId + PrincipalType) and ordered context segments.
- Path-aware hashing (`ComputeContentHash` replacement) including canonical path input and principal fingerprint.
- Dual-write persistence (legacy columns + new principal/path fields), with migration scripts/read models documented.
- Fluent query extensions (`ForPrincipal`, `ForPersona`, `ForAgent`, `WithContext`, `UnderPath`, `WalkUp`) for EF/OpenSearch/Cosmos consumers.
- Updated tooling docs + developer guidance for constructing scopes with principals.
- Regression tests: hashing, persistence, LINQ helpers, dispatcher integration, retrieval fallbacks.

Strategy
- Phase 0 - Discovery & Design
  - Inventory current scope usage (clients, data, jobs, API) and confirm steward requirements.
  - Finalize canonical principal + segment schema, allowed keys, normalization rules, and validation guardrails.
  - Author migration approach (dual-write, backfill, cutover).

- Phase 1 - Core Model & Hashing
  - Introduce `ScopePrincipal`, `ScopeSegment`, `ScopePath`, and canonical renderer.
  - Update `ScopeToken` (or successor) to enforce a single principal segment followed by ordered context segments.
  - Extend hashing utilities to include principal fingerprint + canonical path; guard with feature flag.

- Phase 2 - Persistence & Dual Write
  - Add principal/path columns or JSON fields to relational/vector stores; update EF models.
  - Implement dual-write (legacy fields + new principal/path metadata) behind toggle.
  - Backfill writer logic to populate principal/path data and `Source` fallbacks for old records.

- Phase 3 - Query & Retrieval Surface
  - Build LINQ/EF helpers and query extensions (exact, prefix, ancestor walk-up) keyed off `ScopePrincipal`.
  - Update RAG retrieval, `ToolDispatcher`, and other callers to consume the fluent API.
  - Provide path-aware filters in vector/OpenSearch builders.

- Phase 4 - Migration & Cutover
  - Run backfill scripts or opportunistic updates; verify coverage.
  - Flip hashing to path-aware mode; monitor collisions.
  - Remove legacy-only code paths once adoption verified; document cleanup.

- Phase 5 - Validation & Hardening
  - Expand tests across suites with shared fakes (scope constructors, retrieval fallbacks).
  - Update documentation, examples, and developer onboarding.
  - Capture metrics for scope usage; prepare regression nets as needed.

Data / Storage Changes
- New columns/JSON fields for principal (RootId + PrincipalType), canonical path string, and structured segments.
- Migration scripts for schema adjustments and dual-write toggle.
- Optional view/materialized path for analytics if required.

Migration Strategy
- Introduce feature flags to gate dual-write and path-inclusive hashing.
- Backfill existing rows incrementally with canonical path + hash update.
- Monitor collision metrics before removing legacy hash inputs.
- Rollback plan: disable flag, continue legacy hash/read paths.

Testing / Verification
- Unit tests for path normalization, validation, hashing uniqueness.
- Integration tests for persistence (dual-write, read-back).
- Retrieval/service tests covering exact, prefix, and fallback behavior.
- Tool dispatcher tests verifying scope preservation across execution.
- End-to-end smoke (vector upsert/search, API flows) once toggled on.

Risks & Mitigations
- Collision or bleed if hashing misconfigured -> exhaustive tests + feature gating.
- Query perf regressions on principal/path columns -> add indexes and benchmark before enabling flags.
- Migration drift -> dry-run backfills in lower environments with detailed runbooks.
- Consumer misuse of segments -> guard APIs, validation errors, and developer documentation.

Worklog Protocol
- Snapshot via `arc.py` + git tag before major code changes; record in step note.
- Each discrete task logs to `plans/scope_token_path_refactor_step_YYYYMMDD_HHMM_<slug>.md` using standard template (Goal, Commands, Files, Tests, Issues, Decision, Completion, Next Actions).
- Use `plans/_scratchpad.md` for quick research or TODOs between formal steps.

Checklist
- [ ] Capture pre-change snapshot + tag.
- [x] Finalize canonical path schema & validation rules.
- [x] Implement path-aware model + hashing (behind flag).
- [x] Add persistence dual-write paths and migrations.
- [ ] Update retrieval/tooling layers to new LINQ helpers.
- [ ] Backfill existing data and enable path hashing.
- [x] Expand regression tests (clients/data/jobs/api).
- [ ] Update documentation & developer guidance.
- [ ] Apply `ScopePathDualWrite` migration + OpenSearch mapping updates in each environment.
- [ ] Run scoped backfill + monitoring playbook before flipping `PathAwareHashing`.

