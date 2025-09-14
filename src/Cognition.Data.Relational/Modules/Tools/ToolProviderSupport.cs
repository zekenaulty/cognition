using Cognition.Data.Relational.Modules.LLM;
using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Tools;

public class ToolProviderSupport : BaseEntity
{
    public Guid ToolId { get; set; }
    public Tool Tool { get; set; } = null!;

    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    public Guid? ModelId { get; set; }
    public Model? Model { get; set; }

    public SupportLevel SupportLevel { get; set; } = SupportLevel.Full;
    public string? Notes { get; set; }
}
