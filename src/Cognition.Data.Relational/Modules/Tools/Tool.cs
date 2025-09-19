using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Tools;

public class Tool : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ClassPath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Example { get; set; }
    public string[]? Tags { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
    public bool IsActive { get; set; } = true;

    public List<ToolParameter> Parameters { get; set; } = [];

    // Optional LLM client profile binding for this tool (provider/model/params as data)
    public Guid? ClientProfileId { get; set; }
    public Modules.LLM.ClientProfile? ClientProfile { get; set; }
}
