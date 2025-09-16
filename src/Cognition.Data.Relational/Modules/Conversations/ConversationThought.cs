using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Conversations;

public class ConversationThought : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid PersonaId { get; set; }
    public Modules.Personas.Persona Persona { get; set; } = null!;

    public string Thought { get; set; } = string.Empty;
    public int StepNumber { get; set; }
    public string? Rationale { get; set; }
    public string? PlanSnapshotJson { get; set; } // jsonb
    public string? Prompt { get; set; }
    public Guid? ParentThoughtId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
