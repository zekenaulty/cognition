# Planning the Planner Framework

Objective

- Design a reusable planner framework that standardises the lifecycle (context -> plan -> evaluate -> execute -> finalise) for fiction tooling and future orchestrators.
- Define `IPlannerTool`, `PlannerBase`, and supporting contracts so planners are metadata-driven, testable, and compose cleanly with existing tool infrastructure.
- Outline migration steps to refactor in-flight fiction planners onto the shared foundation without stalling feature work.

Current Status (2025-10-26)

- PlannerBase plus shared telemetry/transcript infrastructure are stable; vision, iterative, chapter architect, scroll refiner, and scene weaver planners now derive from the base class with seeded templates (`planner.fiction.vision`, `planner.fiction.iterative`, `planner.fiction.chapterArchitect`, `planner.fiction.scrollRefiner`, `planner.fiction.sceneWeaver`).
- `FictionPlannerPipelineTests` exercises the scripted vision -> iterative -> architect -> scroll -> scene flow and verifies backlog checkpoints, transcripts, and metadata end-to-end.
- Planner health diagnostics fuse telemetry/backlog heuristics into `PlannerHealthReport.Alerts`, and the Ops webhook publisher routes alerts per ID/severity with optional SLO thresholds and fails fast if no webhook is configured.
- The console backlog telemetry view consumes the richer alert payloads, and Ops configuration (`OpsAlerting`) now exposes routing + debounce knobs for internal alpha validation; configuration validation guards common misconfigurations (missing webhook, non-positive SLO thresholds).
- We remain in alpha with only seed data; no production rollout or lower-environment choreography is required. Remaining effort concentrates on future planner migrations, non-fiction coverage, and tightening developer guidance rather than environment cutovers.
- Third-party review items (template guard rails, planner health endpoint, backlog instrumentation) are closed; focus shifts to completing migrations and codifying rollout recipes.
- External security/observability review (2025-10-27) scored overall alpha readiness at 7.7/10 and introduced P0 blockers (rate limiting, ScopePath factory lock, authorization policies, planner budgets/quotas, correlation IDs); see `plans/alpha_security_observability_hardening.md` for remediation sequencing.

Scope

- Interfaces, base classes, and metadata DSL for planner-oriented tools living under `Cognition.Clients.Tools`.
- Shared planner context/result types, transcripts, telemetry events, and dependency injection wiring.
- Extensions to `ToolRegistry`, `ToolDispatcher`, and retrieval helpers so planners integrate seamlessly with scope-aware contexts and RAG.
- Refactoring path for fiction tools (vision planner, memory weaver, narrative builders) onto the new framework.
- Testing harness updates (shared fakes, scripted LLM, deterministic runners) to cover planner flows.

Out of Scope

- UI / surface area changes (dashboards, visual planners).
- Non-fiction orchestrators unless they share the same lifecycle (will be evaluated once framework stabilises).
- Full documentation for end users (covered in later iteration once code lands).

Planner Framework Design

Core Contracts

- `PlannerMetadata`
  - `Name`, `Description`, `Capabilities` (e.g., `["planning","fiction","iteration"]`).
  - `InputParameters`: strongly typed descriptors (name, type, required, default, scope hints).
  - `DefaultSettings`: e.g., temperature, max iterations, evaluation mode.
  - `StepDescriptors`: metadata for declared steps (id, label, purpose, expected inputs/outputs, optional prompt template id).
  - `TelemetryTags`: default tags for logging/analytics.
- `IPlannerTool`
  - Extends `ITool`.
  - Returns `PlannerResult`.
  - Exposes `PlannerMetadata Metadata { get; }`.
  - Signature: `Task<PlannerResult> PlanAsync(PlannerContext context, PlannerParameters parameters, CancellationToken ct = default)`.
- `PlannerContext`
  - Inherits `ToolContext` but adds planner-specific fields: `ScopePath`, `PrimaryAgentId`, `ConversationState`, `Env`, `SupportsSelfCritique`.
  - Access to retrieval helpers (vector search, memory fetch), optionally as lazy delegates to avoid pulling heavy dependencies when unused.
