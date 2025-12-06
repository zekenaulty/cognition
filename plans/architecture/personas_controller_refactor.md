# PersonasController

## Objective
Cleanly separate user-facing persona CRUD from admin/ownership operations; enforce ScopePath and validation; keep controller thin over services.

## Status
Pending review. Large DTO set; some validation may rely on property annotations on records.

## Hotspots / Risks
- Ownership/PersonaPersonas invariants; ensure tests cover parent/child ownership.
- ScopePath usage when listing/accessing personas; avoid ad-hoc filters.
- DTO validation warnings on record types.

## Planned Actions
- Convert request records to classes or annotate ctor params; add API tests for validation failures.
- Extract persona data/service layer (IAuthorPersonaStore) to remove DbContext from controller.
- Ensure owner/agent links enforce invariants (users have personas; agents have personas; personas can own personas).

## Tests / Checks
- API tests for create/update/access with invalid inputs.
- Scope analyzer passes on persona queries.
- Ownership regression tests (PersonaOwnershipTests) remain green.
