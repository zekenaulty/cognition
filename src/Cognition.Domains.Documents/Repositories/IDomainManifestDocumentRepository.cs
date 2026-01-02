using Cognition.Domains.Documents.Documents;

namespace Cognition.Domains.Documents.Repositories;

public interface IDomainManifestDocumentRepository
{
    Task<DomainManifestDocument?> GetAsync(Guid domainId, string version, CancellationToken cancellationToken = default);
    Task StoreAsync(DomainManifestDocument document, CancellationToken cancellationToken = default);
}
