# DoD Relational Data Layer

Objective
- Add a dedicated relational data layer and supporting class libraries for the Domain of Domains (DoD) model so metadata can be stored, queried, and evolved independently of existing features.

Definition of Done
- New DoD class libraries exist under `src/` and build in `Cognition.sln` with minimal, intentional references.
- EF Core DbContext and entity configurations implement the DoD v1 minimum schema (Domain, BoundedContext, DomainManifest, ScopeType, ScopeInstance, KnowledgeAsset, ToolDescriptor, Policy, EventType) with a baseline migration.
- DI wiring supports a dedicated DoD connection string (or explicit schema choice), and baseline seed data is optional but documented and repeatable.
- Tests validate core invariants and migration application, and docs describe setup and verification.

Scope
- New class libraries for DoD domain models, relational persistence, and contracts/DTOs (final names decided in Story 1).
- EF Core mappings, configurations, and migration scaffolding for the DoD minimum schema.
- Minimal service/repository layer for domain lifecycle actions required to seed and validate the data layer.
- Documentation and developer guidance for configuration and usage.

Out of Scope
- UI/console work, external API surfaces beyond minimal internal wiring, or tool execution workflows.
- Vector/graph storage, ingestion pipelines, retrieval, or RAG orchestration (tracked separately).
- Business-domain specific schemas (only DoD core/technical models in this plan).

Context / Alignment
- `plans/completed/scope_token_path_refactor/scope_token_path_refactor.md` notes alpha builds can cut over schema directly when only seed data exists; keep rollout simple but document future backfill if needed.
- `plans/completed/unit_testing_expansion/unit_testing_expansion.md` recommends EF Core Sqlite in-memory mode for relational tests; follow that for DbContext coverage.

Story Map (Large Chunks)
- Story 1: Architecture and project scaffolding
  - Decide DoD project names, namespaces, and dependencies; add new projects to `Cognition.sln`.
  - Define shared enums/value objects (DomainKind, DomainStatus, manifest versioning) in a core library.
  - Choose storage strategy: dedicated database vs separate schema in existing Postgres; document the decision and connection string key.
- Story 2: Relational schema and EF Core modeling
  - Implement DoD DbContext with entity configs for the v1 minimum schema.
  - Encode invariants with indexes/constraints (canonical keys, manifest versioning, deny-by-default policies).
  - Create the baseline migration and update the model snapshot.
- Story 3: Services and seeding
  - Add minimal repositories/services for domain lifecycle and manifest publishing (draft -> active).
  - Wire DI in `Cognition.Api` (or a host project) with optional seeding hooks.
  - Provide sample seed data for core technical domains and policy defaults.
- Story 4: Validation, tests, and docs
  - Add DbContext tests using Sqlite in-memory for key invariants and migration application.
  - Document configuration, connection strings, and the expected seed data in `docs/`.
  - Add README notes for running migrations and verifying DoD data.

Deliverables
- New DoD class libraries (domain models, persistence, contracts) wired into the solution.
- EF Core DbContext, configurations, and migration for the DoD minimum schema.
- DI configuration and optional seeding path for DoD baseline data.
- Tests for invariants and migrations plus documentation updates.

Data / API / Service / UI Changes
- Data: new DoD schema/tables with versioned manifests, scopes, assets, and policy metadata.
- API: no public endpoints in the initial slice; internal services only unless an admin endpoint is explicitly required.
- Services: thin repository/service layer to manage domain lifecycle and manifest publication.
- UI: none (console/admin UI deferred).

Migration / Rollout Order
1) Add projects and baseline models to the solution.
2) Implement DbContext + migrations; validate against local Postgres.
3) Wire DI and connection string configuration.
4) Seed optional baseline DoD data (draft or active per decision).
5) Document rollback steps and future backfill options.

Testing / Verification
- Unit tests for DbContext invariants and constraints using EF Core Sqlite in-memory.
- Migration apply test: ensure a clean database can be migrated and seeded.
- Local smoke: `dotnet build` and `dotnet test` with DoD projects included.

Risks / Rollback
- Schema ownership ambiguity (new DB vs shared schema) could create operational drift; resolve in Story 1 and document.
- Migration conflicts with existing DbContext updates; isolate DoD migrations under a dedicated project.
- Rollback: disable DoD DI wiring, drop DoD schema/tables, and remove the migration if needed (document exact steps).

Worklog Protocol
- Create step notes under `plans/dod_relational_data_layer_step_YYYYMMDD_HHMM_<slug>.md`.
- Each step note must include Goal, Context, Commands Executed, Files Changed, Tests/Results, Issues, Decision, Completion, Next Actions.
- Keep one discrete action per step note; use `_scratchpad.md` for quick notes.

Checklist
- [ ] Story 1: Decide DoD project names, namespaces, and dependency boundaries.
- [ ] Story 1: Choose storage strategy (dedicated DB vs schema) and connection string key.
- [ ] Story 1: Add new projects to `Cognition.sln` with baseline folders and references.
- [ ] Story 2: Define DoD minimum entities and value objects.
- [ ] Story 2: Implement DbContext + configurations and add baseline migration.
- [ ] Story 2: Add constraints/indexes that enforce core invariants.
- [ ] Story 3: Add minimal services/repositories for domain + manifest lifecycle.
- [ ] Story 3: Wire DI and optional startup seeding.
- [ ] Story 4: Add DbContext tests (Sqlite in-memory) for invariants and migration apply.
- [ ] Story 4: Document configuration, migration, and verification steps.
