using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionChapterBlueprint : BaseEntity
{
    public Guid FictionPlanId { get; set; }
    public FictionPlan FictionPlan { get; set; } = null!;

    public Guid? SourcePlanPassId { get; set; }
    public FictionPlanPass? SourcePlanPass { get; set; }

    public int ChapterIndex { get; set; }
    public string ChapterSlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Synopsis { get; set; } = string.Empty;
    public Dictionary<string, object?>? Structure { get; set; }

    public Guid? BranchId { get; set; }
    public bool IsLocked { get; set; }

    public List<FictionChapterScroll> Scrolls { get; set; } = [];
}
