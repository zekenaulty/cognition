using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Store;

namespace Cognition.Testing.Retrieval;

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly object _sync = new();
    private readonly List<VectorItem> _items = new();

    public List<SimilarityCall> Calls { get; } = new();

    public Task EnsureProvisionedAsync(CancellationToken ct)
        => Task.CompletedTask;

    public Task UpsertAsync(VectorItem item, CancellationToken ct)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        UpsertInternal(item);
        return Task.CompletedTask;
    }

    public Task UpsertManyAsync(IEnumerable<VectorItem> items, CancellationToken ct)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            UpsertInternal(item);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, string tenantKey, string? kind, CancellationToken ct)
    {
        lock (_sync)
        {
            _items.RemoveAll(x =>
                string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.TenantKey, tenantKey, StringComparison.OrdinalIgnoreCase) &&
                (kind is null || string.Equals(x.Kind, kind, StringComparison.OrdinalIgnoreCase)));
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SearchResult>> SimilaritySearchAsync(
        float[] queryEmbedding,
        int topK,
        string tenantKey,
        IDictionary<string, object>? filters,
        string? kind,
        CancellationToken ct)
    {
        List<VectorItem> snapshot;
        lock (_sync)
        {
            snapshot = _items
                .Where(x => string.Equals(x.TenantKey, tenantKey, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(kind))
        {
            snapshot = snapshot
                .Where(x => string.Equals(x.Kind, kind, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (filters is not null)
        {
            foreach (var kv in filters)
            {
                snapshot = snapshot
                    .Where(item => MatchesFilter(item, kv.Key, kv.Value))
                    .ToList();
            }
        }

        var scored = snapshot
            .Take(Math.Max(0, topK))
            .Select(item => new SearchResult
            {
                Item = Clone(item),
                Score = ComputeScore(item, queryEmbedding)
            })
            .ToList();

        Calls.Add(new SimilarityCall(
            Embedding: queryEmbedding.ToArray(),
            TopK: topK,
            TenantKey: tenantKey,
            Filters: filters is null
                ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object>(filters, StringComparer.OrdinalIgnoreCase),
            Kind: kind));

        return Task.FromResult<IReadOnlyList<SearchResult>>(scored);
    }

    public IReadOnlyList<VectorItem> Snapshot()
    {
        lock (_sync)
        {
            return _items.Select(Clone).ToList();
        }
    }

    private static bool MatchesFilter(VectorItem item, string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return true;
        }

        if (string.Equals(key, "AgentId", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(item.Kind, "agent", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        var expected = value?.ToString();
        if (expected is null)
        {
            return true;
        }

        if (string.Equals(key, nameof(VectorItem.Id), StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(item.Id, expected, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(key, nameof(VectorItem.Kind), StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(item.Kind, expected, StringComparison.OrdinalIgnoreCase);
        }

        if (item.Metadata is not null && item.Metadata.TryGetValue(key, out var metaValue))
        {
            var actual = metaValue?.ToString();
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private void UpsertInternal(VectorItem item)
    {
        lock (_sync)
        {
            var clone = Clone(item);
            var index = _items.FindIndex(x => KeysEqual(x, clone));
            if (index >= 0)
            {
                _items[index] = clone;
            }
            else
            {
                _items.Add(clone);
            }
        }
    }

    private static bool KeysEqual(VectorItem left, VectorItem right)
        => string.Equals(left.Id, right.Id, StringComparison.OrdinalIgnoreCase)
           && string.Equals(left.TenantKey, right.TenantKey, StringComparison.OrdinalIgnoreCase)
           && string.Equals(left.Kind, right.Kind, StringComparison.OrdinalIgnoreCase);

    private static VectorItem Clone(VectorItem item)
    {
        return new VectorItem
        {
            Id = item.Id,
            TenantKey = item.TenantKey,
            Kind = item.Kind,
            Text = item.Text,
            Embedding = item.Embedding is null ? null : (float[])item.Embedding.Clone(),
            Metadata = item.Metadata is null
                ? null
                : new Dictionary<string, object>(item.Metadata, StringComparer.OrdinalIgnoreCase),
            SchemaVersion = item.SchemaVersion,
            Extensions = item.Extensions is null
                ? null
                : new Dictionary<string, object>(item.Extensions, StringComparer.OrdinalIgnoreCase),
            ScopePath = item.ScopePath,
            ScopePrincipalType = item.ScopePrincipalType,
            ScopePrincipalId = item.ScopePrincipalId,
            ScopeSegments = item.ScopeSegments is null
                ? null
                : new Dictionary<string, string>(item.ScopeSegments, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static double ComputeScore(VectorItem item, IReadOnlyList<float> embedding)
    {
        if (embedding is { Count: > 0 } && item.Embedding is { Length: > 0 })
        {
            var len = Math.Min(item.Embedding.Length, embedding.Count);
            double dot = 0;
            double itemMagnitude = 0;
            double queryMagnitude = 0;
            for (var i = 0; i < len; i++)
            {
                var itemValue = item.Embedding[i];
                var queryValue = embedding[i];
                dot += itemValue * queryValue;
                itemMagnitude += itemValue * itemValue;
                queryMagnitude += queryValue * queryValue;
            }
            if (itemMagnitude <= 0 || queryMagnitude <= 0)
            {
                return 0;
            }
            return dot / (Math.Sqrt(itemMagnitude) * Math.Sqrt(queryMagnitude));
        }

        return 0.0;
    }

    public record SimilarityCall(float[] Embedding, int TopK, string TenantKey, Dictionary<string, object> Filters, string? Kind);
}
