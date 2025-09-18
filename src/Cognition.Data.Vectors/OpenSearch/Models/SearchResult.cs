namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;

public sealed class SearchResult
{
    public required VectorItem Item { get; init; }
    public double Score { get; init; }
    public Dictionary<string, object>? Highlights { get; init; }
}

