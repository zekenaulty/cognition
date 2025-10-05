using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionChapterScene : BaseEntity
{
    public Guid FictionChapterSectionId { get; set; }
    public FictionChapterSection FictionChapterSection { get; set; } = null!;

    public int SceneIndex { get; set; }
    public string SceneSlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }

    public FictionSceneStatus Status { get; set; } = FictionSceneStatus.Pending;

    public Guid? DraftSegmentVersionId { get; set; }
    public DraftSegmentVersion? DraftSegmentVersion { get; set; }

    public Guid? DerivedFromSceneId { get; set; }
    public FictionChapterScene? DerivedFromScene { get; set; }

    public Guid? BranchId { get; set; }

    public List<FictionStoryMetric> StoryMetrics { get; set; } = [];
    public List<FictionPlanTranscript> Transcripts { get; set; } = [];
    public List<FictionWorldBibleEntry> WorldBibleEntries { get; set; } = [];
}

public enum FictionSceneStatus
{
    Pending,
    Drafting,
    Completed,
    RevisionNeeded,
    Archived
}
