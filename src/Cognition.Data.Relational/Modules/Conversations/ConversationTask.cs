using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Conversations;

public class ConversationTask : BaseEntity
{
    public Guid ConversationPlanId { get; set; }
    public ConversationPlan ConversationPlan { get; set; } = null!;

    public int StepNumber { get; set; }
    public string Thought { get; set; } = string.Empty;
    public string? Action { get; set; }
    public string? Result { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
