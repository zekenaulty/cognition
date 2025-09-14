using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Knowledge;

public class KnowledgeEmbedding : BaseEntity
{
    public Guid KnowledgeItemId { get; set; }
    public KnowledgeItem KnowledgeItem { get; set; } = null!;

    // Optional human-readable label (e.g., "title", "body", or chunk id)
    public string? Label { get; set; }

    // Optional metadata about the embedding (model name, dimensions, etc.)
    public Dictionary<string, object?>? Metadata { get; set; }

    // The embedding vector values
    public float[] Vector { get; set; } = Array.Empty<float>();

    // Typed, queryable descriptors for portability and governance
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public string? ModelVersion { get; set; }
    public int? Dimensions { get; set; }
    public EmbeddingSpace? Space { get; set; }
    public bool? Normalized { get; set; }
    public double? VectorL2Norm { get; set; }
    public string? ContentHash { get; set; }
    public int? ChunkIndex { get; set; }
    public int? CharStart { get; set; }
    public int? CharEnd { get; set; }
    public string? Language { get; set; }
    public int? SchemaVersion { get; set; }
}

public enum EmbeddingSpace
{
    Cosine,
    Dot,
    Euclidean
}