- `PlannerParameters`
  - Typed wrapper over dynamic args (dictionary) with helper methods for validation and strongly typed retrieval.
- `PlannerStep`
  - `Id`, `DisplayName`, `Purpose`, `InputKeys`, `OutputKeys`, `AllowParallel`, `RetryPolicy`.
  - Hooks to inject step-specific prompt templates and evaluation heuristics.
- `PlannerOutcome`
  - Enum: `Success`, `Partial`, `Failed`, `Cancelled`.
- `PlannerResult`
  - `Outcome`, `CompletedSteps`, `OutstandingSteps`, `Artifacts` (structured outputs), `Transcript` (messages exchanged), `Metrics` (duration, token usage), `Diagnostics` (warnings/errors).
  - Helper methods: `WithArtifact`, `Combine`, `ToLogEntry`.

Planner Base Class

- `abstract class PlannerBase<TParams> : IPlannerTool where TParams : PlannerParameters`
  - Provides final implementation of `PlanAsync`.
  - Workflow:
    1. `ValidateInputs(TParams parameters)` (abstract/protected virtual).
    2. `PrepareContextAsync(PlannerContext ctx, TParams parameters)` -> loads required scope data, persona traits, prior outputs.
    3. `DraftPlanAsync` (virtual) -> optional LLM call creating plan skeleton (`IEnumerable<PlannerStepInstance>`).
    4. `ExecutePlanAsync` loops steps:
       - `BeforeStepAsync` (virtual hook).
       - `ExecuteStepAsync` (abstract or requires derived class to wire into tool/LLM).
       - `EvaluateStepAsync` (virtual) for scoring/self-criticism (LLM or heuristics).
       - `AfterStepAsync`.
       - Handles retry policy, branching, conditional skip.
    5. `FinalizeAsync` (virtual) to consolidate outputs.
  - Built-in features:
    - Cancellation token propagation.
    - Max iteration guard (configurable via metadata / parameters).
    - Automatic logging: step start/end, outcome, token metrics.
    - Transcript capture via centralized `PlannerTranscript` service.
    - Telemetry events (structured; can plug into Application Insights or custom sinks).
  - Extension points:
    - `UseSelfCritique(Func<PlannerStepExecutionContext, Task<SelfCritiqueResult>> evaluator)`.
    - `UseFallbackPlan(Func<PlannerContext, PlannerParameters, Task<IEnumerable<PlannerStepInstance>>>)`.
    - `OverridePromptTemplate(string stepId, PromptTemplate template)`.

Metadata-Driven Behaviour

- Step metadata drives default prompts: base class pulls prompt template id from metadata, fetches actual text from `ITemplateRepository`.
- Capability tags align with `ToolRegistry` so planners can be discovered as `Capability == "planning"` plus domain tags.
- `PlannerMetadata` can include `PrerequisiteScopes`: e.g., `["agent","persona"]`. Base class enforces that the context contains those segments before running.
- `PlannerSettings` can be provided via JSON (persisted per client/persona) and merged with defaults at runtime.

Supporting Types

- `PlannerStepInstance`
  - Concrete instance produced by `DraftPlanAsync`.
  - Contains step metadata id, concrete instructions, dependencies, planned inputs and outputs.
- `PlannerTranscript`
  - Collection of entries (timestamp, actor, channel, content, token counts).
  - Supports nested steps (indentation or hierarchical IDs) for visualisation.
- `SelfCritiqueResult`
  - `IsApproved`, `Notes`, `SuggestedChanges`.
- `PlannerMetrics`
  - `Duration`, `TokenUsage`, `ToolCalls`, `Retries`, `Errors`.

TOOL INTEGRATION

Registry updates

- `ToolRegistry` gains optional `CapabilityIndex` to map capability tags to tool paths (for dispatching planners by tag).
- Planners register metadata via static hints or attribute (e.g., `[Planner("fiction.story", Capabilities = "planning,fiction,orchestration")]`).
- `ToolDispatcher` detects planner outputs and handles transcript + metrics logging differently (e.g., storing hierarchical step logs).
- Add convenience helper `IToolDispatcher.PlanAsync<TPlanner>(...)` for DI-friendly invocation.

