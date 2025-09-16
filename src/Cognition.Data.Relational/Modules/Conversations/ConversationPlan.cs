using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Conversations;

public class ConversationPlan : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid PersonaId { get; set; }
    public Modules.Personas.Persona Persona { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ConversationTask> Tasks { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
