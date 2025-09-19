using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class GlossaryTerm : BaseEntity
{
    public Guid FictionProjectId { get; set; }
    public FictionProject FictionProject { get; set; } = null!;

    public string Term { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string[]? Aliases { get; set; }
    public string? Domain { get; set; }

    // Optional projection link
    public Guid? KnowledgeItemId { get; set; }
}