Scope Integration

- Planner context uses new `ScopePath` helpers to guarantee consistent identity across plan steps and down-stream tool calls.
- `PlannerContext` exposes `ScopePath ForAgent(Guid agentId)` methods to produce child scopes (e.g., `agent -> scene`).
- Planner base ensures every tool invocation in the plan inherits the correct `ScopeToken`, preventing context bleed.
- Retrieval helpers (vector store, memory recall) accept canonical scope so plan steps resolve deterministic data.

Telemetry & Logging

- Standard events:
  - `planner.started`, `planner.step.started`, `planner.step.completed`, `planner.completed`, `planner.failed`.
  - Properties: planner id, persona/agent, scope path, step id, iteration, tokens consumed, decision outcome.
- Provide adapter for existing `ToolExecutionLog` so planner steps produce aggregated log entries plus raw transcripts stored separately.
- Optional: output to `PlanTimeline` table for UI (deferred until later).

Testing Strategy

- Unit tests for:
  - Metadata validation (missing persona/agent requirement, invalid segments).
  - Planner base hooks (retry logic, cancellation, transcript capture).
  - Self-critique extension.
  - Step ordering and dependency resolution.
- Integration tests using `ScriptedLLM`, `FakeClock`, `InMemoryVectorStore`:
  - Should simulate plan draft, evaluation, and execution.
  - Verify metrics, transcripts, artifact outputs.
- Snapshot tests for plan artifacts and transcripts (use deterministic fakes).
- Provide test harness `PlannerTestHost` that wires fake DI container, registry, and scripted behaviors.

Migration Plan for Fiction Tools

Current State

- Fiction tools (vision planner, narrative generators, memory orchestrators) contain bespoke planner/steps logic.
- Each tool manages prompts, tool invocation, logging, and state differently.
- Context management relies on the old scope token shape; switching to path-based identity requires rewiring.

Refactor Strategy

1. **Audit & Component Extraction**
   - Survey existing fiction planners to identify shared phases: context collection, plan drafting, step execution, evaluation.
   - Catalogue prompts, tool dependencies, and explicit control flow.
   - Map each to candidate `PlannerStep` definitions.

2. **Introduce Planner Base**
   - Implement `IPlannerTool`, `PlannerBase`, metadata structures.
   - Create shared utilities (prompt repository, transcript logging, evaluation helpers).
   - Deploy base class with feature flag to avoid immediate migration.

3. **Adapter Layer**
   - Build thin adapters for each in-flight planner converting existing parameters into `PlannerParameters`, existing logic into step overrides.
   - Start with the smallest/least critical planner (e.g., `ScriptedScenePlanner`) to verify the approach.

4. **Iterative Migration**
   - For each planner:
     - Create a derived class from `PlannerBase`.
     - Migrate prompts to template repository (metadata references).
     - Replace manual loops with base class execution hooks.
     - Update tests to use new harness.
   - Run cross-check using scripted LLM to ensure output parity.
   - Document deviations (e.g., new metrics).

5. **Scope Alignment**
   - once path-based scope lands, update `PlannerContext` usage to call new scope helpers.
   - Ensure plan steps annotate artifacts with canonical path (for later retrieval).

6. **Cleanup**
   - Remove legacy planner scaffolding once all fiction planners adopt base class.
   - Update `ToolRegistry` metadata for planners to use consistent capability tags.
   - Document new extension patterns (self-critique, fallback).

Risk & Mitigation

- Divergent behaviours across planners -> start with pilot migration and retrofit base class as needed before broad rollout.
- Self-critique or evaluation loops increasing token cost -> add configuration to disable or adjust per planner/per persona.
- Feature gap between base and bespoke logic -> maintain extension hooks (virtual methods, delegates) to allow unique behaviour without forking base class.
- Adoption risk across teams -> provide developer guide and recipe; pair program first migration to share knowledge.

Open Questions

