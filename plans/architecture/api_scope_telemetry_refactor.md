# API Scope & Telemetry Refactor (Architecture)

## Objective
Keep API controllers tight, SOLID, and surface-aware; enforce ScopePath/ScopeToken discipline end-to-end; standardize telemetry contracts; and shrink oversized UI/pages by enforcing component separation.

## Scope
- **In:** API surface delineation (public/admin/internal), scope guardrails (IScopePathBuilder usage), telemetry contract + ingestion for planner/backlog/tools, controller slimming (DDD boundaries), frontend page/component modularity guidance.
- **Out:** Feature adds, auth provider changes, and non-API telemetry (e.g., LLM provider metrics).

## Why Now
- Controllers still mix admin/console/user flows.
- Scope usage is uneven (manual filters, TODOs).
- Telemetry is ad-hoc outside backlog/planner.
- Frontend pages drifted to 1–3k lines; we are already breaking them apart (e.g., PlannerTelemetryPage, FictionProjectsPage).

## Definition of Done
- Surfaces are documented and enforced (public vs admin vs internal routes) with lint/guard rails.
- Scope enforcement: data access and tool dispatch go through ScopePath factory; analyzer or tests fail on raw paths/filters.
- Telemetry contract doc + helper shipped; planner/backlog/tool events emit required fields; ingestion wiring in place.
- Controllers slimmed: orchestration in services; DTO validation clean (no record-constructor warnings).
- Frontend guidance: pages kept small via hooks/components; plans/README.md updated to require modularity across C# and React/TS.

## Deliverables
- Docs: surface map, scope usage guide (SQL/OpenSearch/tools), telemetry contract.
- Guardrails: analyzer/tests for ScopePath, route placement checklist, DTO validation guard.
- Refactors: representative controllers slimmed (Chat/Conversations/Planner/Persona admin), data services for author personas/world-bible access.
- Frontend: short guideline in plans/README.md on page-size limits and component extraction; identify and refactor remaining 1k+ line pages.

## Plan (phased)
1) **Inventory & guardrails**
   - Map controllers by surface; note violations.
   - Add failing test/analyzer for raw ScopePath/filters; add DTO validation guard (constructor params).
   - Document surface/versioning conventions (v1 vs /admin vs /internal).
2) **Scope discipline fixes**
   - Patch hot spots (retrieval, tool dispatcher, planner) to require IScopePathBuilder/ScopePathFactory.
   - Add scoped telemetry helper to stamp scope/correlation consistently.
3) **Telemetry contract**
   - Define minimal contract (event ids/fields) for planner/backlog/tool alerts; add helper/builder.
   - Ensure ingestion (API or job) stores/forwards to dashboards.
4) **Controller slimming & DDD alignment**
   - Extract services (e.g., AuthorPersonaStore, WorldBibleReader) to replace DbContext in controllers.
   - Split orchestration from controllers; keep validation + delegation only.
   - Fix record validation warnings by moving data annotations to ctor parameters or converting to classes.
5) **Frontend modularity**
   - Add guidance to plans/README.md on hook/component separation and page size caps.
   - Identify remaining large pages (Personas, PlannerTelemetry residuals, Fiction projects) and schedule refactors.

## Testing/Verification
- Analyzer/test fails on raw scope usage.
- API tests for surface placement (auth/roles) and DTO validation.
- Telemetry contract: unit snapshot for required fields; integration event emitted for planner/backlog/tool flows.
- Manual: targeted page loads post-split; controllers still return same responses.

## Risk/Rollback
- Risk: analyzer false positives; mitigate with allowlist + docs.
- Risk: surface moves break clients; mitigate with redirect/compat route notes.
- Rollback: disable analyzer/test gate and keep docs if refactor blocks release.

## Worklog Protocol
- Per plans/README.md: note Goal, Context, Commands, Files, Tests, Issues, Decisions, Completion, Next Actions.
