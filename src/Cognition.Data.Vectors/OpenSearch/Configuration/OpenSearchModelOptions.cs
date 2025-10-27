namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;

public sealed class OpenSearchModelOptions
{
    public string? ModelId { get; set; }
    public string EmbeddingField { get; set; } = "embedding";
    public string TextField { get; set; } = "text";
    public OpenSearchModelBootstrapOptions Bootstrap { get; set; } = new();
}

public sealed class OpenSearchModelBootstrapOptions
{
    public string Name { get; set; } = "all-MiniLM-L6-v2";
    public string Version { get; set; } = "1.0.1";
    public string Description { get; set; } = "MiniLM L6 v2 (torchscript, sentence transformers)";
    public string Url { get; set; } = "https://artifacts.opensearch.org/models/ml-models/huggingface/sentence-transformers/all-MiniLM-L6-v2/1.0.1/torch_script/sentence-transformers_all-MiniLM-L6-v2-1.0.1-torch_script.zip";
    public string ContentHash { get; set; } = "c15f0d2e62d872be5b5bc6c84d2e0f4921541e29fefbef51d59cc10a8ae30e0f";
    public string ModelFormat { get; set; } = "TORCH_SCRIPT";
    public string FunctionName { get; set; } = "TEXT_EMBEDDING";
    public string FrameworkType { get; set; } = "sentence_transformers";
    public string ModelType { get; set; } = "bert";
    public int EmbeddingDimension { get; set; } = 384;
}