- Should planner metadata live in code (attributes/fluent builder) or external JSON for hot reload? (Initial recommendation: code-first for type safety; revisit once stable.)
- Do we need a state machine for complex branching (e.g., nested planners)? Possibly in future if fiction planners need recursion; design base class with optional stack but keep scope minimal now.
- How to present transcripts in the UI? (Coordinate with API team once telemetry sinks confirm.)

Next Steps

1. Catalogue non-fiction planners (and adjacent orchestrators) that should adopt `PlannerBase`, outlining migration prerequisites and expected template inputs.
2. Deprecate legacy runner scaffolding now that fiction planners have migrated; tighten CI/lint gates around planner template configuration and scripted pipeline coverage.
3. Explore multi-channel Ops publishing (e.g., Slack + PagerDuty) and acknowledgement workflows once webhook payloads stabilise under alpha usage.
4. Extend planner health dashboards with additional alert drill-downs/SLA visualisations to support upcoming planner additions.
5. Track world-bible telemetry gaps: add PlannerHealth alerts/Ops routing for missing or stale lore snapshots and ensure console filters surface the warning (see step note `planning_the_planner_step_20251102_0930_world_bible_alerts`).

Alpha Hardening Requirements (2025-10-27)

- Enforce API rate limits, per-principal quotas, request size caps, and correlation logging to cover planner/job workflows (plans/alpha_security_observability_hardening.md).
- Lock ScopePath construction to shared factories and add analyzers/tests guarding against ad-hoc instantiation (plans/scope_token_path_refactor.md, plans/alpha_security_observability_hardening.md).
- Introduce planner token budgets, throttle-aware Hangfire scheduling, and telemetry (`planner.throttled`, `planner.rejected`) so Ops dashboards can surface quota hits.
- Validate planner templates at startup (metadata -> seeded template), gate DTO input with FluentValidation/DataAnnotations, and extend PlannerHealth to track latency percentiles, retries, and critique usage.
- Guard OpenSearch mapping/schema drift at boot; refuse to start when the pipeline schema diverges from expected shape.

Planner Migration Guidance (Updated 2025-10-21)

- Full rollout recipe: `plans/planning_the_planner_rollout_recipe.md` (source of truth for checklist + lower-env runbook).
- `FictionPlannerPipelineTests` now assert backlog state, checkpoint completion, and transcript metadata across the vision -> scene flow. Update the scripted responses/templates in that harness before migrating any new planner so we keep parity evidence in CI.
- Every migrated planner must build its `ScopePath` via `IScopePathBuilder` and thread backlog metadata (`backlogItemId`) through `PlannerResult`, transcripts, and telemetry so the jobs layer can flip statuses automatically.
- Record a rollout note that captures: (a) scripts/fakes used for parity, (b) critique budget defaults, and (c) required backlog inputs/outputs. These notes seed the README guidance and unblock downstream tooling.
- Run the planner health endpoint after each migration to confirm template availability, backlog freshness, and `planner.*` telemetry.

Pipeline Completeness Focus

- Verify the end-to-end fiction planner flow (vision -> iterative -> architect -> scroll -> scene) produces actionable artifacts and automatically advances backlog state without manual intervention.
- Shore up any missing runner integrations or persistence paths discovered during the audit before revisiting additional observability work.
- DONE Added multi-phase regression (`FictionPlannerPipelineTests`) that scripts planner responses and asserts backlog items close as the pipeline progresses.

Telemetry Updates (Backlog Signals)

- DONE Confirm ingestion: `FictionPhaseProgressed` events, plan notifier payloads, and stored transcripts now surface `backlogItemId`, enabling backlog-centric analytics.
- DONE Add counters: backlog transitions are logged per phase and exposed through planner health `RecentTransitions` for dashboard aggregation.
- DONE Planner health report now exposes per-plan backlog coverage plus transcript snippets/message ids for recent planner failures, giving dashboards/linkouts without digging through storage.
- Surface dashboards: update the planner health view to display backlog coverage per phase, highlight stuck items (no completion within SLO), and expose most-recent transcript links for failed backlog executions.
- Alerting outline: wire a lightweight monitor that pages when a backlog item flips back to pending more than N times within 24h, signalling persistent execution failure.
