using Cognition.Domains.Documents.Documents;

namespace Cognition.Domains.Documents.Repositories;

public interface IKnowledgeAssetDocumentRepository
{
    Task<KnowledgeAssetDocument?> GetAsync(Guid knowledgeAssetId, CancellationToken cancellationToken = default);
    Task StoreAsync(KnowledgeAssetDocument document, CancellationToken cancellationToken = default);
}
