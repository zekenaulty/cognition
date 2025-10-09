using System.Reflection;
using Cognition.Api.Controllers;
using Cognition.Data.Relational.Modules.Knowledge;
using FluentAssertions;
using Xunit;

namespace Cognition.Api.Tests.Controllers;

public class KnowledgeIndexControllerMetadataTests
{
    [Fact]
    public void BuildMetadata_ShouldCaptureAllFields()
    {
        var item = new KnowledgeItem
        {
            ContentType = KnowledgeContentType.Concept,
            Categories = new[] { "ml" },
            Keywords = new[] { "embedding" },
            Source = "docs",
            Timestamp = new DateTime(2024, 10, 05, 9, 0, 0, DateTimeKind.Utc),
            Properties = new Dictionary<string, object?> { ["importance"] = "high" }
        };

        var metadata = InvokeBuildMetadata(item);

        metadata.Should().Contain(new KeyValuePair<string, object>("contentType", "Concept"));
        metadata["categories"].Should().BeEquivalentTo(item.Categories);
        metadata["keywords"].Should().BeEquivalentTo(item.Keywords);
        metadata["source"].Should().Be("docs");
        metadata["timestamp"].Should().Be(item.Timestamp);
        metadata["properties"].Should().BeEquivalentTo(item.Properties);
    }

    [Fact]
    public void BuildMetadata_ShouldOmitEmptyOptionalValues()
    {
        var item = new KnowledgeItem
        {
            ContentType = KnowledgeContentType.Other,
            Categories = null,
            Keywords = Array.Empty<string>(),
            Source = "",
            Properties = new Dictionary<string, object?>()
        };

        var metadata = InvokeBuildMetadata(item);

        metadata.Keys.Should().NotContain(new[] { "categories", "keywords", "source", "properties" });
        metadata.Should().ContainKey("timestamp");
    }

    private static Dictionary<string, object> InvokeBuildMetadata(KnowledgeItem item)
    {
        var method = typeof(KnowledgeIndexController)
            .GetMethod("BuildMetadata", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildMetadata method not found");

        return (Dictionary<string, object>)method.Invoke(null, new object[] { item })!;
    }
}
