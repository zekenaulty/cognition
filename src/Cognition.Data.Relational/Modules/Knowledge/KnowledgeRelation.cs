using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Knowledge;

public class KnowledgeRelation : BaseEntity
{
    public Guid FromKnowledgeItemId { get; set; }
    public KnowledgeItem FromKnowledgeItem { get; set; } = null!;
    public Guid ToKnowledgeItemId { get; set; }
    public KnowledgeItem ToKnowledgeItem { get; set; } = null!;
    public string RelationshipType { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
    public string? Description { get; set; }
}
