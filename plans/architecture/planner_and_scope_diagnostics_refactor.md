# PlannerDiagnostics / ScopeDiagnostics / OpenSearchDiagnostics

## Objective
Ensure diagnostic endpoints are internal/admin-scoped, emit consistent telemetry, and keep controllers thin over services with ScopePath-aware queries.

## Status
Planner telemetry page refactor underway; diagnostics controllers need surface/validation review.

## Hotspots / Risks
- Routes may be callable without strict admin auth.
- DTO validation and query scope (OpenSearch diagnostics filters).
- Telemetry/event shapes not documented.

## Planned Actions
- Confirm [Authorize] policies (admin/internal) on diagnostics controllers.
- Add DTO validation for filters/ids; convert records if needed.
- Document telemetry contract for planner/backlog/tool alerts; ensure controllers emit or read structured events only.
- ScopePath guard on any data queries.

## Tests / Checks
- API tests for auth/validation on diagnostics routes.
- Scope analyzer for diagnostic queries.
- Telemetry schema snapshot/unit test for expected fields.
