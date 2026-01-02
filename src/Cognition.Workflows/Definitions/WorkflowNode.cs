using Cognition.Workflows.Common;

namespace Cognition.Workflows.Definitions;

public class WorkflowNode : BaseEntity
{
    public Guid WorkflowDefinitionId { get; set; }
    public string Key { get; set; } = "";
    public string NodeType { get; set; } = "";
    public string? Name { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = new();
}
