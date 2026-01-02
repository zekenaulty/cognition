# DoD Domains + Workflows v2 — Step 2026-01-01 00:36 — baseline migrations

Goal
- Generate baseline EF Core migrations for DoD and Workflows relational contexts.

Context
- New DbContexts and configurations are in place; migrations are required for schema creation.

Commands Executed
- `dotnet ef migrations add InitialDomains --project src\Cognition.Domains.Relational`
- `dotnet ef migrations add InitialWorkflows --project src\Cognition.Workflows.Relational`

Files Changed
- `src/Cognition.Domains.Relational/Migrations/20260101063715_InitialDomains.cs`
- `src/Cognition.Domains.Relational/Migrations/20260101063715_InitialDomains.Designer.cs`
- `src/Cognition.Domains.Relational/Migrations/CognitionDomainsDbContextModelSnapshot.cs`
- `src/Cognition.Workflows.Relational/Migrations/20260101063735_InitialWorkflows.cs`
- `src/Cognition.Workflows.Relational/Migrations/20260101063735_InitialWorkflows.Designer.cs`
- `src/Cognition.Workflows.Relational/Migrations/CognitionWorkflowsDbContextModelSnapshot.cs`

Tests / Results
- Not run (migration scaffolding only).

Issues
- EF Core tools version warning: tools 9.0.9 vs runtime 9.0.11.

Decision
- Use separate migrations per context inside each relational project.

Completion
- [x]

Next Actions
- Apply migrations and wire startup migration application once verified.
