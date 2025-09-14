using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Personas;

public class PersonaMemoryType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // unique key, e.g., long_term, short_term, episodic
    public string? Category { get; set; } // optional grouping
    public string? Description { get; set; }
    public Dictionary<string, object?>? Properties { get; set; }
}

