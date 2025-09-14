using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.FeatureFlags;

public class FeatureFlag : BaseEntity
{
    // Unique key used in code to check the flag
    public string Key { get; set; } = null!;

    // Optional human-friendly description
    public string? Description { get; set; }

    // Current enabled state
    public bool IsEnabled { get; set; }
}
