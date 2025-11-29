Goal
- Harden high-traffic Chat/Users DTOs with built-in validation and regression tests.

Context
- Aligns with the P0/P1 requirement to enforce DTO validation (plans/alpha_security_observability_hardening.md).

Commands Executed
- dotnet build Cognition.sln
- dotnet test tests/Cognition.Api.Tests/Cognition.Api.Tests.csproj -c Debug

Files Changed
- src/Cognition.Api/Controllers/ChatController.cs
- src/Cognition.Api/Controllers/UsersController.cs
- src/Cognition.Api/Infrastructure/Validation/NotEmptyGuidAttribute.cs
- tests/Cognition.Api.Tests/DataAnnotationTests.cs

Tests / Results
- Chat/Users annotations exercised via `dotnet test ...Cognition.Api.Tests.csproj` (pass).

Issues
- Remaining DTO surfaces (e.g., Config, Tools, Personas) still lack validation; captured as follow-up.

Decision
- Keep attribute-based validation for now; revisit FluentValidation integration once package availability is sorted.

Completion
- ✅

Next Actions
- Extend DTO validation across remaining controllers (Config/Tools/Personas) and consolidate error responses.
