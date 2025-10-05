using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionPlanCheckpoint : BaseEntity
{
    public Guid FictionPlanId { get; set; }
    public FictionPlan FictionPlan { get; set; } = null!;

    public string Phase { get; set; } = string.Empty;
    public FictionPlanCheckpointStatus Status { get; set; } = FictionPlanCheckpointStatus.Pending;
    public int? CompletedCount { get; set; }
    public int? TargetCount { get; set; }
    public Dictionary<string, object?>? Progress { get; set; }

    public Guid? LockedByAgentId { get; set; }
    public Guid? LockedByConversationId { get; set; }
    public DateTime? LockedAtUtc { get; set; }
}

public enum FictionPlanCheckpointStatus
{
    Pending,
    InProgress,
    Complete,
    Cancelled
}
