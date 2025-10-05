using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionStoryMetric : BaseEntity
{
    public Guid FictionPlanId { get; set; }
    public FictionPlan FictionPlan { get; set; } = null!;

    public Guid? FictionChapterSceneId { get; set; }
    public FictionChapterScene? FictionChapterScene { get; set; }

    public Guid? DraftSegmentVersionId { get; set; }
    public DraftSegmentVersion? DraftSegmentVersion { get; set; }

    public string MetricKey { get; set; } = string.Empty;
    public double? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public Dictionary<string, object?>? Data { get; set; }
}
