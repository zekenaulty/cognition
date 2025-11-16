# Architecture Watchlist — API & Scope Discipline

## Status
- Drafting concerns that should be addressed after the current fiction backlog/lifecycle work ships.
- No engineering assigned yet; this document is a parking lot for the next infrastructure-focused session.

## Topics to Revisit

### 1. API Surface Drift
- Controllers currently mix admin console operations, job orchestration, and user-facing API calls under the same route.
- Action items:
  - Define clear API surfaces (public vs. console/admin vs. internal job endpoints).
  - Introduce a formal versioning strategy; remove or rename “v2” routes that haven’t shipped to staging.
  - Add acceptance criteria for new endpoints to ensure they live under the correct surface.

### 2. Scope Discipline
- `ScopeToken`/`ScopePath` provide consistent multi-tenant lineage, but usage is uneven (manual filters, TODOs).
- Action items:
  - Audit retrieval/tooling code for places that bypass `IScopePathBuilder`.
  - Provide guardrails or analyzers to flag calls that hit data stores without a scope.
  - Document examples of canonical scope usage for OpenSearch, SQL, and job telemetry.

### 3. Telemetry Consistency
- `fiction.backlog.telemetry` is now canonical for backlog events, but other workflows rely on ad-hoc logging.
- Action items:
  - Define a small telemetry contract (event kinds, required fields).
  - Add replay endpoints or ingestion jobs so dashboards consume workflow logs uniformly.
  - Identify workflows still lacking structured telemetry and prioritize them.

### 4. Persistence Layout for Author Metadata
- Author personas currently use the generic `Personas` schema. This works for prompt context, but future needs (billing, co-author permissions, contracts) will need structured data.
- Action items:
  - Decide whether to extend `Personas` with new columns or add a child table keyed by `PersonaId`.
  - Capture concrete metadata requirements before multi-author features ship.
  - Ensure new author metadata keeps working with existing `AuthorPersonaRegistry`.

### 5. Versioning Hygiene
- “v2” routes exist even though no prod/staging contract has shipped.
- Action items:
  - Remove or rename pre-release routes; treat them as internal until staged.
  - Establish a versioning guideline (e.g., v1 once we hit staging, internal preview routes should live under `/experimental`).

### 6. Separation of Responsibilities
- Services such as `AuthorPersonaRegistry` read directly from EF contexts, world-bible tables, etc., making testing/reuse harder.
- Action items:
  - Extract narrow data services (e.g., `IAuthorPersonaStore`, `IWorldBibleReader`) and inject them instead of raw `CognitionDbContext`.
  - Use those abstractions in controllers/jobs to limit knowledge of the schema.

## Next Steps
- Finish the current fiction backlog/lifecycle deliverables first.
- Revisit this doc during the next architecture-focused planning session.
- Assign owners for each topic once we have bandwidth.
