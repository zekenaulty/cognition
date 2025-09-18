using System.Text.Json.Nodes;

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
                // Map to metadata.<key> as term filter
                filterClauses.Add(new
                {
                    term = new Dictionary<string, object?>
                    {
                        [$"metadata.{kv.Key}"] = kv.Value
                    }
                });
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
    }
}

