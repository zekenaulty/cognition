using Cognition.Clients.Scope;
using Cognition.Contracts;
using Cognition.Contracts.Scopes;
using FluentAssertions;
using Cognition.Testing.Utilities;
using Xunit;

namespace Cognition.Clients.Tests.Scopes;

public class ScopePathTests
{
    [Fact]
    public void ToScopePath_ShouldSelectAgentPrincipal_AndNormalizeSegments()
    {
        var tenantId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var personaId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        var token = new ScopeToken(tenantId, appId, personaId, agentId, conversationId, null, null);

        var builder = ScopePathBuilderTestHelper.CreateBuilder();
        var path = builder.Build(token);

        path.Principal.Should().Be(new ScopePrincipal(agentId, "agent"));
        path.Segments.Should().Contain(new ScopeSegment("tenant", tenantId.ToString("D")));
        path.Segments.Should().Contain(new ScopeSegment("app", appId.ToString("D")));
        path.Segments.Should().Contain(new ScopeSegment("persona", personaId.ToString("D")));
        path.Segments.Should().Contain(new ScopeSegment("conversation", conversationId.ToString("D")));
        path.Canonical.Should().StartWith($"agent:{agentId:D}|".Replace("|", "/"));
        path.Canonical.Should().Contain($"conversation={conversationId:D}");
    }

    [Fact]
    public void ToScopePath_ShouldFallbackToPersonaPrincipal_WhenAgentMissing()
    {
        var personaId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var token = new ScopeToken(null, null, personaId, null, null, projectId, null);

        var builder = ScopePathBuilderTestHelper.CreateBuilder();
        var path = builder.Build(token);

        path.Principal.Should().Be(new ScopePrincipal(personaId, "persona"));
        path.Segments.Should().ContainSingle(s => s.Key == "project" && s.Value == projectId.ToString("D"));
        path.Canonical.Should().Be($"persona:{personaId:D}/project={projectId:D}");
    }

    [Fact]
    public void ScopeSegment_ShouldNormalizeKeyAndTrimValue()
    {
        var segment = new ScopeSegment(" Conversation ", "  Value  ");

        segment.Key.Should().Be("conversation");
        segment.Value.Should().Be("Value");
        segment.Canonical.Should().Be("conversation=Value");
    }
}
