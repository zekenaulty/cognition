using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class PlotArc : BaseEntity
{
    public Guid FictionProjectId { get; set; }
    public FictionProject FictionProject { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Premise { get; set; }
    public string? Goal { get; set; }
    public string? Conflict { get; set; }
    public string? Resolution { get; set; }

    public List<OutlineNode> OutlineNodes { get; set; } = [];
}

public class OutlineNode : BaseEntity
{
    public Guid FictionProjectId { get; set; }
    public FictionProject FictionProject { get; set; } = null!;

    public Guid? PlotArcId { get; set; }
    public PlotArc? PlotArc { get; set; }

    public Guid? ParentId { get; set; }
    public OutlineNode? Parent { get; set; }
    public List<OutlineNode> Children { get; set; } = [];

    public OutlineNodeType Type { get; set; } = OutlineNodeType.Scene;
    public string Title { get; set; } = string.Empty;
    public int SequenceIndex { get; set; }
    public int ActiveVersionIndex { get; set; } = -1;

    public List<OutlineNodeVersion> Versions { get; set; } = [];
}

public enum OutlineNodeType
{
    Act,
    Part,
    Chapter,
    Scene
}

public class OutlineNodeVersion : BaseEntity
{
    public Guid OutlineNodeId { get; set; }
    public OutlineNode OutlineNode { get; set; } = null!;

    public int VersionIndex { get; set; }
    public Dictionary<string, object?>? Beats { get; set; } // promise/progress/payoff; try/fail cycles
    public string? Pov { get; set; }
    public string? Goals { get; set; }
    public string? Tension { get; set; }
    public string? Status { get; set; }

    // Optional projection link
    public Guid? KnowledgeItemId { get; set; }
}

