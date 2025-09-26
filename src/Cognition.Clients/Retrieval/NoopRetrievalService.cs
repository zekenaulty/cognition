using Cognition.Contracts;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Retrieval;

public class NoopRetrievalService : IRetrievalService
{
    private readonly ILogger<NoopRetrievalService> _logger;
    public NoopRetrievalService(ILogger<NoopRetrievalService> logger) { _logger = logger; }

    public Task<IReadOnlyList<(string Id, string Content, double Score)>> SearchAsync(
        ScopeToken scope,
        string query,
        int k = 8,
        IDictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Noop retrieval: {Query} scope: {@Scope}", query, scope);
        IReadOnlyList<(string, string, double)> empty = Array.Empty<(string, string, double)>();
        return Task.FromResult(empty);
    }

    public Task<bool> WriteAsync(
        ScopeToken scope,
        string content,
        IDictionary<string, object?>? metadata = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Noop write: len={Len} scope: {@Scope}", content?.Length ?? 0, scope);
        return Task.FromResult(true);
    }
}

