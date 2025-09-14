using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Config;

public class SystemVariable : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Type { get; set; }
    public Dictionary<string, object?>? Value { get; set; }
}
