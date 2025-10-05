using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionProject : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Logline { get; set; }
    public FictionProjectStatus Status { get; set; } = FictionProjectStatus.Active;

    public Guid? PrimaryStyleGuideId { get; set; }
    public StyleGuide? PrimaryStyleGuide { get; set; }

    public List<StyleGuide> StyleGuides { get; set; } = [];
    public List<GlossaryTerm> GlossaryTerms { get; set; } = [];
    public List<WorldAsset> WorldAssets { get; set; } = [];
    public List<FictionPlan> FictionPlans { get; set; } = [];
    public List<PlotArc> PlotArcs { get; set; } = [];
}

public enum FictionProjectStatus
{
    Active,
    Paused,
    Archived
}


