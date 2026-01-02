# DoD Domains + Workflows v2 - Step 2026-01-02 12:16 - Relational tests

Goal
- Add model validation and migration reflection tests for DoD and Workflows relational contexts.

Context
- Story 7 requires DbContext invariants and migration apply coverage.

Commands Executed
- `New-Item -ItemType Directory -Force tests/Cognition.Domains.Relational.Tests`
- `New-Item -ItemType Directory -Force tests/Cognition.Workflows.Relational.Tests`
- `Apply patch: tests/Cognition.Domains.Relational.Tests/Cognition.Domains.Relational.Tests.csproj`
- `Apply patch: tests/Cognition.Domains.Relational.Tests/GlobalUsings.cs`
- `Apply patch: tests/Cognition.Domains.Relational.Tests/EntityConfigurationReflectionTests.cs`
- `Apply patch: tests/Cognition.Domains.Relational.Tests/MigrationReflectionTests.cs`
- `Apply patch: tests/Cognition.Domains.Relational.Tests/DomainsConfigurationTests.cs`
- `Apply patch: tests/Cognition.Workflows.Relational.Tests/Cognition.Workflows.Relational.Tests.csproj`
- `Apply patch: tests/Cognition.Workflows.Relational.Tests/GlobalUsings.cs`
- `Apply patch: tests/Cognition.Workflows.Relational.Tests/EntityConfigurationReflectionTests.cs`
- `Apply patch: tests/Cognition.Workflows.Relational.Tests/MigrationReflectionTests.cs`
- `Apply patch: tests/Cognition.Workflows.Relational.Tests/WorkflowsConfigurationTests.cs`
- `dotnet sln Cognition.sln add tests/Cognition.Domains.Relational.Tests/Cognition.Domains.Relational.Tests.csproj tests/Cognition.Workflows.Relational.Tests/Cognition.Workflows.Relational.Tests.csproj`

Files Changed
- `tests/Cognition.Domains.Relational.Tests/Cognition.Domains.Relational.Tests.csproj`
- `tests/Cognition.Domains.Relational.Tests/GlobalUsings.cs`
- `tests/Cognition.Domains.Relational.Tests/EntityConfigurationReflectionTests.cs`
- `tests/Cognition.Domains.Relational.Tests/MigrationReflectionTests.cs`
- `tests/Cognition.Domains.Relational.Tests/DomainsConfigurationTests.cs`
- `tests/Cognition.Workflows.Relational.Tests/Cognition.Workflows.Relational.Tests.csproj`
- `tests/Cognition.Workflows.Relational.Tests/GlobalUsings.cs`
- `tests/Cognition.Workflows.Relational.Tests/EntityConfigurationReflectionTests.cs`
- `tests/Cognition.Workflows.Relational.Tests/MigrationReflectionTests.cs`
- `tests/Cognition.Workflows.Relational.Tests/WorkflowsConfigurationTests.cs`
- `Cognition.sln`

Tests / Results
- Not run (new tests only).

Issues
- None.

Decision
- Validate schema invariants via EF model inspection and migration reflection rather than SQLite apply.

Completion
- [x]

Next Actions
- Run `dotnet test` to validate new test projects.
