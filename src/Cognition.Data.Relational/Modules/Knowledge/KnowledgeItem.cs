using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Knowledge;

public class KnowledgeItem : BaseEntity
{
    public KnowledgeContentType ContentType { get; set; } = KnowledgeContentType.Other;
    public string Content { get; set; } = string.Empty;
    public string[]? Categories { get; set; }
    public string[]? Keywords { get; set; }
    public string? Source { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object?>? Properties { get; set; }

    // Multiple embeddings per item (e.g., different models or chunking strategies)
    public ICollection<KnowledgeEmbedding> Embeddings { get; set; } = new List<KnowledgeEmbedding>();
}

public enum KnowledgeContentType
{
    Question,
    Answer,
    Fact,
    Concept,
    PersonaTrait,
    CategoryDefinition,
    KeywordDefinition,
    Hypothesis,
    Procedure,
    UserPreference,
    SystemInstruction,
    Summary,
    Memory,
    Other
}
