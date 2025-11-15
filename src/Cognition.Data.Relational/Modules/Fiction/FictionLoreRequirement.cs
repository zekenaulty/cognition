using System;
using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionLoreRequirement : BaseEntity
{
    public Guid FictionPlanId { get; set; }
    public FictionPlan FictionPlan { get; set; } = null!;

    public Guid? ChapterScrollId { get; set; }
    public FictionChapterScroll? ChapterScroll { get; set; }

    public Guid? ChapterSceneId { get; set; }
    public FictionChapterScene? ChapterScene { get; set; }

    public Guid? CreatedByPlanPassId { get; set; }
    public FictionPlanPass? CreatedByPlanPass { get; set; }

    public Guid? WorldBibleEntryId { get; set; }
    public FictionWorldBibleEntry? WorldBibleEntry { get; set; }

    public string RequirementSlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public FictionLoreRequirementStatus Status { get; set; } = FictionLoreRequirementStatus.Planned;
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
}

public enum FictionLoreRequirementStatus
{
    Planned,
    Blocked,
    Ready
}
