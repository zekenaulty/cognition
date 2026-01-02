namespace Cognition.Domains.Documents.Documents;

public class DomainManifestDocument
{
    public string Id { get; set; } = string.Empty;
    public Guid DomainId { get; set; }
    public string Version { get; set; } = "v1";
    public string Content { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
