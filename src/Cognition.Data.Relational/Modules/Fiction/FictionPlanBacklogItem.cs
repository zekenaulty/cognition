using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionPlanBacklogItem : BaseEntity
{
    public Guid FictionPlanId { get; set; }
    public FictionPlan FictionPlan { get; set; } = null!;

    public string BacklogId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FictionPlanBacklogStatus Status { get; set; } = FictionPlanBacklogStatus.Pending;
    public string[]? Inputs { get; set; }
    public string[]? Outputs { get; set; }
    public DateTime? InProgressAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public enum FictionPlanBacklogStatus
{
    Pending,
    InProgress,
    Complete
}
