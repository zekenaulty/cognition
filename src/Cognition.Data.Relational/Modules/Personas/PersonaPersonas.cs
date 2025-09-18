using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Personas;

public class PersonaPersonas : BaseEntity
{
    public Guid FromPersonaId { get; set; }
    public Persona FromPersona { get; set; } = null!;

    public Guid ToPersonaId { get; set; }
    public Persona ToPersona { get; set; } = null!;

    public bool IsOwner { get; set; } = false;
    public string? Label { get; set; }
}

