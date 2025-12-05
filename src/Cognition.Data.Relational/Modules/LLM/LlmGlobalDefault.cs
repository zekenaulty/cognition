using System;
using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.LLM;

public sealed class LlmGlobalDefault : BaseEntity
{
    public Guid ModelId { get; set; }
    public Model Model { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;
    public Guid? UpdatedByUserId { get; set; }
}
