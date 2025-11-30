# Alpha Security & Sandbox Closeout

## Objective
- Finish P0/P1 security items in pre-alpha: HGTF + Sandbox implementation, abuse headers, correlation propagation, auth/ops alerting gates.

## Scope
- Sandbox policy surface + tool dispatcher integration.
- HGTF worker/approval jobs and supporting infrastructure.
- API abuse headers + rate-limit e2e tests.
- Correlation propagation API → Jobs → Clients → LLM.
- Auth/ops alerting gates and docs updates.
- Pre-alpha only (single dev environment; no staging/prod).

## Definition of Done
- HGTF worker + approval jobs run in the pre-alpha environment with integration tests covering enable/approval flows.
- Sandbox policy enforced in tool dispatch; violations blocked/logged; tests cover allowed/denied cases.
- API abuse headers + rate-limit e2e tests pass.
- Correlation IDs flow API → Jobs → Clients → LLM (validated by tests/log assertions).
- Ops alerting exercised via webhook test; auth gates validated for covered endpoints.
- Docs updated (docs/specs/* as needed) with config flags/runbooks.

## Deliverables
- New infrastructure/services and Hangfire jobs for HGTF worker/approval queue.
- Sandbox policy surface and dispatcher integration with config flags.
- Integration/unit/e2e tests for sandbox/HGTF, abuse headers, rate limits, correlation propagation, auth/ops alerting.
- Docs updates (docs/specs/*), config samples, and step notes per `plans/README.md`.

## Migration / Rollout Order (pre-alpha)
1) Design sandbox policy surface and dispatcher integration; define config flags.
2) Scaffold HGTF job lane and approval queue (Hangfire jobs/services).
3) Add e2e rate-limit + abuse-header tests; correlation propagation tests.
4) Implement sandbox enforcement + HGTF flows; wire ops alerting/auth gates.
5) Update docs/specs and record runbooks; validate end-to-end in pre-alpha.

## Testing / Verification
- E2E: abuse headers + rate limits; sandbox allow/deny; HGTF approval path; ops webhook alert; correlation propagation traced.
- Unit/integration: dispatcher sandbox policy, HGTF services/jobs, auth gate checks.
- Docs validated against runnable configs in pre-alpha.

## Risks / Mitigations
- Sandbox false positives: start with audit/log mode and narrow policies; add allowlist tests.
- HGTF complexity: keep minimal viable lane + approval path first.
- Correlation gaps: add log assertions and middleware/unit coverage.
- CI/time: scope tests to pre-alpha essentials.

## Worklog Protocol
- Follow `plans/README.md`: each step in `plans/alpha_security_sandbox_closeout_step_YYYYMMDD_HHMM_<topic>.md` with goal/context/commands/files/tests/issues/decision/completion/next actions.

## Initial Steps
1) Design sandbox policy surface and dispatcher integration.
2) Scaffold HGTF job lane and approval queue.
3) Add e2e rate-limit + abuse-header tests; wire correlation scopes into tool runner paths.
