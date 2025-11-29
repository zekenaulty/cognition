# Alpha Security & Observability Hardening

Context: 2025-10-27 external review highlighted security, quota, observability, and scope integrity gaps before any external pilots. This plan captures the remediation path and maps each recommendation into actionable work.

## Definition of Done
- ScopePath construction is locked behind the DI factory with analyzer/test coverage proving no direct `new ScopePath` or `Parse` calls ship; regression guard lives in CI.
- Correlation IDs and structured logging propagate from API → Jobs → Clients → vector + LLM calls, and staging traces show end-to-end linkage for at least one fiction + non-fiction workflow.
- HGTF sandbox orchestration runs in staging (OOPS worker, enable/approval jobs, integration test battery) with docs describing the rollout switches.
- DTO validation + abuse headers + explicit auth policies for admin/console endpoints are enforced, with automated tests covering success/failure and PlannerHealth dashboards reflecting latency/retry/SLO metrics.
- Cosine similarity scoring parity is verified by running the offline harness against OpenSearch fixtures, and developer/ops guides are updated to capture the new safeguards.

## Snapshot & Scorecard
- Overall alpha readiness: **7.7 / 10** (architecture 8.5, planner framework 8.0, data/vector layers 8.0, API 7.5, observability 7.5, testing 7.5, security/devex 6.5, console 7.0).
- Strengths: PlannerBase lifecycle, scope-path refactor scaffolding, deterministic fakes, Ops alerting + telemetry, Plans discipline.
- Weak spots: missing rate limits/quotas/authorization policies, ad-hoc ScopePath construction risk, token spend controls, OpenSearch schema drift protections, end-to-end correlation IDs, console auth/error guards.

## P0 Blockers (Pre-pilot)
1. **ScopePath factory lockdown**
   - Make ScopePath builder the only construction path (internal constructors, DI-exposed factory).
   - Audit repository for `new ScopePath` usage and replace with factory.
   - Add analyzer coverage + tests that fail when `ScopePath.Parse` or direct constructors surface; document the factory-only contract for future contributors.
   - ✅ `TRACK-482` Ensure analyzer/CI rules flag direct `ScopePathBuilder` instantiation outside tests and propagate correlation IDs through jobs/planner telemetry before enabling stricter quotas.
2. **Correlation IDs & structured logging**
   - Extend correlation scopes beyond API (Jobs, Clients, telemetry payloads).
3. **Tool foundry + sandbox orchestration**
   - Implement the OOPS worker lane, deterministic build/enable Hangfire jobs, and HGTF integration tests so the spec matches code.

## P1 Stabilisation Tasks
- Fail-fast template seeding: validate every `PlannerMetadata.Step` template exists at startup (`StartupDataSeeder` self-test).
- OpenSearch schema/mapping guard: boot-time verification of index template dimensions + fields.
- [ ] Harden DTO validation and API abuse headers. ✅ Attribute coverage landed across Chat/Users/Config/Tools/Personas/Conversations (see lpha_security_observability_hardening_step_20251102_2100_dto_validation.md). Follow-on: finish API abuse headers and assert ApiErrorResponse codes via automated tests.
- Anti-abuse headers/CORS defaults for API + Console.
- Standardize API error payloads; publish the contract (docs/api-error-responses.md) and instrument telemetry for high-volume codes.
- Align in-memory vector scoring with cosine similarity so offline harnesses match production ranking.
- Enforce explicit authorization policies/roles across admin endpoints.
- Introduce feature flags for HGTF, sandbox, and canary rollouts.
- Enhance PlannerHealth with latency percentiles, retry counts, critique usage, SLO breach rollups.

## P2 DevEx & Testing
- Enable nullable reference types, treat warnings as errors, and adopt Roslyn analyzers (IDisposable, async suffix, allocation).
- Add golden transcript/artifact snapshots for the fiction pipeline.
- Introduce property-based tests for ScopePath canonicalization and vector DSL queries.
- Provide QueryBuilder fluency to guard KNN-without-scope scenarios.

## 30-Day Hardening Cadence
- **Week 1:** Deliver all P0 blockers (rate limits, scope factory, auth policies, planner budgets, correlation logging) and document new configuration in README + Ops guides.
- **Week 2:** Complete DTO validation, template seeding check, OpenSearch schema guard, and rate-limit regression tests.
- **Week 3:** Flip analyzers/warnings-as-errors, land golden snapshots, add property-based testing, and wire circuit-breaker aware job scheduling.
- **Week 4:** Extend PlannerHealth dashboards (SLOs, p95s, drill-downs), add Console PlanTimeline links, and finalize Ops multi-channel routing.

## Checklist
- [ ] Refactor ScopePath construction to a locked factory; remove public constructors.
- [ ] Introduce analyzer or CI check preventing direct `new ScopePath`.
- [ ] Wire structured logging + correlation IDs across API, Jobs, Clients, LLM, and vector layers.
- [ ] Stand up tool foundry + sandbox orchestration (OOPS worker, enable/approval jobs, HGTF integration tests).
- [ ] Replace in-memory vector scoring with cosine similarity so local harnesses mirror OpenSearch.
- [ ] Add template seeding self-test and OpenSearch schema guard.
- [ ] Harden DTO validation and API abuse headers. ✅ Attribute coverage landed across Chat/Users/Config/Tools/Personas/Conversations (see lpha_security_observability_hardening_step_20251102_2100_dto_validation.md). Follow-on: finish API abuse headers and assert ApiErrorResponse codes via automated tests.
- [ ] Extend PlannerHealth with latency, retry, critique, and SLO aggregates.
- [ ] Enforce explicit authorization policies/roles across admin endpoints.
- [ ] Introduce feature flags for HGTF, sandbox, and canary rollouts.
- [ ] Adopt nullable reference types + warnings as errors + analyzers.
- [ ] Add golden transcript snapshots and property-based tests.
- [ ] Publish updated developer/ops documentation and checklist for alpha hardening.

## References
- External review (2025-10-27) scorecard + recommendations.
- Existing plans: `plans/planning_the_planner.md`, `plans/scope_token_path_refactor.md`, `plans/_next_session_prompt.md`, `plans/hot_targeted_todo.md`.
- Relevant code: `src/Cognition.Api/*`, `src/Cognition.Clients/Scope`, `src/Cognition.Clients/Tools/Planning`, `src/Cognition.Jobs/*`, `tests/*`, `docs/api-error-responses.md`.
