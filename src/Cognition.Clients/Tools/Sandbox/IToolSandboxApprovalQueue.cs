namespace Cognition.Clients.Tools.Sandbox;

public interface IToolSandboxApprovalQueue
{
    Task EnqueueAsync(ToolSandboxWorkRequest request, CancellationToken ct);
    bool TryDequeue(out ToolSandboxWorkRequest? request);
    IReadOnlyCollection<ToolSandboxWorkRequest> Snapshot();
}
