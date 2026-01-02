using Cognition.Workflows.Common;

namespace Cognition.Workflows.Executions;

public class WorkflowExecution : BaseEntity
{
    public Guid WorkflowDefinitionId { get; set; }
    public WorkflowExecutionStatus Status { get; set; } = WorkflowExecutionStatus.Pending;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = new();
}
