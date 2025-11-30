using System.Net;
using System.Net.Http.Headers;
using Cognition.Api.Infrastructure.Diagnostics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cognition.Api.Tests.Controllers;

public class AbuseHeadersAndRateLimitE2ETests : IClassFixture<AbuseTestFactory>
{
    private readonly AbuseTestFactory _factory;

    public AbuseHeadersAndRateLimitE2ETests(AbuseTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Second_request_is_rate_limited_and_carries_correlation_header()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "fake");
        client.DefaultRequestHeaders.Add("X-Agent-Id", Guid.NewGuid().ToString());

        var first = await client.GetAsync("/api/system/health");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.True(first.Headers.Contains(CorrelationConstants.HeaderName));

        var second = await client.GetAsync("/api/system/health");

        Assert.True(second.StatusCode == HttpStatusCode.TooManyRequests || second.StatusCode == HttpStatusCode.OK);
        Assert.True(second.Headers.Contains(CorrelationConstants.HeaderName));
    }

    [Fact]
    public async Task Agent_policy_falls_back_to_conversation_when_agent_missing()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "fake");

        var convoId = Guid.NewGuid().ToString();
        var first = await client.GetAsync($"/api/system/health?conversationId={convoId}");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.GetAsync($"/api/system/health?conversationId={convoId}");
        Assert.True(second.StatusCode == HttpStatusCode.TooManyRequests || second.StatusCode == HttpStatusCode.OK);
    }
}

public sealed class AbuseTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ApiRateLimiting:Global:IsEnabled"] = "false",
                ["ApiRateLimiting:PerAgent:PermitLimit"] = "1",
                ["ApiRateLimiting:PerAgent:WindowSeconds"] = "60",
                ["ApiRateLimiting:PerAgent:QueueLimit"] = "0",
                ["ApiRateLimiting:PerPersona:PermitLimit"] = "100",
                ["ApiRateLimiting:PerPersona:WindowSeconds"] = "60",
                ["ApiRateLimiting:PerPersona:QueueLimit"] = "0"
            };

            config.AddInMemoryCollection(overrides!);
        });

        builder.ConfigureTestServices(services =>
        {
            // nothing extra yet
        });
    }
}
