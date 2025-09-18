namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Store;

public sealed class VectorQuery
{
    public required float[] Embedding { get; init; }
    public int TopK { get; init; } = 10;
    public required string TenantKey { get; init; }
    public string? Kind { get; init; }
    public IDictionary<string, object>? Filters { get; init; }
}

