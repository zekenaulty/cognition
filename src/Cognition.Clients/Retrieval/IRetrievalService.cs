using Cognition.Contracts;

namespace Cognition.Clients.Retrieval;

public interface IRetrievalService
{
    Task<IReadOnlyList<(string Id, string Content, double Score)>> SearchAsync(
        ScopeToken scope,
        string query,
        int k = 8,
        IDictionary<string, object?>? filters = null,
        CancellationToken ct = default);

    Task<bool> WriteAsync(
        ScopeToken scope,
        string content,
        IDictionary<string, object?>? metadata = null,
        CancellationToken ct = default);
}

