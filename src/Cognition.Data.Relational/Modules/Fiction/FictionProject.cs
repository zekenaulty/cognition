using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionProject : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Logline { get; set; }
    public FictionProjectStatus Status { get; set; } = FictionProjectStatus.Active;

    public List<FictionPlan> FictionPlans { get; set; } = [];
}

public enum FictionProjectStatus
{
    Active,
    Paused,
    Archived
}

