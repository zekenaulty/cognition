# DoD Domains + Workflows v2 — Step 2026-01-01 00:28 — RavenDB documents skeleton

Goal
- Add RavenDB client wiring and document model skeletons for DoD artifacts.

Context
- RavenDB runs in Docker and stores long-form DoD manifests/assets.

Commands Executed
- `Remove-Item -Path src\Cognition.Domains.Documents\Class1.cs`
- `New-Item -ItemType Directory -Force src\Cognition.Domains.Documents\Documents`
- `Set-Content -Path src\Cognition.Domains.Documents\Documents\DomainManifestDocument.cs`
- `Set-Content -Path src\Cognition.Domains.Documents\Documents\KnowledgeAssetDocument.cs`
- `Set-Content -Path src\Cognition.Domains.Documents\ServiceCollectionExtensions.cs`

Files Changed
- `src/Cognition.Domains.Documents/Cognition.Domains.Documents.csproj`
- `src/Cognition.Domains.Documents/Documents/DomainManifestDocument.cs`
- `src/Cognition.Domains.Documents/Documents/KnowledgeAssetDocument.cs`
- `src/Cognition.Domains.Documents/ServiceCollectionExtensions.cs`
- `src/Cognition.Domains.Documents/Class1.cs` (removed)

Tests / Results
- Not run (integration scaffolding only).

Issues
- None.

Decision
- Register a singleton DocumentStore and scoped async session for RavenDB access.

Completion
- [x]

Next Actions
- Add repositories and usage in services once DoD ingestion flows are defined.
