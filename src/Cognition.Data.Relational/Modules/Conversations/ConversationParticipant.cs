using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Conversations;

public class ConversationParticipant : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid PersonaId { get; set; }
    public Modules.Personas.Persona Persona { get; set; } = null!;

    public string? Role { get; set; }
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}
