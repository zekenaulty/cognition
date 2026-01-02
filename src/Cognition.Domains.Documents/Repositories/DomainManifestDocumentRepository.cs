using Cognition.Domains.Documents.Documents;
using Raven.Client.Documents.Session;

namespace Cognition.Domains.Documents.Repositories;

public class DomainManifestDocumentRepository : IDomainManifestDocumentRepository
{
    private readonly IAsyncDocumentSession _session;

    public DomainManifestDocumentRepository(IAsyncDocumentSession session)
    {
        _session = session;
    }

    public Task<DomainManifestDocument?> GetAsync(Guid domainId, string version, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Version is required.", nameof(version));
        }

        var id = DocumentIds.DomainManifest(domainId, version);
        return _session.LoadAsync<DomainManifestDocument>(id, cancellationToken);
    }

    public async Task StoreAsync(DomainManifestDocument document, CancellationToken cancellationToken = default)
    {
        if (document.DomainId == Guid.Empty)
        {
            throw new ArgumentException("DomainId is required.", nameof(document));
        }

        if (string.IsNullOrWhiteSpace(document.Version))
        {
            throw new ArgumentException("Version is required.", nameof(document));
        }

        if (string.IsNullOrWhiteSpace(document.Id))
        {
            document.Id = DocumentIds.DomainManifest(document.DomainId, document.Version);
        }

        await _session.StoreAsync(document, document.Id, cancellationToken);
        await _session.SaveChangesAsync(cancellationToken);
    }
}
