namespace Cognition.Domains.Documents.Documents;

public class KnowledgeAssetDocument
{
    public string Id { get; set; } = string.Empty;
    public Guid KnowledgeAssetId { get; set; }
    public Guid DomainId { get; set; }
    public Guid? BoundedContextId { get; set; }
    public string ContentType { get; set; } = "";
    public string Content { get; set; } = "";
    public Dictionary<string, object?> Metadata { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
