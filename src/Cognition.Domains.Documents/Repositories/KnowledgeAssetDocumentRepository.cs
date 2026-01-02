using Cognition.Domains.Documents.Documents;
using Raven.Client.Documents.Session;

namespace Cognition.Domains.Documents.Repositories;

public class KnowledgeAssetDocumentRepository : IKnowledgeAssetDocumentRepository
{
    private readonly IAsyncDocumentSession _session;

    public KnowledgeAssetDocumentRepository(IAsyncDocumentSession session)
    {
        _session = session;
    }

    public Task<KnowledgeAssetDocument?> GetAsync(Guid knowledgeAssetId, CancellationToken cancellationToken = default)
    {
        if (knowledgeAssetId == Guid.Empty)
        {
            throw new ArgumentException("KnowledgeAssetId is required.", nameof(knowledgeAssetId));
        }

        var id = DocumentIds.KnowledgeAsset(knowledgeAssetId);
        return _session.LoadAsync<KnowledgeAssetDocument>(id, cancellationToken);
    }

    public async Task StoreAsync(KnowledgeAssetDocument document, CancellationToken cancellationToken = default)
    {
        if (document.KnowledgeAssetId == Guid.Empty)
        {
            throw new ArgumentException("KnowledgeAssetId is required.", nameof(document));
        }

        if (string.IsNullOrWhiteSpace(document.Id))
        {
            document.Id = DocumentIds.KnowledgeAsset(document.KnowledgeAssetId);
        }

        await _session.StoreAsync(document, document.Id, cancellationToken);
        await _session.SaveChangesAsync(cancellationToken);
    }
}
