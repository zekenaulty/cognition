using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class TimelineEvent : BaseEntity
{
    public Guid FictionProjectId { get; set; }
    public FictionProject FictionProject { get; set; } = null!;

    public Guid? OutlineNodeId { get; set; }
    public OutlineNode? OutlineNode { get; set; }

    public string? InWorldDate { get; set; }
    public int? Index { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }

    public List<TimelineEventAsset> Assets { get; set; } = [];
}

public class TimelineEventAsset : BaseEntity
{
    public Guid TimelineEventId { get; set; }
    public TimelineEvent TimelineEvent { get; set; } = null!;

    public Guid WorldAssetId { get; set; }
    public WorldAsset WorldAsset { get; set; } = null!;
}

