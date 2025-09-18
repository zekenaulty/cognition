namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;

public sealed class OpenSearchModelOptions
{
    public string? ModelId { get; set; }
    public string EmbeddingField { get; set; } = "embedding";
    public string TextField { get; set; } = "text";
}

