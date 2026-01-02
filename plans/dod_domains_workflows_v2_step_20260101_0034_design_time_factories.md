# DoD Domains + Workflows v2 — Step 2026-01-01 00:34 — design-time DbContext factories

Goal
- Add EF Core design-time DbContext factories for DoD and workflows migrations.

Context
- `dotnet ef` requires design-time factories for new DbContexts.

Commands Executed
- `New-Item -ItemType Directory -Force src\Cognition.Domains.Relational\DesignTime, src\Cognition.Workflows.Relational\DesignTime`
- `Set-Content -Path src\Cognition.Domains.Relational\DesignTime\CognitionDomainsDbContextFactory.cs`
- `Set-Content -Path src\Cognition.Workflows.Relational\DesignTime\CognitionWorkflowsDbContextFactory.cs`

Files Changed
- `src/Cognition.Domains.Relational/DesignTime/CognitionDomainsDbContextFactory.cs`
- `src/Cognition.Workflows.Relational/DesignTime/CognitionWorkflowsDbContextFactory.cs`

Tests / Results
- Not run (design-time scaffolding only).

Issues
- None.

Decision
- Mirror the existing `CognitionDbContextFactory` pattern with environment-aware connection strings.

Completion
- [x]

Next Actions
- Generate baseline migrations once factories are in place.
