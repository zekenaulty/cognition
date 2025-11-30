namespace Cognition.Clients.Tools.Sandbox;

public interface IToolSandboxWorker
{
    Task<ToolSandboxResult> ExecuteAsync(ToolSandboxWorkRequest request, CancellationToken ct);
}
