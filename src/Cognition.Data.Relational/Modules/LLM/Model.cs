using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.LLM;

public class Model : BaseEntity
{
    public Guid ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    public string Name { get; set; } = string.Empty; // Identifier, e.g., gpt-4o-mini
    public string? DisplayName { get; set; }
    public int? ContextWindow { get; set; }
    public bool SupportsVision { get; set; }
    public bool SupportsStreaming { get; set; }

    public double? InputCostPer1M { get; set; }
    public double? CachedInputCostPer1M { get; set; }
    public double? OutputCostPer1M { get; set; }
    public bool IsDeprecated { get; set; }

    public Dictionary<string, object?>? Metadata { get; set; }
}
