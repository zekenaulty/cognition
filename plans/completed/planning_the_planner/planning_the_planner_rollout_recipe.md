# Planner Rollout Recipe

This recipe documents the end-to-end checklist for migrating a planner onto `PlannerBase`, seeding prompts, wiring telemetry/alerts, and rolling the change through lower environments. Treat it as the canonical runbook for future fiction and non-fiction planners.

## Definition of Done
- Every planner migration walks through Sections 1-4 with evidence captured in a step note: code diff, seeded templates, updated pipeline tests, and Ops alert validation.
- Lower environments (dev + staging) demonstrate healthy `PlannerHealthReport` entries for the new planner plus a synthetic Ops alert acknowledged by the target channel.
- README / plan docs and rollout matrix entries link back to this recipe so future teams can follow it without tribal knowledge.

## 1. Code Preparation

1. **Baseline**  
   - Confirm `PlannerBase`, telemetry (`IPlannerTelemetry`), transcript store, and template repository are already wired (check `Cognition.Clients` service registration).
   - Ensure `FictionPlannerPipelineTests` (or an equivalent scripted harness) has deterministic responses for the new planner phase.

2. **Planner Implementation**  
   - Create `<PlannerName>PlannerParameters` + `<PlannerName>PlannerTool` deriving from `PlannerBase<TParams>`.  
   - Define metadata (`PlannerMetadata`) with the appropriate capability tags and step descriptors.  
   - Implement prompt helpers (template-backed + fallback), call `IAgentService.ChatAsync`, and capture artifacts/diagnostics/metrics.  
   - Add transcripts with metadata (scene/scroll IDs, validation status) so jobs and dashboards can surface context.

3. **Runner Migration**  
   - Update the corresponding `IFictionPhaseRunner` to resolve the new planner via DI, build a `PlannerContext`, and convert `PlannerResult` back into `FictionPhaseResult`.  
   - Thread backlog metadata (`backlogItemId`) through the conversation state and ensure transcripts retain the planner outcome.

4. **Template Seeding**  
   - Add a constant prompt template to `StartupDataSeeder.EnsurePlannerPromptTemplatesAsync` and include it in the seeding list.  
   - Extend integration/unit tests that rely on the in-memory template repository.

5. **Pipeline Regression**  
   - Update `FictionPlannerPipelineTests` to instantiate the new planner tool, provide a scripted response, and assert transcript metadata/backlog completion for the new phase.

6. **Ops Alerting**  
   - Extend `OpsWebhookAlertPublisher` behaviour/tests when new metadata is introduced.  
   - Validate configuration through `OpsAlertingOptionsValidator` to fail fast if `Enabled` is true but no webhook is configured or SLO thresholds are invalid.

## 2. Documentation & Developer Notes

1. Refresh `README.md` with:
   - The new planner template identifier.
   - Ops alerting configuration examples (routing overrides, SLO thresholds, validation guarantees).

2. Update `plans/planning_the_planner.md` current status and next steps to reflect the migration.

3. Capture a step note (`plans/planning_the_planner_step_YYYYMMDD_xx_<slug>.md`) summarising:
   - Commands executed (tests, builds).
   - Files touched (planner tool, runner, seeder, tests, docs).
   - Outstanding follow-ups or risks discovered during the migration.

## 3. Lower Environment Rollout

> Run these steps per environment (dev → staging → production). Track evidence (screenshots, logs) in the step note.

1. **Deploy Artifacts**  
   - Build/publish the API and jobs workloads with the new planner binaries.

2. **Database & Seeder**  
   - Apply EF Core migrations (none expected after the initial planner framework).  
   - Execute `StartupDataSeeder.SeedAsync` (via API start-up or manual invocation) and confirm the new `planner.fiction.<slug>` template exists in `PromptTemplates`.

3. **Configuration**  
   - Populate `OpsAlerting` settings (`WebhookUrl`, overrides, SLO thresholds).  
   - Verify startup logs show `OpsAlertingOptions` validation succeeded; failures block boot and should be resolved before proceeding.

4. **Smoke Tests**  
   - Hit `/api/diagnostics/planner` and confirm `PlannerHealthReport` lists the new planner template and no alert regressions.  
   - Run the scripted pipeline job (or execute each phase via Hangfire/Rebus triggers) to ensure backlog checkpoints and transcripts close as expected.  
   - Validate console telemetry (`Operations → Backlog Telemetry`) reflects the new phase with backlog coverage and alert metadata.

5. **Ops Verification**  
   - Trigger a synthetic planner alert (e.g., force a stale backlog entry) and confirm the webhook payload contains routing/SLO metadata.  
   - Confirm Ops recipients acknowledge receipt and routing (Slack channel, PagerDuty service, etc.).

6. **Sign-off**  
   - Update the environment checklist in `plans/planning_the_planner.md` (or associated step note) with the deployment date, verification artefacts, and owner sign-off.

## 4. Post-Rollout Follow-ups

- Monitor planner health dashboards for 24–48 hours to catch flapping or stale backlog regressions.  
- Schedule backlog grooming or model tuning if critiques/self-critique thresholds trigger frequently.  
- Queue non-fiction planner audits once fiction pipeline stability is confirmed.  
- Remove legacy runner scaffolding associated with the migrated planner once rollout is complete and archived.

## Quick Reference Checklist

- [ ] Planner tool + parameters derived from `PlannerBase`.  
- [ ] Runner uses planner, builds `PlannerContext`, converts `PlannerResult`.  
- [ ] Prompt template seeded (`StartupDataSeeder`) and available in tests.  
- [ ] Pipeline tests updated with deterministic response + assertions.  
- [ ] Ops validator passes (webhook + SLO thresholds).  
- [ ] README / plan docs updated.  
- [ ] Step note recorded.  
- [ ] Lower env deployment validated (planner diagnostics + Ops webhook).  
- [ ] Legacy code scheduled for cleanup once rollout is stable.
