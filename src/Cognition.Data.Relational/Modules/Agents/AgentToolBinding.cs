using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Agents;

public class AgentToolBinding : BaseEntity
{
    // Scope: either Agent or Persona. Store as string to keep flexible.
    public string ScopeType { get; set; } = "Agent"; // or "Persona"
    public Guid ScopeId { get; set; }

    public Guid ToolId { get; set; }
    public Modules.Tools.Tool Tool { get; set; } = null!;

    public bool Enabled { get; set; } = true;
    public Dictionary<string, object?>? Config { get; set; }
}
