using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Sandbox;
using Cognition.Data.Relational.Modules.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cognition.Clients.Tests.Tools;

public class SandboxAlertPublisherTests
{
    [Fact]
    public async Task PublishesWebhook_when_enabled_and_denied()
    {
        var sent = new List<HttpRequestMessage>();
        var handler = new RecordingHandler(sent, new HttpResponseMessage(HttpStatusCode.OK));
        var clientFactory = new FakeHttpClientFactory(handler);

        var options = Options.Create(new ToolSandboxAlertOptions
        {
            Enabled = true,
            WebhookUrl = "http://localhost/hook"
        });

        var publisher = new LoggerSandboxAlertPublisher(NullLogger<LoggerSandboxAlertPublisher>.Instance, options, clientFactory);
        var decision = SandboxDecision.Deny(SandboxMode.Enforce, "denied");
        var tool = new Tool { Id = Guid.NewGuid(), ClassPath = "Test.Tool" };
        var context = new ToolContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new ServiceCollection().BuildServiceProvider(), CancellationToken.None);

        await publisher.PublishAsync(decision, tool, context, CancellationToken.None);

        sent.Should().HaveCount(1);
        sent[0].RequestUri.Should().Be(options.Value.WebhookUrl);
        var payload = await sent[0].Content!.ReadFromJsonAsync<Dictionary<string, object?>>();
        payload.Should().NotBeNull();
        payload!["toolId"]!.ToString().Should().Be(tool.Id.ToString());
        payload["reason"]!.ToString().Should().Be("denied");
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly List<HttpRequestMessage> _sink;
        private readonly HttpResponseMessage _response;

        public RecordingHandler(List<HttpRequestMessage> sink, HttpResponseMessage response)
        {
            _sink = sink;
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _sink.Add(request);
            return Task.FromResult(_response);
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public FakeHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name = "") => new HttpClient(_handler, disposeHandler: false);
    }
}
