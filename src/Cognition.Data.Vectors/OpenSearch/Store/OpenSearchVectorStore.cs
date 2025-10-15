using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using OpenSearch.Client;
using OpenSearch.Net;

using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Utils;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Store;

public sealed class OpenSearchVectorStore : IVectorStore
{
    private readonly IOpenSearchClient _client;
    private readonly OpenSearchVectorsOptions _options;
    private readonly ILogger<OpenSearchVectorStore> _logger;
    private readonly Provisioning.OpenSearchProvisioner _provisioner;

    public OpenSearchVectorStore(IOpenSearchClient client, IOptions<OpenSearchVectorsOptions> options, ILogger<OpenSearchVectorStore> logger, Provisioning.OpenSearchProvisioner provisioner)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
        _provisioner = provisioner;
    }

    public async Task EnsureProvisionedAsync(CancellationToken ct)
    {
        await _provisioner.EnsureProvisionedAsync(ct).ConfigureAwait(false);
    }

    public async Task UpsertAsync(VectorItem item, CancellationToken ct)
    {
        Guard.NotNullOrEmpty(item.Id, nameof(item.Id));
        Guard.NotNullOrEmpty(item.TenantKey, nameof(item.TenantKey));

        if (!_options.UseEmbeddingPipeline)
            Guard.EnsureDimension(item.Embedding, _options.Dimension);

        var index = ResolveIndex(item.TenantKey, item.Kind);
        var doc = BuildDoc(item);

        var response = await _client.IndexAsync<object>(doc, i =>
            {
                i.Index(index).Id(item.Id);
                if (_options.UseEmbeddingPipeline && item.Embedding is null)
                    i.Pipeline(_options.PipelineId);
                return i;
            }, ct).ConfigureAwait(false);

        if (!response.IsValid)
            throw new InvalidOperationException($"Index failed: {response.ServerError?.Error?.Reason ?? response.DebugInformation}");
    }

    public async Task UpsertManyAsync(IEnumerable<VectorItem> items, CancellationToken ct)
    {
        var list = items.ToList();
        if (list.Count == 0) return;
        var index = ResolveIndex(list[0].TenantKey, list[0].Kind);
        var docs = list.Select(BuildDoc).ToList();
        var pipeline = _options.UseEmbeddingPipeline ? _options.PipelineId : null;
        var bulk = new BulkHelper(_client, _options);
        await bulk.IndexChunksAsync(docs, index, pipeline, 1000, ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id, string tenantKey, string? kind, CancellationToken ct)
    {
        var index = ResolveIndex(tenantKey, kind);
        var resp = await _client.DeleteAsync<object>(id, d => d.Index(index), ct).ConfigureAwait(false);
        if (resp.ServerError is not null)
            _logger.LogWarning("Delete error: {Error}", resp.ServerError?.Error?.Reason);
    }

    public async Task<IReadOnlyList<SearchResult>> SimilaritySearchAsync(float[] queryEmbedding, int topK, string tenantKey, IDictionary<string, object>? filters, string? kind, CancellationToken ct)
    {
        Guard.EnsureDimension(queryEmbedding, _options.Dimension);
        var index = ResolveIndex(tenantKey, kind);
        var body = QueryDslBuilder.BuildKnnQuery("embedding", queryEmbedding, topK, tenantKey, kind, filters);
        var response = await _client.LowLevel.SearchAsync<StringResponse>(index, PostData.Serializable(body)).ConfigureAwait(false);
        if (response.HttpStatusCode is < 200 or >= 300)
            throw new InvalidOperationException($"Search failed: {response.Body}");

        using var doc = JsonDocument.Parse(response.Body!);
        var root = doc.RootElement;
        var hits = root.GetProperty("hits").GetProperty("hits");
        var results = new List<SearchResult>(hits.GetArrayLength());
        foreach (var h in hits.EnumerateArray())
        {
            var score = h.TryGetProperty("_score", out var s) ? s.GetDouble() : 0d;
            var src = h.GetProperty("_source");
            var item = new VectorItem
            {
                Id = src.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty,
                TenantKey = src.TryGetProperty("tenantKey", out var tk) ? tk.GetString() ?? string.Empty : string.Empty,
                Kind = src.TryGetProperty("kind", out var kd) ? kd.GetString() ?? string.Empty : string.Empty,
                Text = src.TryGetProperty("text", out var tx) ? tx.GetString() ?? string.Empty : string.Empty,
                SchemaVersion = src.TryGetProperty("schemaVersion", out var sv) && sv.TryGetInt32(out var svi) ? svi : 1,
                ScopePath = src.TryGetProperty("scopePath", out var sp) ? sp.GetString() : null,
                ScopePrincipalType = src.TryGetProperty("scopePrincipalType", out var spt) ? spt.GetString() : null,
                ScopePrincipalId = src.TryGetProperty("scopePrincipalId", out var spi) ? spi.GetString() : null,
                ScopeSegments = src.TryGetProperty("scopeSegments", out var ss) && ss.ValueKind == JsonValueKind.Object
                    ? ss.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    : null
            };
            results.Add(new SearchResult { Item = item, Score = score });
        }
        return results;
    }

    private string ResolveIndex(string tenantKey, string? kind)
    {
        // Keep single index by default; hook left for future expansion
        return _options.DefaultIndex;
    }

    private static object BuildDoc(VectorItem item)
    {
        var dict = new Dictionary<string, object?>
        {
            ["id"] = item.Id,
            ["tenantKey"] = item.TenantKey,
            ["kind"] = item.Kind,
            ["text"] = item.Text,
            ["schemaVersion"] = item.SchemaVersion
        };

        if (item.Embedding is not null)
            dict["embedding"] = item.Embedding;
        if (item.Metadata is not null)
            dict["metadata"] = item.Metadata;
        if (!string.IsNullOrWhiteSpace(item.ScopePath))
            dict["scopePath"] = item.ScopePath;
        if (!string.IsNullOrWhiteSpace(item.ScopePrincipalType))
            dict["scopePrincipalType"] = item.ScopePrincipalType;
        if (!string.IsNullOrWhiteSpace(item.ScopePrincipalId))
            dict["scopePrincipalId"] = item.ScopePrincipalId;
        if (item.ScopeSegments is not null && item.ScopeSegments.Count > 0)
            dict["scopeSegments"] = item.ScopeSegments;

        return dict;
    }
}
