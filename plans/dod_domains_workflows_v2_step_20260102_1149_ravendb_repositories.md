# DoD Domains + Workflows v2 — Step 2026-01-02 11:49 — RavenDB repositories

Goal
- Add RavenDB repositories for DoD manifest and asset documents.

Context
- `Cognition.Domains.Documents` currently exposes document models and DI wiring; repositories complete Story 4.

Commands Executed
- `New-Item -ItemType Directory -Force src\Cognition.Domains.Documents\Repositories`
- `Set-Content -Path src\Cognition.Domains.Documents\Repositories\DocumentIds.cs`
- `Set-Content -Path src\Cognition.Domains.Documents\Repositories\IDomainManifestDocumentRepository.cs`
- `Set-Content -Path src\Cognition.Domains.Documents\Repositories\DomainManifestDocumentRepository.cs`
- `Set-Content -Path src\Cognition.Domains.Documents\Repositories\IKnowledgeAssetDocumentRepository.cs`
- `Set-Content -Path src\Cognition.Domains.Documents\Repositories\KnowledgeAssetDocumentRepository.cs`
- `Apply patch: src/Cognition.Domains.Documents/ServiceCollectionExtensions.cs`

Files Changed
- `src/Cognition.Domains.Documents/Repositories/DocumentIds.cs`
- `src/Cognition.Domains.Documents/Repositories/IDomainManifestDocumentRepository.cs`
- `src/Cognition.Domains.Documents/Repositories/DomainManifestDocumentRepository.cs`
- `src/Cognition.Domains.Documents/Repositories/IKnowledgeAssetDocumentRepository.cs`
- `src/Cognition.Domains.Documents/Repositories/KnowledgeAssetDocumentRepository.cs`
- `src/Cognition.Domains.Documents/ServiceCollectionExtensions.cs`

Tests / Results
- Not run (repository scaffolding only).

Issues
- None.

Decision
- Keep repositories thin and call `SaveChangesAsync` inside store/update operations.

Completion
- [x]

Next Actions
- Use repositories in DoD services once domain ingestion flows are defined.
