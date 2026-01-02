using Cognition.Domains.Common;

namespace Cognition.Domains.Domains;

public class DomainManifest : BaseEntity
{
    public Guid DomainId { get; set; }
    public string Version { get; set; } = "v1";
    public List<string> AllowedEmbeddingFlavors { get; set; } = new();
    public string? DefaultEmbeddingFlavor { get; set; }
    public IndexIsolationPolicy IndexIsolationPolicy { get; set; } = IndexIsolationPolicy.Shared;
    public List<ToolCategory> AllowedToolCategories { get; set; } = new();
    public string? RequiredMetadataSchema { get; set; }
    public string? SafetyProfile { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
