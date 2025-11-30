# Fiction Phase 001 Closeout (Pre-Alpha)

## Objective
- Convert the fiction pipeline from “mostly there” to a pre-alpha that authors can run end-to-end without handholding: backlog-driven orchestration, lore/character lifecycle with provenance, and console visibility/alerts that match reality.

## Current Status (grounded in code/tests)
- End-to-end pipeline: `FictionPlannerPipelineTests.cs`, `FictionResumeRegressionTests.cs`, and `FictionWeaverJobCancellationTests.cs` cover phase runs, resume, and cancellation; `FictionBacklogSchedulerTests.cs` validates backlog scheduling.
- Backlog/resume contract: `BacklogContractTests.cs` + `20251123152156_AddConversationTaskMetadata` enforce provider/model/agent/task metadata; API exposes backlog/resume endpoints; console wiring exists (`FictionResumeBacklogDialog.tsx`, `FictionBacklogPanel.tsx`, `PlannerTelemetryPage.tsx`).
- Plan creation/console: wizard dialog present (`FictionPlanWizardDialog.tsx`); backlog panels and action logs render in Planner Telemetry.
- Persona obligations: entity/migration (`20251116202103_AddFictionPersonaObligations`), controller handlers/tests (`FictionPlansControllerTests`), and console dialogs (`PersonaObligationActionDialog.tsx`) exist.
- Character/lore lifecycle: `CharacterLifecycleService` updates characters, links to world-bible entries when present, appends persona memories; runners gate on lore requirements (validation tests in jobs).
- Gaps: world-bible provenance/persona linkage still thin (`FictionWorldBibleEntry` lacks persona/agent provenance; `CharacterLifecycleService` does not mint bible entries); branch-aware roster/bible lineage not persisted; console lacks explicit SLO/alert surfacing for backlog/lore drift; plan creation/resume flows lack e2e UI/API regression; ops/alert fanout for fiction not verified.

## Definition of Done
- Backlog/resume: console and API flows verified end-to-end (tests) with required metadata; action logs and alerts render; no DB edits needed to start/resume plans.
- Lifecycle: tracked characters/lore auto-mint world-bible entries with persona/agent provenance and branch lineage; roster/bible views surface provenance in API + console.
- Obligations/lore fulfillment: obligations tied to backlog/tasks with audit history; lore fulfillment automation records provenance and updates bible entries; console shows status/history.
- Telemetry/alerts: PlannerHealth/backlog telemetry exposes backlog/lore/obligation drift with webhook-capable alerts (reuses ops publisher); SLO-ish signals (age, retries) visible in console.
- Tests/docs: regression/API/UI tests cover the above; docs updated; work logged per `plans/README.md`.

## Scope
- Fiction Phase 001 pipeline, backlog/resume, character/lore lifecycle, persona obligations, console views, and fiction-specific telemetry/alerts.
- Pre-alpha only (single dev environment; no staging/prod choreography).
- Out of scope: HGTF/Sandbox plumbing (handled elsewhere), non-fiction planners.

## Deliverables
- World-bible provenance: schema updates + lifecycle code to mint/link entries to personas/agents/branches with provenance metadata; roster/bible APIs updated.
- Backlog/resume closeout: API + console e2e coverage for plan creation, backlog actions, resume, and alerts.
- Telemetry/alerts: backlog/lore/obligation drift surfaced in PlannerHealth/console with webhook alert routes wired.
- Tests: API/regression/UI and lifecycle tests; updated fixtures for roster/bible provenance.
- Docs: developer/operator notes for running the fiction loop end-to-end; updated plan/lifecycle docs.

## Workstreams
1) **World-Bible & Roster Provenance**
   - Add provenance fields (persona/agent, branch, source pass/task) to world-bible entries and roster responses; migrate existing rows.
   - Extend `CharacterLifecycleService` (and runners) to create/update bible entries when tracking characters/lore; persist provenance; tests for mint/update paths.
   - Expose provenance in API responses; add console roster/bible views to display lineage.
2) **Backlog/Resume & Console E2E**
   - Add regression tests for plan creation → backlog seed → resume → scheduler → lore job loop (API + UI where feasible).
   - Ensure console backlog/actions/alerts reflect API state (age, missing metadata, obligations, lore status); add drift/blocked banners.
   - Harden action log/audit payloads and ensure resume metadata (provider/model/agent/task) is user-visible.
3) **Lore Fulfillment & Obligations**
   - Wire fulfillment automation to update bible entries with provenance and audit history; ensure blockers clear before scroll/scene.
   - Obligation flows: enforce source backlog/task linkage, resolution notes, and drift flags; add tests and console surfacing.
4) **Telemetry/Alerts**
   - Pipe backlog/lore/obligation drift into PlannerHealth; add webhook alert routes (reuse ops publisher) with fiction-specific payloads.
   - Add console telemetry widgets/SLO chips (age/retry/backlog drift) and validate via tests.

## Testing / Verification
- API/regression: backlog/resume, plan creation, obligations, fulfillment, provenance payloads.
- Lifecycle: unit/integration for `CharacterLifecycleService` mint/update + provenance; scroll/scene gating on lore.
- UI: console backlog/resume/alerts/roster provenance rendering (pre-alpha manual + automated where possible).
- Telemetry/alerts: assert PlannerHealth/backlog telemetry and webhook payloads include fiction data.

## Risks / Mitigations
- Schema churn: migration + rollback script; keep provenance additive where possible.
- Test flake: keep fixtures deterministic; reuse in-memory DB patterns from existing tests.
- UI drift vs API: align types/DTOs and add small smoke tests.

## Worklog Protocol
- Follow `plans/README.md`: step notes at `plans/fiction_phase_001_closeout_step_YYYYMMDD_HHMM_<topic>.md` with goal/context/commands/files/tests/issues/decision/completion/next actions.
- Capture scope/context changes as you work (static RAG in `plans/`) so later sessions can anchor.
