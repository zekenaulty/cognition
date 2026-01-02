using Cognition.Workflows.Common;

namespace Cognition.Workflows.Definitions;

public class WorkflowEdge : BaseEntity
{
    public Guid WorkflowDefinitionId { get; set; }
    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }
    public string Kind { get; set; } = "";
    public Dictionary<string, object?> Metadata { get; set; } = new();
}
