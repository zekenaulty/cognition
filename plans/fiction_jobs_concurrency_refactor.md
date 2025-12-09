# Fiction Jobs Concurrency Refactor

## Objective
Make fiction jobs (backlog scheduler, weaver phases, lore fulfillment) resilient to concurrent workers and failures by using atomic claims and deterministic recovery instead of ad-hoc status flips. Prevent duplicate execution, stranded InProgress items, and conflicting world-bible writes, while keeping pre-alpha scope lean.

## Scope
- In: Backlog claim/resume/auto-queue paths; phase job failure handling; lore fulfillment concurrency; Hangfire storage guidance; minimal schema/state changes to support atomic claims.
- Out: New features, deep planner logic changes, sandbox/tooling changes, and non-fiction jobs.

## Deliverables
- Atomic claim/update path for backlog items (Pending→InProgress) and for auto-resume (InProgress→Pending) with guard predicates.
- Phase job wrapper that, on failure, moves backlog/task/checkpoint off InProgress (to Failed or Pending retry) and records the error.
- Lore fulfillment guard: early claim + unique-conflict handling to avoid duplicate entries/slug collisions.
- Optional concurrency token(s) (`RowVersion`) on backlog/requirements if needed.
- Docs note on Hangfire Redis storage for durability/retries.

## Data / Service Changes
- EF updates: guarded status updates (or rowversion) on `FictionPlanBacklogItem`; optional on `FictionLoreRequirement`.
- Scheduler: atomic claim/resume; enqueue only on successful claim.
- Phase jobs: failure handler resets states; optional heartbeat column for long runs (backlog/checkpoint).
- Lore fulfillment: catch unique violation on slug/version, re-read requirement, and exit if already linked.

## Migration / Rollout Order
1) Add optional rowversion/columns (or guarded updates) for backlog + lore requirement; keep migration small.
2) Update scheduler to atomic claim/resume; add tests for double-claim prevention.
3) Add job failure handler to set backlog/task/checkpoint to Failed/Pending retry; ensure publish/log.
4) Lore fulfillment guard (claim + unique-handling).
5) Docs/config note: Hangfire Redis storage recommended in prod.

## Testing / Verification
- Unit/integration tests: two schedulers racing → only one claim; failed job → backlog returns to Pending/Failed and can be re-queued; lore fulfillment double-run → no duplicate entry, no exception leak.
- Existing regression: rerun fiction resume/backlog tests.

## Risk / Rollback
- Risk: new claim logic could stall scheduling if predicates wrong. Mitigation: keep fallback logging and metrics; feature-flag the guarded claim if needed.
- Rollback: revert to prior scheduler/job changes; migrations are additive (rowversion columns safe to keep).

## Worklog Protocol
- Step notes per `plans/README.md`, one discrete action each, with commands, paths, tests, decisions, and completion status.

## Checklist
- [ ] Migration/guard for backlog/lore claim
- [ ] Scheduler atomic claim/resume
- [ ] Failure handler resets states
- [ ] Lore fulfillment conflict guard
- [ ] Tests added/updated
- [ ] Redis storage note in docs/config
