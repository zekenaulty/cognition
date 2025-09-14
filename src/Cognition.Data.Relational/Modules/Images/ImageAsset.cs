using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Images;

public class ImageAsset : BaseEntity
{
    public Guid? ConversationId { get; set; }
    public Modules.Conversations.Conversation? Conversation { get; set; }

    public Guid? CreatedByPersonaId { get; set; }
    public Modules.Personas.Persona? CreatedByPersona { get; set; }

    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    public string MimeType { get; set; } = "image/png";
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] Bytes { get; set; } = Array.Empty<byte>();
    public string? Sha256 { get; set; }

    public string Prompt { get; set; } = string.Empty;
    public string? NegativePrompt { get; set; }

    public Guid? StyleId { get; set; }
    public ImageStyle? Style { get; set; }

    public int Steps { get; set; }
    public float Guidance { get; set; }
    public int? Seed { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}

