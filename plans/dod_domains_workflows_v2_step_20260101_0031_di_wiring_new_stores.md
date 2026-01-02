# DoD Domains + Workflows v2 — Step 2026-01-01 00:31 — DI wiring for new data stores

Goal
- Wire DoD/workflows DbContexts and RavenDB documents into API/Jobs DI.

Context
- New relational and RavenDB projects expose `AddCognitionDomainsDb`, `AddCognitionWorkflowsDb`, and `AddCognitionDomainsDocuments`.

Commands Executed
- None.

Files Changed
- `src/Cognition.Api/Cognition.Api.csproj`
- `src/Cognition.Api/Program.cs`
- `src/Cognition.Jobs/Cognition.Jobs.csproj`
- `src/Cognition.Jobs/Program.cs`

Tests / Results
- Not run (DI wiring only).

Issues
- None.

Decision
- Register services in API and Jobs without auto-running migrations until schemas exist.

Completion
- [x]

Next Actions
- Add migrations and update startup migration application once schemas are ready.
