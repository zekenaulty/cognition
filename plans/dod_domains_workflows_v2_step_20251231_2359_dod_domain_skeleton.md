# DoD Domains + Workflows v2 — Step 2025-12-31 23:59 — DoD domain skeleton

Goal
- Add initial DoD domain models and enums in `Cognition.Domains`.

Context
- v2 plan Story 2 requires DoD entities/value objects before EF Core mappings.

Commands Executed
- `New-Item -ItemType Directory -Force src\Cognition.Domains\Common, src\Cognition.Domains\Domains, src\Cognition.Domains\Scopes, src\Cognition.Domains\Assets, src\Cognition.Domains\Tools, src\Cognition.Domains\Policies, src\Cognition.Domains\Events`
- `Remove-Item -Path src\Cognition.Domains\Class1.cs`
- `Set-Content -Path src\Cognition.Domains\Common\BaseEntity.cs`
- `Set-Content -Path src\Cognition.Domains\Common\Enums.cs`
- `Set-Content -Path src\Cognition.Domains\Domains\Domain.cs`
- `Set-Content -Path src\Cognition.Domains\Domains\BoundedContext.cs`
- `Set-Content -Path src\Cognition.Domains\Domains\DomainManifest.cs`
- `Set-Content -Path src\Cognition.Domains\Scopes\ScopeType.cs`
- `Set-Content -Path src\Cognition.Domains\Scopes\ScopeInstance.cs`
- `Set-Content -Path src\Cognition.Domains\Assets\KnowledgeAsset.cs`
- `Set-Content -Path src\Cognition.Domains\Tools\ToolDescriptor.cs`
- `Set-Content -Path src\Cognition.Domains\Policies\Policy.cs`
- `Set-Content -Path src\Cognition.Domains\Events\EventType.cs`

Files Changed
- `src/Cognition.Domains/Common/BaseEntity.cs`
- `src/Cognition.Domains/Common/Enums.cs`
- `src/Cognition.Domains/Domains/Domain.cs`
- `src/Cognition.Domains/Domains/BoundedContext.cs`
- `src/Cognition.Domains/Domains/DomainManifest.cs`
- `src/Cognition.Domains/Scopes/ScopeType.cs`
- `src/Cognition.Domains/Scopes/ScopeInstance.cs`
- `src/Cognition.Domains/Assets/KnowledgeAsset.cs`
- `src/Cognition.Domains/Tools/ToolDescriptor.cs`
- `src/Cognition.Domains/Policies/Policy.cs`
- `src/Cognition.Domains/Events/EventType.cs`
- `src/Cognition.Domains/Class1.cs` (removed)

Tests / Results
- Not run (model scaffolding only).

Issues
- None.

Decision
- Keep domain classes POCO-only with minimal properties; enforce invariants later via services or EF constraints.

Completion
- [x]

Next Actions
- Add EF Core mappings in `Cognition.Domains.Relational` and update the plan checklist.
