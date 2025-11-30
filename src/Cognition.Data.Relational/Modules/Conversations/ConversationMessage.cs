using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Conversations;

public class ConversationMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid? FromPersonaId { get; set; }
    public Modules.Personas.Persona? FromPersona { get; set; }
    public Guid? ToPersonaId { get; set; }
    public Modules.Personas.Persona? ToPersona { get; set; }

    public Guid FromAgentId { get; set; }
    public Modules.Agents.Agent FromAgent { get; set; } = null!;

    public Guid? CreatedByUserId { get; set; }

    public ChatRole Role { get; set; }
    public string? Metatype { get; set; } // e.g. Image, TextResponse, PlanThought, ToolResult
    public string Content { get; set; } = string.Empty; // Active content (current version)
    public int ActiveVersionIndex { get; set; } = 0;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
