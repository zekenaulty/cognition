namespace Cognition.Clients.Tools.Sandbox;

public sealed class NoopSandboxWorker : IToolSandboxWorker
{
    public Task<ToolSandboxResult> ExecuteAsync(ToolSandboxWorkRequest request, CancellationToken ct)
    {
        return Task.FromResult(new ToolSandboxResult(false, null, "Sandbox worker not implemented."));
    }
}
