using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Alerts;
using Cognition.Api.Infrastructure.Planning;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cognition.Api.Tests.Infrastructure;

public class OpsWebhookAlertPublisherTests
{
    [Fact]
    public async Task PublishAsync_respects_route_overrides()
    {
        var handler = new RecordingHandler();
        var client = new HttpClient(handler);
        var factory = new SingleClientFactory(client);
        var options = Options.Create(new OpsAlertingOptions
        {
            Enabled = true,
            WebhookUrl = "https://default-webhook",
            Routes = new Dictionary<string, OpsAlertRoute>
            {
                ["alert:backlog:stale"] = new OpsAlertRoute
                {
                    WebhookUrl = "https://backlog-webhook",
                    RoutingKey = "backlog-route"
                }
            }
        });

        var publisher = new OpsWebhookAlertPublisher(factory, options, NullLogger<OpsWebhookAlertPublisher>.Instance);

        var alerts = new[]
        {
            new PlannerHealthAlert("backlog:stale", PlannerHealthAlertSeverity.Warning, "Stale backlog items", "desc", DateTime.UtcNow)
        };

        await publisher.PublishAsync(alerts, CancellationToken.None);

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri.Should().Be(new Uri("https://backlog-webhook"));

        var payload = handler.ReadPayload(0);
        payload.GetProperty("routingKey").GetString().Should().Be("backlog-route");
        payload.GetProperty("alerts").EnumerateArray().Should().ContainSingle();
    }

    [Fact]
    public async Task PublishAsync_sets_slo_metadata()
    {
        var handler = new RecordingHandler();
        var client = new HttpClient(handler);
        var factory = new SingleClientFactory(client);
        var options = Options.Create(new OpsAlertingOptions
        {
            Enabled = true,
            WebhookUrl = "https://ops",
            AlertSloThresholds = new Dictionary<string, TimeSpan>
            {
                ["alert:planner:recent-failures"] = TimeSpan.Zero
            }
        });

        var publisher = new OpsWebhookAlertPublisher(factory, options, NullLogger<OpsWebhookAlertPublisher>.Instance);

        var alert = new PlannerHealthAlert("planner:recent-failures", PlannerHealthAlertSeverity.Error, "Recent failures", "desc", DateTime.UtcNow);
        await publisher.PublishAsync(new[] { alert }, CancellationToken.None);

        handler.Requests.Should().ContainSingle();
        var record = handler.ReadPayload(0).GetProperty("alerts").EnumerateArray().Single();
        record.GetProperty("sloMinutes").GetDouble().Should().Be(0);
        record.GetProperty("sloBreached").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task PublishAsync_debounces_repeated_alerts()
    {
        var handler = new RecordingHandler();
        var client = new HttpClient(handler);
        var factory = new SingleClientFactory(client);
        var options = Options.Create(new OpsAlertingOptions
        {
            Enabled = true,
            WebhookUrl = "https://ops",
            DebounceWindow = TimeSpan.FromMinutes(10)
        });

        var publisher = new OpsWebhookAlertPublisher(factory, options, NullLogger<OpsWebhookAlertPublisher>.Instance);

        var alert = new PlannerHealthAlert("planner:recent-failures", PlannerHealthAlertSeverity.Error, "Recent failures", "desc", DateTime.UtcNow);

        await publisher.PublishAsync(new[] { alert }, CancellationToken.None);
        await publisher.PublishAsync(new[] { alert }, CancellationToken.None);

        handler.Requests.Should().ContainSingle();
    }

    private sealed class SingleClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public SingleClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly List<HttpRequestMessage> _requests = new();

        public IReadOnlyList<HttpRequestMessage> Requests => _requests;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        public JsonElement ReadPayload(int index)
        {
            var message = _requests[index];
            var json = message.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "{}";
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
    }
}
