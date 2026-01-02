using Cognition.Workflows.Common;

namespace Cognition.Workflows.Definitions;

public class WorkflowDefinition : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Version { get; set; } = "v1";
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;

    public List<WorkflowNode> Nodes { get; set; } = new();
    public List<WorkflowEdge> Edges { get; set; } = new();
}
