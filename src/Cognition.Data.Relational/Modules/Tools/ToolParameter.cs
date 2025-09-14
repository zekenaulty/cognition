using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Tools;

public class ToolParameter : BaseEntity
{
    public Guid ToolId { get; set; }
    public Tool Tool { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public ToolParamDirection Direction { get; set; } = ToolParamDirection.Input;
    public bool Required { get; set; }
    public Dictionary<string, object?>? DefaultValue { get; set; }
    public Dictionary<string, object?>? Options { get; set; }
    public string? Description { get; set; }
}
