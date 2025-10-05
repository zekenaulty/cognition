using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionPlanPass : BaseEntity
{
    public Guid FictionPlanId { get; set; }
    public FictionPlan FictionPlan { get; set; } = null!;

    public int PassIndex { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}
