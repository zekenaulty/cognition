using Cognition.Domains.Common;

namespace Cognition.Domains.Assets;

public class KnowledgeAsset : BaseEntity
{
    public Guid DomainId { get; set; }
    public Guid? BoundedContextId { get; set; }
    public string ScopeString { get; set; } = "";
    public KnowledgeAssetType AssetType { get; set; } = KnowledgeAssetType.Doc;
    public string ContentRef { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public Dictionary<string, object?> Metadata { get; set; } = new();
    public Guid? DerivedFromAssetId { get; set; }
}
