# DoD Domains + Workflows v2 — Step 2025-12-31 23:57 — project references

Goal
- Establish initial project reference boundaries between new DoD/workflow libraries.

Context
- `Cognition.Domains` should own core models; relational/doc projects depend on it.
- `Cognition.Workflows` should own workflow core models; relational persistence depends on it.

Commands Executed
- `dotnet add src\Cognition.Domains.Relational\Cognition.Domains.Relational.csproj reference src\Cognition.Domains\Cognition.Domains.csproj`
- `dotnet add src\Cognition.Domains.Documents\Cognition.Domains.Documents.csproj reference src\Cognition.Domains\Cognition.Domains.csproj`
- `dotnet add src\Cognition.Workflows.Relational\Cognition.Workflows.Relational.csproj reference src\Cognition.Workflows\Cognition.Workflows.csproj`

Files Changed
- `src/Cognition.Domains.Relational/Cognition.Domains.Relational.csproj`
- `src/Cognition.Domains.Documents/Cognition.Domains.Documents.csproj`
- `src/Cognition.Workflows.Relational/Cognition.Workflows.Relational.csproj`

Tests / Results
- Not run (project references only).

Issues
- None.

Decision
- Keep references minimal and one-way: relational/doc projects depend on core libraries only.

Completion
- [x]

Next Actions
- Add EF Core/RavenDB packages when DbContexts and repositories are introduced.
