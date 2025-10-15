using System;
using System.Collections.Generic;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Utils;

public static class QueryDslBuilder
{
    public static object BuildKnnQuery(
        string embeddingField,
        float[] queryVector,
        int topK,
        string tenantKey,
        string? kind,
        IDictionary<string, object>? filters)
    {
        var filterClauses = new List<object>
        {
            new { term = new Dictionary<string, object?> { ["tenantKey"] = tenantKey } }
        };

        if (!string.IsNullOrWhiteSpace(kind))
            filterClauses.Add(new { term = new Dictionary<string, object?> { ["kind"] = kind } });

        if (filters is not null)
        {
            foreach (var kv in filters)
            {
                if (kv.Value is null)
                {
                    continue;
                }

                if (string.Equals(kv.Key, "ScopePath", StringComparison.OrdinalIgnoreCase))
                {
                    filterClauses.Add(Term("scopePath", kv.Value));
                    filterClauses.Add(Term("metadata.ScopePath", kv.Value));
                    continue;
                }

                if (string.Equals(kv.Key, "ScopePrincipalType", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Key, "ScopePrincipalId", StringComparison.OrdinalIgnoreCase))
                {
                    var fieldName = $"scope{kv.Key["Scope".Length..]}";
                    filterClauses.Add(Term(fieldName, kv.Value));
                    filterClauses.Add(Term($"metadata.{kv.Key}", kv.Value));
                    continue;
                }

                if (string.Equals(kv.Key, "ScopeSegments", StringComparison.OrdinalIgnoreCase) &&
                    kv.Value is IDictionary<string, object?> segments)
                {
                    foreach (var segment in segments)
                    {
                        if (segment.Value is null) continue;
                        filterClauses.Add(Term($"scopeSegments.{segment.Key}", segment.Value));
                        filterClauses.Add(Term($"metadata.ScopeSegments.{segment.Key}", segment.Value));
                    }
                    continue;
                }

                filterClauses.Add(Term($"metadata.{kv.Key}", kv.Value));
            }
        }

        var body = new
        {
            size = topK,
            query = new
            {
                @bool = new
                {
                    filter = filterClauses
                }
            },
            knn = new
            {
                field = embeddingField,
                query_vector = queryVector,
                k = topK,
                num_candidates = Math.Max(topK * 4, 50)
            }
        };

        return body;

        static object Term(string field, object? value) => new
        {
            term = new Dictionary<string, object?>
            {
                [field] = value
            }
        };
    }
}
