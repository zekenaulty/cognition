using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Conversations;

public class ConversationSummary : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid ByPersonaId { get; set; }
    public Modules.Personas.Persona ByPersona { get; set; } = null!;

    public Guid? ReferencesPersonaId { get; set; }
    public Modules.Personas.Persona? ReferencesPersona { get; set; }

    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
