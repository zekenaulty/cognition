using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Personas;

public class PersonaDream : BaseEntity
{
    public Guid PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;

    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public string[]? Tags { get; set; }

    public int Valence { get; set; } // -5..+5
    public int Vividness { get; set; } // 0..100
    public bool Lucid { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public Dictionary<string, object?>? Properties { get; set; }
}

