# DoD Domains + Workflows v2 — Step 2026-01-01 00:13 — DoD relational context

Goal
- Add EF Core packages, DbContext, and entity configurations for DoD relational storage.

Context
- Domain models live in `src/Cognition.Domains` and need relational mappings in `Cognition.Domains.Relational`.

Commands Executed
- `Remove-Item -Path src\Cognition.Domains.Relational\Class1.cs`
- `New-Item -ItemType Directory -Force src\Cognition.Domains.Relational\Configuration`
- `Set-Content -Path src\Cognition.Domains.Relational\Configuration\JsonValueConversions.cs`
- `Set-Content -Path src\Cognition.Domains.Relational\Configuration\DomainConfiguration.cs`
- `Set-Content -Path src\Cognition.Domains.Relational\Configuration\BoundedContextConfiguration.cs`
- `Set-Content -Path src\Cognition.Domains.Relational\Configuration\DomainManifestConfiguration.cs`
- `Set-Content -Path src\Cognition.Domains.Relational\Configuration\ScopeTypeConfiguration.cs`
- `Set-Content -Path src\Cognition.Domains.Relational\Configuration\ScopeInstanceConfiguration.cs`
- `Set-Content -Path src\Cognition.Domains.Relational\Configuration\KnowledgeAssetConfiguration.cs`
- `Set-Content -Path src\Cognition.Domains.Relational\Configuration\ToolDescriptorConfiguration.cs`
- `Set-Content -Path src\Cognition.Domains.Relational\Configuration\PolicyConfiguration.cs`
- `Set-Content -Path src\Cognition.Domains.Relational\Configuration\EventTypeConfiguration.cs`
- `Set-Content -Path src\Cognition.Domains.Relational\CognitionDomainsDbContext.cs`
- `Set-Content -Path src\Cognition.Domains.Relational\ServiceCollectionExtensions.cs`

Files Changed
- `src/Cognition.Domains.Relational/Cognition.Domains.Relational.csproj`
- `src/Cognition.Domains.Relational/CognitionDomainsDbContext.cs`
- `src/Cognition.Domains.Relational/ServiceCollectionExtensions.cs`
- `src/Cognition.Domains.Relational/Configuration/JsonValueConversions.cs`
- `src/Cognition.Domains.Relational/Configuration/DomainConfiguration.cs`
- `src/Cognition.Domains.Relational/Configuration/BoundedContextConfiguration.cs`
- `src/Cognition.Domains.Relational/Configuration/DomainManifestConfiguration.cs`
- `src/Cognition.Domains.Relational/Configuration/ScopeTypeConfiguration.cs`
- `src/Cognition.Domains.Relational/Configuration/ScopeInstanceConfiguration.cs`
- `src/Cognition.Domains.Relational/Configuration/KnowledgeAssetConfiguration.cs`
- `src/Cognition.Domains.Relational/Configuration/ToolDescriptorConfiguration.cs`
- `src/Cognition.Domains.Relational/Configuration/PolicyConfiguration.cs`
- `src/Cognition.Domains.Relational/Configuration/EventTypeConfiguration.cs`
- `src/Cognition.Domains.Relational/Class1.cs` (removed)

Tests / Results
- Not run (EF Core scaffolding only).

Issues
- None.

Decision
- Use JSONB columns for list/dictionary fields and string conversions for enums.

Completion
- [x]

Next Actions
- Add migrations once mappings are stable and wire DI in API/Jobs.
