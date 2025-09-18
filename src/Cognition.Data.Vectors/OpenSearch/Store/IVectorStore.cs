using Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Store;

public interface IVectorStore
{
    Task EnsureProvisionedAsync(CancellationToken ct);
    Task UpsertAsync(VectorItem item, CancellationToken ct);
    Task UpsertManyAsync(IEnumerable<VectorItem> items, CancellationToken ct);
    Task DeleteAsync(string id, string tenantKey, string? kind, CancellationToken ct);
    Task<IReadOnlyList<SearchResult>> SimilaritySearchAsync(
        float[] queryEmbedding, int topK, string tenantKey,
        IDictionary<string, object>? filters, string? kind,
        CancellationToken ct);
}

