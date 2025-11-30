# Testing & CI Gap Close

## Objective
- Finish the unit-testing expansion with CI rigor for pre-alpha.

## Scope
- Tests and CI for planner, tool dispatcher, scope path hashing/flags, and (future) sandbox/HGTF once implemented.
- Coverage/mutation strategy (Stryker or equivalent), API regression nets, and documentation.

## Definition of Done
- Checklists in `plans/completed/unit_testing_expansion/unit_testing_expansion.md` are closed or explicitly waived with rationale.
- Mutation/Stryker (or equivalent) coverage plan is documented, with thresholds and target suites.
- Hotspots (planner, tool dispatcher, scope path hashing/flag behavior, sandbox/HGTF once built) have regression tests in place.
- README/CI docs updated with run commands, thresholds, and pass/fail criteria; CI config enforces them.

## Deliverables
- New/augmented tests covering hotspots and missing API regression nets.
- CI configuration additions (coverage/mutation thresholds, gating).
- Test documentation (commands, scope, expected results) and recorded pass/fail criteria in step notes.

## Migration / Rollout Order (pre-alpha)
1) Codify hotspot list (planner, tool dispatcher, scope path hashing/flags; sandbox/HGTF placeholders).
2) Add missing API regression nets and targeted unit/integration tests for hotspots.
3) Integrate mutation testing/coverage thresholds into CI; set sensible pre-alpha gates.
4) Update README/CI docs with run commands and expectations; record in step notes.

## Testing / Verification
- Run hotspot test suites and API nets; validate coverage and mutation scores.
- Ensure CI pipeline runs the suites with configured thresholds.
- Document runs and outcomes in step notes.

## Risks / Mitigations
- Flaky tests under mutation/load: start with scoped targets and incremental thresholds.
- CI time/cost: keep pre-alpha gates modest and focused on hotspots.

## Worklog Protocol
- Follow `plans/README.md`: each step in `plans/testing_and_ci_gap_close_step_YYYYMMDD_HHMM_<topic>.md` with goal/context/commands/files/tests/issues/decision/completion/next actions.
- Capture scope/context changes as you work (static RAG in `plans/`) so later sessions can anchor.

## Initial Steps
1) Codify hotspots list.
2) Add missing API regression nets.
3) Integrate mutation/coverage thresholds and document run commands.
