namespace Cognition.Workflows.Common;

public enum WorkflowStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2
}

public enum WorkflowExecutionStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Canceled = 4
}
