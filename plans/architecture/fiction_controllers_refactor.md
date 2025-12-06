# FictionPlans / FictionProjects / Images

## Objective
Align fiction controllers with surface/DDD boundaries, enforce ScopePath/ScopeToken on backlog/plan data, and ensure DTO validation. Keep controllers thin over services.

## Status
Fiction UI refactors underway; controllers need scope/validation audit.

## Hotspots / Risks
- Backlog/resume/obligation endpoints: ensure scope + persona ownership checks.
- DTO record annotations; large payloads should validate contracts.
- Image routes (images/image styles) should enforce agent/persona scoping.

## Planned Actions
- Convert/annotate DTOs; add API tests for invalid payloads.
- Ensure backlog/plan queries use ScopePathBuilder; avoid manual participant filters.
- Separate admin vs user fiction operations if mixed.

## Tests / Checks
- API tests for backlog/resume/obligation validations and auth.
- Scope analyzer for fiction plan queries.
- Image upload/list scoped tests.
