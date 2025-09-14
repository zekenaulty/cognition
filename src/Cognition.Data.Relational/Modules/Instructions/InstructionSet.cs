using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Instructions;

public class InstructionSet : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Scope { get; set; } // e.g., global/provider/model/persona/agent
    public Guid? ScopeRefId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public List<InstructionSetItem> Items { get; set; } = [];
}
