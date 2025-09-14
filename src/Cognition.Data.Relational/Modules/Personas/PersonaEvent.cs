using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Personas;

public class PersonaEvent : BaseEntity
{
    public Guid PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;

    public Guid? TypeId { get; set; }
    public PersonaEventType? Type { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string[]? Categories { get; set; }
    public string[]? Tags { get; set; }
    public string? Location { get; set; }
    public double? Importance { get; set; }

    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }

    public Dictionary<string, object?>? Properties { get; set; }
}

