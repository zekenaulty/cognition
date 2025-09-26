using System.Security.Cryptography;
using System.Text;
using Cognition.Contracts;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;

namespace Cognition.Clients.Retrieval;

public sealed class RetrievalService : IRetrievalService
{
    private readonly IVectorStore _store;
    private readonly ILogger<RetrievalService> _logger;
    private readonly OpenSearchVectorsOptions _options;
    private readonly Cognition.Clients.LLM.IEmbeddingsClient _emb;

    public RetrievalService(IVectorStore store, IOptions<OpenSearchVectorsOptions> options, ILogger<RetrievalService> logger, Cognition.Clients.LLM.IEmbeddingsClient emb)
    {
        _store = store;
        _logger = logger;
        _options = options.Value;
        _emb = emb;
    }

    public async Task<IReadOnlyList<(string Id, string Content, double Score)>> SearchAsync(
        ScopeToken scope,
        string query,
        int k = 8,
        IDictionary<string, object?>? filters = null,
        CancellationToken ct = default)
    {
        // Build strict filters
        var baseFilters = new Dictionary<string, object?>();
        if (filters is not null) foreach (var kv in filters) baseFilters[kv.Key] = kv.Value;

        var tenantKey = ResolveTenantKey(scope);
        // Compute query embedding via embeddings client
        var embedding = await _emb.EmbedAsync(query, ct).ConfigureAwait(false);
        if (embedding.Length == 0)
        {
            _logger.LogWarning("Embedding returned empty vector; query='{Query}'", query);
            return Array.Empty<(string, string, double)>();
        }

        var results = new List<(string Id, string Content, double Score)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1) Conversation-scoped search
        if (scope.ConversationId.HasValue)
        {
            var f = new Dictionary<string, object?>(baseFilters)
            {
                ["ConversationId"] = scope.ConversationId.Value.ToString()
            };
            var convHits = await _store.SimilaritySearchAsync(embedding, k, tenantKey, f, kind: null, ct).ConfigureAwait(false);
            foreach (var h in convHits)
            {
                if (seen.Add(h.Item.Id)) results.Add((h.Item.Id, h.Item.Text, h.Score));
            }
        }

        // 2) Fallback to Agent root if needed
        if (results.Count < k && scope.AgentId.HasValue)
        {
            var need = k - results.Count;
            var f = new Dictionary<string, object?>(baseFilters)
            {
                ["AgentId"] = scope.AgentId.Value.ToString()
            };
            var agentHits = await _store.SimilaritySearchAsync(embedding, need, tenantKey, f, kind: null, ct).ConfigureAwait(false);
            foreach (var h in agentHits)
            {
                if (seen.Add(h.Item.Id)) results.Add((h.Item.Id, h.Item.Text, h.Score));
            }
        }

        return results;
    }

    public async Task<bool> WriteAsync(
        ScopeToken scope,
        string content,
        IDictionary<string, object?>? metadata = null,
        CancellationToken ct = default)
    {
        // Default write target: Conversation level if available; otherwise Agent level
        var kind = scope.ConversationId.HasValue ? "conversation" : "agent";
        var tenantKey = ResolveTenantKey(scope);

        var meta = BuildScopeMetadata(scope, metadata);
        var id = ComputeContentHash(content, scope, meta);

        var item = new VectorItem
        {
            Id = id,
            TenantKey = tenantKey,
            Kind = kind,
            Text = content,
            Embedding = _options.UseEmbeddingPipeline ? null : null, // require pipeline; else not supported here
            Metadata = meta,
            SchemaVersion = 1
        };

        if (!_options.UseEmbeddingPipeline)
        {
            _logger.LogWarning("OpenSearch.UseEmbeddingPipeline=false but no embedding provider is wired. Skipping write for id {Id}", id);
            return false;
        }

        await _store.UpsertAsync(item, ct).ConfigureAwait(false);
        return true;
    }

    private static string ResolveTenantKey(ScopeToken scope)
    {
        // Prefer TenantId, else AppId, else default
        if (scope.TenantId.HasValue) return scope.TenantId.Value.ToString();
        if (scope.AppId.HasValue) return scope.AppId.Value.ToString();
        return "default";
    }

    private static Dictionary<string, object?> BuildScopeMetadata(ScopeToken scope, IDictionary<string, object?>? extra)
    {
        var m = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["TenantId"] = scope.TenantId?.ToString(),
            ["AppId"] = scope.AppId?.ToString(),
            ["PersonaId"] = scope.PersonaId?.ToString(),
            ["AgentId"] = scope.AgentId?.ToString(),
            ["ConversationId"] = scope.ConversationId?.ToString(),
            ["ProjectId"] = scope.ProjectId?.ToString(),
            ["WorldId"] = scope.WorldId?.ToString(),
        };
        if (extra is not null)
        {
            foreach (var kv in extra) m[kv.Key] = kv.Value;
        }
        return m;
    }

    private static string ComputeContentHash(string content, ScopeToken scope, IDictionary<string, object?> meta)
    {
        var sb = new StringBuilder();
        sb.AppendLine(content.Trim());
        void Add(string k, object? v) { if (v is null) return; sb.Append('|').Append(k).Append('=').Append(v); }
        Add("TenantId", scope.TenantId);
        Add("AppId", scope.AppId);
        Add("AgentId", scope.AgentId);
        Add("ConversationId", scope.ConversationId);
        Add("ProjectId", scope.ProjectId);
        Add("WorldId", scope.WorldId);
        if (meta.TryGetValue("Source", out var src)) Add("Source", src);
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
