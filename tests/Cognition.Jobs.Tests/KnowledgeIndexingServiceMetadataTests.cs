using System.Reflection;
using Cognition.Data.Relational.Modules.Knowledge;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;
using Cognition.Jobs;
using FluentAssertions;
using Xunit;

namespace Cognition.Jobs.Tests;

public class KnowledgeIndexingServiceMetadataTests
{
    [Fact]
    public void BuildMetadata_ShouldCaptureAllRelevantFields()
    {
        var item = new KnowledgeItem
        {
            ContentType = KnowledgeContentType.Fact,
            Categories = new[] { "physics", "science" },
            Keywords = new[] { "gravity" },
            Source = "wikipedia",
            Timestamp = new DateTime(2024, 05, 01, 12, 0, 0, DateTimeKind.Utc),
            Properties = new Dictionary<string, object?>
            {
                ["author"] = "einstein",
                ["confidence"] = 0.9
            }
        };

        var metadata = InvokeBuildMetadata(item);

        metadata.Should().ContainKeys("contentType", "categories", "keywords", "source", "timestamp", "properties");
        metadata["contentType"].Should().Be("Fact");
        metadata["categories"].Should().BeEquivalentTo(item.Categories);
        metadata["keywords"].Should().BeEquivalentTo(item.Keywords);
        metadata["source"].Should().Be("wikipedia");
        metadata["timestamp"].Should().Be(item.Timestamp);
        metadata["properties"].Should().BeEquivalentTo(item.Properties);
    }

    [Fact]
    public void BuildMetadata_ShouldOmitEmptyOptionalFields()
    {
        var item = new KnowledgeItem
        {
            ContentType = KnowledgeContentType.Other,
            Categories = Array.Empty<string>(),
            Keywords = null,
            Source = string.Empty,
            Properties = new Dictionary<string, object?>()
        };

        var metadata = InvokeBuildMetadata(item);

        metadata.Should().ContainKey("contentType").WhoseValue.Should().Be("Other");
        metadata.Should().ContainKey("timestamp");
        metadata.Keys.Should().NotContain(new[] { "categories", "keywords", "source", "properties" });
    }

    [Fact]
    public void ApplyScopeFromMetadata_ShouldPopulateVectorItem()
    {
        var agentId = Guid.NewGuid();
        var properties = new Dictionary<string, object?>
        {
            ["AgentId"] = agentId,
            ["ConversationId"] = Guid.NewGuid()
        };

        var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var vector = new VectorItem { Metadata = metadata };

        var applied = vector.ApplyScopeFromMetadata(properties, metadata);

        applied.Should().BeTrue();
        vector.ScopePrincipalType.Should().Be("agent");
        vector.ScopePrincipalId.Should().Be(agentId.ToString("D"));
        vector.ScopeSegments.Should().NotBeNull();
        metadata.Should().ContainKey("ScopePath");
    }

    private static Dictionary<string, object> InvokeBuildMetadata(KnowledgeItem item)
    {
        var method = typeof(KnowledgeIndexingService)
            .GetMethod("BuildMetadata", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildMetadata method not found");

        return (Dictionary<string, object>)method.Invoke(null, new object[] { item })!;
    }
}
