using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Personas;

public class PersonaEventType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // unique key, e.g., milestone, appointment, achievement
    public string? Category { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object?>? Properties { get; set; }
}

