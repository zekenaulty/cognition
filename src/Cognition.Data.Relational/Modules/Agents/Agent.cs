using Cognition.Data.Relational.Modules.LLM;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cognition.Data.Relational.Modules.Agents;

public class Agent : BaseEntity
{
    public Guid PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;

    public Guid Version { get; set; } = Guid.NewGuid();
    public bool RolePlay { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }

    public Guid? ClientProfileId { get; set; }
    public ClientProfile? ClientProfile { get; set; }

    [NotMapped]
    public Dictionary<string, object?>? State { get; set; }

    public List<AgentToolBinding> ToolBindings { get; set; } = [];
}
