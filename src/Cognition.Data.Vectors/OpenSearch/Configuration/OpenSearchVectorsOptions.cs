using System.ComponentModel.DataAnnotations;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;

public sealed class OpenSearchVectorsOptions
{
    [Required]
    public string Url { get; set; } = "http://localhost:9200";

    public string? Username { get; set; }
    public string? Password { get; set; }

    public bool DisableCertValidation { get; set; }

    [Required]
    public string DefaultIndex { get; set; } = "vectors-knowledge";

    [Range(1, 32768)]
    public int Dimension { get; set; } = 768;

    public bool UseEmbeddingPipeline { get; set; }
    public string PipelineId { get; set; } = "vectors-embed";
}

