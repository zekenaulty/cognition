# DoD Domains + Workflows v2 — Step 2026-01-01 00:20 — workflow core models

Goal
- Add core workflow graph models in `Cognition.Workflows`.

Context
- Workflows need a shared model for definitions, nodes, edges, and executions before persistence.

Commands Executed
- `New-Item -ItemType Directory -Force src\Cognition.Workflows\Common, src\Cognition.Workflows\Definitions, src\Cognition.Workflows\Executions`
- `Remove-Item -Path src\Cognition.Workflows\Class1.cs`
- `Set-Content -Path src\Cognition.Workflows\Common\BaseEntity.cs`
- `Set-Content -Path src\Cognition.Workflows\Common\Enums.cs`
- `Set-Content -Path src\Cognition.Workflows\Definitions\WorkflowDefinition.cs`
- `Set-Content -Path src\Cognition.Workflows\Definitions\WorkflowNode.cs`
- `Set-Content -Path src\Cognition.Workflows\Definitions\WorkflowEdge.cs`
- `Set-Content -Path src\Cognition.Workflows\Executions\WorkflowExecution.cs`

Files Changed
- `src/Cognition.Workflows/Common/BaseEntity.cs`
- `src/Cognition.Workflows/Common/Enums.cs`
- `src/Cognition.Workflows/Definitions/WorkflowDefinition.cs`
- `src/Cognition.Workflows/Definitions/WorkflowNode.cs`
- `src/Cognition.Workflows/Definitions/WorkflowEdge.cs`
- `src/Cognition.Workflows/Executions/WorkflowExecution.cs`
- `src/Cognition.Workflows/Class1.cs` (removed)

Tests / Results
- Not run (model scaffolding only).

Issues
- None.

Decision
- Keep workflow models minimal and execution-agnostic; persistence will live in `Cognition.Workflows.Relational`.

Completion
- [x]

Next Actions
- Add EF Core mappings for workflow definitions/executions.
