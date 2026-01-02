# DoD Domains + Workflows v2 — Step 2025-12-31 23:13 — scaffold projects

Goal
- Scaffold new DoD/workflow projects and add them to the solution.

Context
- v2 plan: `plans/dod_domains_workflows_v2.md`.
- Projects to add: `Cognition.Domains`, `Cognition.Domains.Relational`, `Cognition.Domains.Documents`, `Cognition.Workflows`, `Cognition.Workflows.Relational`.

Commands Executed
- `dotnet new classlib -n Cognition.Domains -o src\Cognition.Domains -f net9.0`
- `dotnet new classlib -n Cognition.Domains.Relational -o src\Cognition.Domains.Relational -f net9.0`
- `dotnet new classlib -n Cognition.Domains.Documents -o src\Cognition.Domains.Documents -f net9.0`
- `dotnet new classlib -n Cognition.Workflows -o src\Cognition.Workflows -f net9.0`
- `dotnet new classlib -n Cognition.Workflows.Relational -o src\Cognition.Workflows.Relational -f net9.0`
- `dotnet sln Cognition.sln add src\Cognition.Domains\Cognition.Domains.csproj src\Cognition.Domains.Relational\Cognition.Domains.Relational.csproj src\Cognition.Domains.Documents\Cognition.Domains.Documents.csproj src\Cognition.Workflows\Cognition.Workflows.csproj src\Cognition.Workflows.Relational\Cognition.Workflows.Relational.csproj`

Files Changed
- `Cognition.sln`
- `src/Cognition.Domains/Cognition.Domains.csproj`
- `src/Cognition.Domains/Class1.cs`
- `src/Cognition.Domains.Relational/Cognition.Domains.Relational.csproj`
- `src/Cognition.Domains.Relational/Class1.cs`
- `src/Cognition.Domains.Documents/Cognition.Domains.Documents.csproj`
- `src/Cognition.Domains.Documents/Class1.cs`
- `src/Cognition.Workflows/Cognition.Workflows.csproj`
- `src/Cognition.Workflows/Class1.cs`
- `src/Cognition.Workflows.Relational/Cognition.Workflows.Relational.csproj`
- `src/Cognition.Workflows.Relational/Class1.cs`

Tests / Results
- Not run (project scaffolding only).

Issues
- None.

Decision
- Create new class library projects as net9.0 placeholders; add package references later when wiring EF Core/RavenDB.

Completion
- [x]

Next Actions
- Decide connection string keys and add dependency rules/references between the new projects.
