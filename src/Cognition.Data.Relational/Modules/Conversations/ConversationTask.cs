using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Conversations;

public class ConversationTask : BaseEntity
{
    public Guid ConversationPlanId { get; set; }
    public ConversationPlan ConversationPlan { get; set; } = null!;

    public int StepNumber { get; set; }
    public string Thought { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public string? Rationale { get; set; }
    public Guid? ToolId { get; set; }
    public string? ToolName { get; set; }
    public string? ArgsJson { get; set; } // jsonb
    public string? Observation { get; set; }
    public string? Error { get; set; }
    public bool Finish { get; set; }
    public string? FinalAnswer { get; set; }
    public string? Status { get; set; } // Pending/Success/Failure
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? BacklogItemId { get; set; }
}
