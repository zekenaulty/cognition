using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Instructions;

public class Instruction : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public InstructionKind Kind { get; set; } = InstructionKind.Other;
    public string Content { get; set; } = string.Empty;
    public bool RolePlay { get; set; }
    public string[]? Tags { get; set; }
    public string? Version { get; set; }
    public bool IsActive { get; set; } = true;

    public List<InstructionSetItem> InstructionSetItems { get; set; } = [];
}
