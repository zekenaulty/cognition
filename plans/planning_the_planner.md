# Planning the Planner Framework

Objective
- Design a reusable planner framework that standardises the lifecycle (context → plan → evaluate → execute → finalise) for fiction tooling and future orchestrators.
- Define `IPlannerTool`, `PlannerBase`, and supporting contracts so planners are metadata-driven, testable, and compose cleanly with existing tool infrastructure.
- Outline migration steps to refactor in-flight fiction planners onto the shared foundation without stalling feature work.

Current Status (2025-10-13)
- Planner contracts and base class are implemented; Vision planner runs through `PlannerBase` with transcript/metric capture.
- Planner executions now persist via `PlannerTranscriptStore` using the new `planner_executions` table, and the startup seeder provisions the canonical vision planner prompt template.
- Vision planner prompt now yields a dynamic `planningBacklog` (rather than a finished outline) so downstream phases can iteratively fill gaps, matching the iterative flow captured in `reference/iterate-book.py`.
- Planner-focused tests cover capability lookup, Vision planner orchestration, transcript persistence, template resolution, and telemetry redaction.
- Outstanding work includes rolling the migration/template seeding through lower environments, migrating the next planner, and documenting rollout guidance.
- Third-party review (2025-10-14) highlighted three priorities: (1) guard rails for template availability and self-critique budgets, (2) a planner health/diagnostics surface, and (3) backlog metadata plumbing through the dispatcher + fiction runners.

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
    2. `PrepareContextAsync(PlannerContext ctx, TParams parameters)` → loads required scope data, persona traits, prior outputs.
    3. `DraftPlanAsync` (virtual) → optional LLM call creating plan skeleton (`IEnumerable<PlannerStepInstance>`).
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
- `PlannerContext` exposes `ScopePath ForAgent(Guid agentId)` methods to produce child scopes (e.g., `agent → scene`).
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
- Divergent behaviours across planners → start with pilot migration and retrofit base class as needed before broad rollout.
- Self-critique or evaluation loops increasing token cost → add configuration to disable or adjust per planner/per persona.
- Feature gap between base and bespoke logic → maintain extension hooks (virtual methods, delegates) to allow unique behaviour without forking base class.
- Adoption risk across teams → provide developer guide and recipe; pair program first migration to share knowledge.

Open Questions
- Should planner metadata live in code (attributes/fluent builder) or external JSON for hot reload? (Initial recommendation: code-first for type safety; revisit once stable.)
- Do we need a state machine for complex branching (e.g., nested planners)? Possibly in future if fiction planners need recursion; design base class with optional stack but keep scope minimal now.
- How to present transcripts in the UI? (Coordinate with API team once telemetry sinks confirm.)

Next Steps
1. Deploy `20251013181358_PlannerExecutions`/`AddFictionPlanBacklog` migrations and run the seeder in lower environments; verify transcripts and backlog entries persist, and archive screenshots in the rollout log.
2. Wire planner health/diagnostics endpoint surfacing planner registrations, template availability, backlog counts, last failures, and token metrics; expose `planner.*` telemetry streams for dashboards.
3. Enforce template availability: make `PlannerBase` throw when a declared `StepDescriptor` template is missing, and add tests that resolve every template id through `IPlannerTemplateRepository`.
4. Add self-critique budget controls (metadata + runtime guard) and record critique token usage in planner telemetry; default to disabled unless explicitly enabled per planner/persona.
5. Automate backlog metadata plumbing: dispatcher must always propagate `backlogItemId`, and fiction phase runners should consume/update statuses without manual args.
6. Migrate the next fiction planner (iterative/story) onto `PlannerBase`, capture parity results with ScriptedLLM, and update the dev recipe before broader rollout.
7. Publish planner rollout guidance covering telemetry expectations, prompt layout, backlog usage, and QA signoff requirements once the above foundations are in place.
