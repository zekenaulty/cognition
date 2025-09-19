using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class CanonRule : BaseEntity
{
    public Guid FictionProjectId { get; set; }
    public FictionProject FictionProject { get; set; } = null!;

    public CanonScope Scope { get; set; } = CanonScope.Global;
    public string Key { get; set; } = string.Empty;
    public Dictionary<string, object?>? Value { get; set; } // jsonb
    public string? Evidence { get; set; }
    public double Confidence { get; set; } = 0.9;

    public Guid? PlotArcId { get; set; }
    public PlotArc? PlotArc { get; set; }

    // Optional projection link
    public Guid? KnowledgeItemId { get; set; }
}

public enum CanonScope
{
    Global,
    Arc,
    Character,
    Location,
    System
}

public class Source : BaseEntity
{
    public Guid FictionProjectId { get; set; }
    public FictionProject FictionProject { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Citation { get; set; }
    public DateTime? PublishedAt { get; set; }
}

