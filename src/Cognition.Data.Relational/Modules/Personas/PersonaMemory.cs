using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Personas;

public class PersonaMemory : BaseEntity
{
    public Guid PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;

    public Guid? TypeId { get; set; }
    public PersonaMemoryType? Type { get; set; }

    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;

    public double? Importance { get; set; } // 0..1
    public string[]? Emotions { get; set; }
    public string[]? Tags { get; set; }
    public string? Source { get; set; }

    public DateTime? OccurredAtUtc { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }

    public Dictionary<string, object?>? Properties { get; set; }
}

