using System.Collections.Generic;
using System.Text.Json;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Utils;
using FluentAssertions;
using Xunit;

namespace Cognition.Data.Vectors.Tests.OpenSearch.Utils;

public class QueryDslBuilderTests
{
    [Fact]
    public void BuildKnnQuery_ShouldIncludeTenantKindAndFilters()
    {
        var filters = new Dictionary<string, object>
        {
            ["AgentId"] = "agent",
            ["Custom"] = 42
        };

        var body = QueryDslBuilder.BuildKnnQuery(
            embeddingField: "embedding",
            queryVector: new[] { 0.1f, 0.2f },
            topK: 3,
            tenantKey: "tenant",
            kind: "knowledge",
            filters: filters);

        var json = JsonSerializer.Serialize(body);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("size").GetInt32().Should().Be(3);
        root.GetProperty("knn").GetProperty("field").GetString().Should().Be("embedding");
        root.GetProperty("knn").GetProperty("num_candidates").GetInt32().Should().BeGreaterThan(3);

        var filter = root.GetProperty("query").GetProperty("bool").GetProperty("filter");
        filter.GetArrayLength().Should().Be(1 + 1 + filters.Count);
        filter[0].GetProperty("term").GetProperty("tenantKey").GetString().Should().Be("tenant");
        filter[1].GetProperty("term").GetProperty("kind").GetString().Should().Be("knowledge");
        filter[2].GetProperty("term").GetProperty("metadata.AgentId").GetString().Should().Be("agent");
        filter[3].GetProperty("term").GetProperty("metadata.Custom").GetInt32().Should().Be(42);
    }

    [Fact]
    public void BuildKnnQuery_ShouldOmitKindAndFilters_WhenNotProvided()
    {
        var body = QueryDslBuilder.BuildKnnQuery(
            embeddingField: "embedding",
            queryVector: new[] { 1f },
            topK: 2,
            tenantKey: "tenant",
            kind: null,
            filters: null);

        var json = JsonSerializer.Serialize(body);
        using var doc = JsonDocument.Parse(json);
        var filter = doc.RootElement.GetProperty("query").GetProperty("bool").GetProperty("filter");

        filter.GetArrayLength().Should().Be(1);
        filter[0].GetProperty("term").GetProperty("tenantKey").GetString().Should().Be("tenant");
    }
}
