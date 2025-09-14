using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Personas;

namespace Cognition.Data.Relational.Modules.Users;

public class UserPersona : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;

    public bool IsDefault { get; set; }
    public string? Label { get; set; }
}

