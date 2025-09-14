using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Instructions;

public class InstructionSetItem : BaseEntity
{
    public Guid InstructionSetId { get; set; }
    public InstructionSet InstructionSet { get; set; } = null!;

    public Guid InstructionId { get; set; }
    public Instruction Instruction { get; set; } = null!;

    public int Order { get; set; }
    public bool Enabled { get; set; } = true;
}
