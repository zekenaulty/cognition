using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Personas;

public class PersonaLink : BaseEntity
{
    public Guid FromPersonaId { get; set; }
    public Persona FromPersona { get; set; } = null!;
    public Guid ToPersonaId { get; set; }
    public Persona ToPersona { get; set; } = null!;
    public string? RelationshipType { get; set; }
    public double Weight { get; set; } = 1.0;
    public string? Description { get; set; }
}
