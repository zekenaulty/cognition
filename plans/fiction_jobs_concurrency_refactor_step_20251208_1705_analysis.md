# Step Note — fiction_jobs_concurrency_refactor — 2025-12-08 17:05

## Goal
Deepen analysis of fiction job concurrency and outline code changes for atomic claims/recovery without locks.

## Context
- Examined `FictionWeaverJobs` (phase execution and lore fulfillment) and `FictionBacklogScheduler` (claiming and auto-resume).
- Unique indexes exist (plan+backlogId, checkpoint phase, world bible slug/version) but no atomic claim or failure recovery; concurrent schedulers can double-enqueue or strand InProgress items.
- Direct `DbContext` use in jobs risks stale tracking and silent overwrites.

## Commands Executed
- `rg "class FictionWeaverJobs" -n`
- `Get-Content src/Cognition.Jobs/FictionWeaverJobs.cs`
- `Get-Content src/Cognition.Jobs/FictionBacklogScheduler.cs`
- `Get-Content src/Cognition.Data.Relational/Modules/Fiction/Config.cs -First 320/-Tail 120`

## Files Touched
- plans/fiction_jobs_concurrency_refactor_step_20251208_1705_analysis.md (this note)

## Findings / Issues
- Backlog claim is non-atomic: read Pending → mark InProgress → enqueue; two schedulers can race and both enqueue the same item.
- Auto-resume flips stale InProgress to Pending without guard; concurrent reclaim can conflict.
- Lore fulfillment can double-run: eligibility check then insert world-bible entry; concurrent jobs can both create entries or throw on unique slug/version.
- No failure handler to move InProgress → Failed/Pending on exceptions; jobs can leave stranded rows.
- Direct DbContext tracking means ExecuteUpdate/atomic patterns must avoid stale tracked entities.

## Decisions
- Proceed with guarded updates (no locks): use `ExecuteUpdateAsync` with status predicate to claim/requeue backlog items; reload after claim for further updates.
- Add failure-path resets in phase jobs: on exception, set backlog/task/checkpoint to Failed (or Pending retry) before rethrow.
- Add lore fulfillment guard: early claim + catch unique conflict and re-read to avoid duplicate insert.
- No schema change yet; prefer guarded updates first. RowVersion is optional if guard proves insufficient.

## Completion
- Status: ☐ (analysis only; no code changes yet)

## Next Actions
- Implement guarded claim in `FictionBacklogScheduler`: atomic Pending→InProgress, enqueue only when claim succeeds; guard auto-resume.
- Add failure handler in `FictionWeaverJobs` to reset InProgress on exceptions.
- Add lore fulfillment duplicate guard and/or unique-violation catch.
- Add focused tests for double-claim and failure recovery. 
