
Goal
- Begin Milestone A inventory: capture Python prompts, validators, retry patterns, emitted artifacts, and Cognition prompt scaffolds.

Context
- Repository snapshot already available; focus on cataloging orchestration specifics that inform later prompt templating and data migration work.

Commands Executed
- python -c "..." (AST walks to extract prompt text from `iterate-book.py`).
- Appended documentation via PowerShell (BuildBeatsPrompt/BuildDraftPrompt/etc.).

Files Changed
- plans/fiction/phase-001/milestone-a_inventory.md

Tests / Results
- Documentation task only; no automated tests executed.

Issues
- Provider-specific work deferred; prioritize OpenAI/Gemini prompts. Confirm context/token details later.

Decision
- Inventory document now captures Python workflow prompts/validators/retries/outputs plus Cognition fiction tool prompts and usage.

Completion
- Complete (updated 2025-10-05)

Follow-Up Tasks
- [ ] Refactor `FictionWeaverJobs` + Hangfire routing so backlog items drive the entire phase sequence (no hard-coded calls) and extend pipeline regression coverage (outside review 2025-11-01).
- [ ] Audit remaining prompts outside fiction tools (e.g., remember/promotion flows) for completeness (track under Phase 001 Milestone A).
- [ ] Thread `backlogItemId` metadata documentation update with telemetry screenshots (pending write-up).
- [ ] Review a `_context` snapshot to verify the file-output list and capture anomalies (file new issue if discrepancies persist).
- [ ] Verify the front-end persists `fictionPlanId`/provider/model metadata before the next user turn so auto-resume triggers reliably (coordinate with UI team).
- [ ] Land the `FictionPlanCheckpoint` lock manager migration, add resume/cancel regression tests, and ensure backlog status flips respect checkpoint failures.
- [ ] Capture per-phase token budgets (vision/iterative/architect/scroll/scene), implement soft-stop `Partial` outcomes, and persist token metrics alongside backlog snapshots.


Update (data modeling)
- Scaffolded EF POCOs and configuration mappings for FictionPlan graph (plan, passes, blueprints, scrolls, sections, scenes, checkpoints, transcripts, story metrics, world bible).
- Added navigation from FictionProject to FictionPlans to support new relationships.


Legacy tooling plan
- Outline existing fiction POCOs/tools; mark which will be replaced by plan graph vs polished.

Phase runner modeling
- Drafted Hangfire job plan for VisionPlanner -> SceneWeaver with AgentService conversation requirements (plans/fiction/phase-001/hangfire_jobs.md).
- Added weaver runner skeletons, context, and result types under `src/Cognition.Clients/Tools/Fiction/Weaver`.
- Implemented Hangfire job wrappers composing phase runners with checkpoint locking/persistence (see `src/Cognition.Jobs/FictionWeaverJobs.cs`); jobs now write `FictionPlanTranscript` rows and publish `FictionPhaseProgressed` bus/SignalR notifications with branch-aware cancel/resume flows.
- Hangfire job wrappers now enqueue all fiction phases with checkpoint persistence and progress signaling (see [Hangfire job matrix](hangfire_jobs.md)); integration validation and replay tests remain outstanding follow-ups.

Vision planner backlog alignment
- Vision planner prompt now returns a dynamic `planningBacklog` scaffold (instead of a finished outline) paired with book goals and author summary.
- Backlog items enumerate pending planner passes (e.g., outline arcs, map conflicts) so the iterative pipeline from `reference/iterate-book.py` can fill them through blueprint/scroll runners.
- Schema change captured in `FictionResponseValidator.BuildVisionSchema`; seeder/template updates ensure lower environments adopt the backlog-based contract.
- `FictionWeaverJobs` now persists backlog state via `FictionPlanBacklogItem`, automatically flips backlog items to in-progress/complete (or back to pending on failure), and downstream phases echo `backlogItemId` across phase contexts/results so the updated contract (and telemetry) is ready for documentation.
