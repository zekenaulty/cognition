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
}

