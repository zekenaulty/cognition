using System.Collections.Concurrent;

namespace Cognition.Clients.Tools.Sandbox;

public sealed class InMemorySandboxApprovalQueue : IToolSandboxApprovalQueue
{
    private readonly ConcurrentQueue<ToolSandboxWorkRequest> _queue = new();

    public Task EnqueueAsync(ToolSandboxWorkRequest request, CancellationToken ct)
    {
        _queue.Enqueue(request);
        return Task.CompletedTask;
    }

    public bool TryDequeue(out ToolSandboxWorkRequest? request)
    {
        var ok = _queue.TryDequeue(out var value);
        request = value;
        return ok;
    }

    public IReadOnlyCollection<ToolSandboxWorkRequest> Snapshot() => _queue.ToArray();

}
