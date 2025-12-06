# Users / Credentials / Config / Admin

## Objective
Keep identity/config endpoints minimal and validated; separate admin-only from self-service; fix record validation warnings.

## Status
Mixed controllers; some DTOs are records with property annotations.

## Hotspots / Risks
- Record DTO validation ignored on properties (LoginRequest, CreateConversationRequest pattern seen elsewhere).
- AdminController/ConfigController may expose settings without strict policy.
- Scope: minimal here, but ensure persona ownership checks on persona grants.

## Planned Actions
- Convert/annotate DTOs to eliminate validation warnings.
- Ensure [Authorize] policies: admin-only for config/admin routes; self-service for profile/password.
- Add API tests for validation/auth failures.

## Tests / Checks
- Validation API tests (login/register/password change).
- Auth tests for admin/config routes.
