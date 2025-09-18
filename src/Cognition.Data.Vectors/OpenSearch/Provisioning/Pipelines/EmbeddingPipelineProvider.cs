using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Provisioning.Pipelines;

public static class EmbeddingPipelineProvider
{
    public static object Build(OpenSearchVectorsOptions vectorsOptions, OpenSearchModelOptions modelOptions)
    {
        var textField = "text";
        var embeddingField = "embedding";

        if (!string.IsNullOrWhiteSpace(modelOptions.TextField)) textField = modelOptions.TextField;
        if (!string.IsNullOrWhiteSpace(modelOptions.EmbeddingField)) embeddingField = modelOptions.EmbeddingField;

        return new
        {
            description = "Cognition text embedding pipeline",
            processors = new object[]
            {
                new
                {
                    text_embedding = new
                    {
                        model_id = modelOptions.ModelId ?? string.Empty,
                        field_map = new Dictionary<string, string>
                        {
                            [textField] = embeddingField
                        }
                    }
                }
            }
        };
    }
}

