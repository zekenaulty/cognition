using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class DraftSegment : BaseEntity
{
    public Guid FictionProjectId { get; set; }
    public FictionProject FictionProject { get; set; } = null!;

    public Guid? OutlineNodeId { get; set; }
    public OutlineNode? OutlineNode { get; set; }

    public string Title { get; set; } = string.Empty;
    public int ActiveVersionIndex { get; set; } = -1;

    public List<DraftSegmentVersion> Versions { get; set; } = [];
}

public class DraftSegmentVersion : BaseEntity
{
    public Guid DraftSegmentId { get; set; }
    public DraftSegment DraftSegment { get; set; } = null!;

    public int VersionIndex { get; set; }
    public string BodyMarkdown { get; set; } = string.Empty;
    public Dictionary<string, object?>? Metrics { get; set; } // jsonb: counts, readability, diffs

    // Optional projection link
    public Guid? KnowledgeItemId { get; set; }
}

public class Annotation : BaseEntity
{
    public Guid FictionProjectId { get; set; }
    public FictionProject FictionProject { get; set; } = null!;

    public string TargetType { get; set; } = string.Empty; // e.g., DraftSegmentVersion, OutlineNodeVersion
    public Guid TargetId { get; set; }

    public AnnotationType Type { get; set; } = AnnotationType.Todo;
    public string? Message { get; set; }
    public string? Details { get; set; }
    public AnnotationSeverity Severity { get; set; } = AnnotationSeverity.Info;
    public bool Resolved { get; set; }
}

public enum AnnotationType
{
    Continuity,
    FactCheck,
    StyleViolation,
    Timeline,
    Todo,
    Other
}

public enum AnnotationSeverity
{
    Info,
    Warning,
    Error
}

