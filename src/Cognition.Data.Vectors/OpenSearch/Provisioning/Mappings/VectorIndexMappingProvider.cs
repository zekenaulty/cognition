using System.Collections.Generic;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Provisioning.Mappings;

public static class VectorIndexMappingProvider
{
    public static object BuildIndexRequestBody(OpenSearchVectorsOptions options, string embeddingField = "embedding")
    {
        var body = new
        {
            settings = new Dictionary<string, object>
            {
                ["index.knn"] = true,
            },
            mappings = new
            {
                properties = new Dictionary<string, object>
                {
                    ["id"] = new { type = "keyword" },
                    ["tenantKey"] = new { type = "keyword" },
                    ["kind"] = new { type = "keyword" },
                    ["text"] = new { type = "text" },
                    ["scopePath"] = new { type = "keyword" },
                    ["scopePrincipalType"] = new { type = "keyword" },
                    ["scopePrincipalId"] = new { type = "keyword" },
                    ["scopeSegments"] = new { type = "object", enabled = true },
                    [embeddingField] = new
                    {
                        type = "knn_vector",
                        dimension = options.Dimension,
                        method = new { name = "hnsw", space_type = "cosinesimil" }
                    },
                    ["metadata"] = new { type = "object", enabled = true },
                    ["schemaVersion"] = new { type = "integer" }
                }
            }
        };

        return body;
    }
}
