# Alpha Security & Observability Hardening

Context: 2025-10-27 external review highlighted security, quota, observability, and scope integrity gaps before any external pilots. This plan captures the remediation path and maps each recommendation into actionable work.

## Snapshot & Scorecard
- Overall alpha readiness: **7.7 / 10** (architecture 8.5, planner framework 8.0, data/vector layers 8.0, API 7.5, observability 7.5, testing 7.5, security/devex 6.5, console 7.0).
- Strengths: PlannerBase lifecycle, scope-path refactor scaffolding, deterministic fakes, Ops alerting + telemetry, Plans discipline.
- Weak spots: missing rate limits/quotas/authorization policies, ad-hoc ScopePath construction risk, token spend controls, OpenSearch schema drift protections, end-to-end correlation IDs, console auth/error guards.

## P0 Blockers (Pre-pilot)
1. **Global & per-principal throttles**
   - DONE Add ASP.NET rate limiting middleware with persona/agent quotas (Program.cs:67, src/Cognition.Api/appsettings.json:12).
   - TODO Audit CancellationToken propagation in downstream clients after tightening quotas.
2. **ScopePath factory lockdown**
   - Make ScopePath builder the only construction path (internal constructors, DI-exposed factory).
   - Audit repository for `new ScopePath` usage and replace with factory.
   - Optional: Roslyn analyzer or CI script to block direct instantiation.
3. **Authorization policies**
   - Define `[Authorize(Policy = "...")]` per controller/action, driven by persona/agent roles.
   - Add unit tests that fail when endpoints lack required attributes.
4. **Planner budgets & throttling**
   - Configuration per planner/persona for max iterations, critique toggle, token caps.
   - Emit `planner.throttled` / `planner.rejected` telemetry and Hangfire circuit breaker to avoid requeue storms.
5. **Correlation IDs & structured logging**
   - DONE RequestCorrelationMiddleware stamps correlation IDs at the API edge (RequestCorrelationMiddleware.cs).
   - TODO extend correlation scopes beyond API (Jobs, Clients, telemetry payloads).

## P1 Stabilisation Tasks
- Fail-fast template seeding: validate every `PlannerMetadata.Step` template exists at startup (`StartupDataSeeder` self-test).
- OpenSearch schema/mapping guard: boot-time verification of index template dimensions + fields.
- Harden DTO validation (FluentValidation/DataAnnotations); add integration tests asserting 400s.
- Anti-abuse headers/CORS defaults for API + Console.
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
- [x] Implement ASP.NET rate limiting + per-agent/persona quotas with configuration in `appsettings`.
- [ ] Enforce request body size limits and audit CancellationToken usage.
- [ ] Refactor ScopePath construction to a locked factory; remove public constructors.
- [ ] Introduce analyzer or CI check preventing direct `new ScopePath`.
- [ ] Define and test authorization policies per controller.
- [ ] Ship planner token budgets and throttling telemetry.
- [ ] Wire structured logging + correlation IDs across API, Jobs, Clients, LLM, and vector layers.
- [ ] Add template seeding self-test and OpenSearch schema guard.
- [ ] Harden DTO validation and API abuse headers.
- [ ] Extend PlannerHealth with latency, retry, critique, and SLO aggregates.
- [ ] Adopt nullable reference types + warnings as errors + analyzers.
- [ ] Add golden transcript snapshots and property-based tests.
- [ ] Publish updated developer/ops documentation and checklist for alpha hardening.

## References
- External review (2025-10-27) scorecard + recommendations.
- Existing plans: `plans/planning_the_planner.md`, `plans/scope_token_path_refactor.md`, `plans/_next_session_prompt.md`, `plans/hot_targeted_todo.md`.
- Relevant code: `src/Cognition.Api/*`, `src/Cognition.Clients/Scope`, `src/Cognition.Clients/Tools/Planning`, `src/Cognition.Jobs/*`, `tests/*`.
