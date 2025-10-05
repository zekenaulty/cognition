using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionWorldBible : BaseEntity
{
    public Guid FictionPlanId { get; set; }
    public FictionPlan FictionPlan { get; set; } = null!;

    public string Domain { get; set; } = string.Empty;
    public string? BranchSlug { get; set; }

    public List<FictionWorldBibleEntry> Entries { get; set; } = [];
}
