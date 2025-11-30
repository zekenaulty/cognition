using Cognition.Contracts;
using Cognition.Contracts.Scopes;
using Xunit;

namespace Cognition.Api.Tests.Infrastructure;

public class ScopePathProjectionTests
{
    [Fact]
    public void TryCreate_returns_projection_with_canonical_path()
    {
        var scope = new ScopeToken(
            TenantId: null,
            AppId: null,
            PersonaId: null,
            AgentId: Guid.NewGuid(),
            ConversationId: Guid.NewGuid(),
            PlanId: null,
            ProjectId: null,
            WorldId: null);

        var ok = ScopePathProjection.TryCreate(scope, out var projection);

        Assert.True(ok);
        Assert.False(string.IsNullOrWhiteSpace(projection.Canonical));
        Assert.Equal("agent", projection.PrincipalType);
    }
}
