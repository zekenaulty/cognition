using System.Security.Claims;
using Cognition.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Cognition.Api.Tests.Infrastructure;

public class ApiRateLimiterPartitionKeysTests
{
    [Fact]
    public void ResolveUserId_ReturnsNameIdentifier_WhenAuthenticated()
    {
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        }, "test");
        context.User = new ClaimsPrincipal(identity);

        var result = ApiRateLimiterPartitionKeys.ResolveUserId(context);

        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void ResolvePersonaId_PrefersHeaderValue()
    {
        var personaId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Persona-Id"] = personaId.ToString();

        var result = ApiRateLimiterPartitionKeys.ResolvePersonaId(context);

        Assert.Equal(personaId.ToString("N"), result);
    }

    [Fact]
    public void ResolveAgentId_FallsBackToQueryString()
    {
        var agentId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString($"?agentId={agentId}");

        var result = ApiRateLimiterPartitionKeys.ResolveAgentId(context);

        Assert.Equal(agentId.ToString("N"), result);
    }
}
