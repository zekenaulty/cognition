# DoD Domains + Workflows v2 — Step 2026-01-01 00:24 — workflows relational context

Goal
- Add EF Core packages, DbContext, and entity configurations for workflow definitions/executions.

Context
- Workflow core models live in `src/Cognition.Workflows` and need relational mappings in `Cognition.Workflows.Relational`.

Commands Executed
- `Remove-Item -Path src\Cognition.Workflows.Relational\Class1.cs`
- `New-Item -ItemType Directory -Force src\Cognition.Workflows.Relational\Configuration`
- `Set-Content -Path src\Cognition.Workflows.Relational\Configuration\JsonValueConversions.cs`
- `Set-Content -Path src\Cognition.Workflows.Relational\Configuration\WorkflowDefinitionConfiguration.cs`
- `Set-Content -Path src\Cognition.Workflows.Relational\Configuration\WorkflowNodeConfiguration.cs`
- `Set-Content -Path src\Cognition.Workflows.Relational\Configuration\WorkflowEdgeConfiguration.cs`
- `Set-Content -Path src\Cognition.Workflows.Relational\Configuration\WorkflowExecutionConfiguration.cs`
- `Set-Content -Path src\Cognition.Workflows.Relational\CognitionWorkflowsDbContext.cs`
- `Set-Content -Path src\Cognition.Workflows.Relational\ServiceCollectionExtensions.cs`

Files Changed
- `src/Cognition.Workflows.Relational/Cognition.Workflows.Relational.csproj`
- `src/Cognition.Workflows.Relational/CognitionWorkflowsDbContext.cs`
- `src/Cognition.Workflows.Relational/ServiceCollectionExtensions.cs`
- `src/Cognition.Workflows.Relational/Configuration/JsonValueConversions.cs`
- `src/Cognition.Workflows.Relational/Configuration/WorkflowDefinitionConfiguration.cs`
- `src/Cognition.Workflows.Relational/Configuration/WorkflowNodeConfiguration.cs`
- `src/Cognition.Workflows.Relational/Configuration/WorkflowEdgeConfiguration.cs`
- `src/Cognition.Workflows.Relational/Configuration/WorkflowExecutionConfiguration.cs`
- `src/Cognition.Workflows.Relational/Class1.cs` (removed)

Tests / Results
- Not run (EF Core scaffolding only).

Issues
- None.

Decision
- Use JSONB columns for metadata dictionaries and string conversions for enums.

Completion
- [x]

Next Actions
- Add migrations once mappings are stable and wire DI in API/Jobs if needed.
