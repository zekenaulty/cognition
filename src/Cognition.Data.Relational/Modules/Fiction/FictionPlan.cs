using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Conversations;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionPlan : BaseEntity
{
    public Guid FictionProjectId { get; set; }
    public FictionProject FictionProject { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PrimaryBranchSlug { get; set; } = "main";
    public FictionPlanStatus Status { get; set; } = FictionPlanStatus.Draft;

    public List<FictionPlanPass> Passes { get; set; } = [];
    public List<FictionChapterBlueprint> ChapterBlueprints { get; set; } = [];
    public List<FictionPlanCheckpoint> Checkpoints { get; set; } = [];
    public List<FictionPlanBacklogItem> Backlog { get; set; } = [];
    public List<FictionPlanTranscript> Transcripts { get; set; } = [];
    public List<FictionStoryMetric> StoryMetrics { get; set; } = [];
    public List<FictionWorldBible> WorldBibles { get; set; } = [];
    public List<FictionCharacter> Characters { get; set; } = [];
    public List<FictionLoreRequirement> LoreRequirements { get; set; } = [];
    public Guid? CurrentConversationPlanId { get; set; }
    public ConversationPlan? CurrentConversationPlan { get; set; }
}

public enum FictionPlanStatus
{
    Draft,
    InProgress,
    Complete,
    Archived
}
