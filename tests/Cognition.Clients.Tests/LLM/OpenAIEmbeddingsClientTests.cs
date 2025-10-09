using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Cognition.Clients.LLM;
using Cognition.Testing.Utilities;
using FluentAssertions;
using Xunit;

namespace Cognition.Clients.Tests.LLM;

public class OpenAIEmbeddingsClientTests
{
    [Fact]
    public async Task EmbedAsync_sends_expected_request_and_parses_response()
    {
        using var env = EnvironmentVariableScope.Set(new Dictionary<string, string?>
        {
            ["OPENAI_API_KEY"] = "test-key",
            ["OPENAI_BASE_URL"] = "https://mock.openai.local",
            ["OPENAI_EMBEDDING_MODEL"] = "text-embedding-custom"
        });

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":[{\"embedding\":[0.1,0.2,0.3]}]}", Encoding.UTF8, "application/json")
        };

        var handler = new RecordingHandler(response);
        var client = new OpenAIEmbeddingsClient(new HttpClient(handler));

        var vector = await client.EmbedAsync("hello world");

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri.Should().Be(new Uri("https://mock.openai.local/v1/embeddings"));
        handler.LastRequest.Headers.Authorization?.Parameter.Should().Be("test-key");
        handler.LastRequestContent.Should().NotBeNull();

        JsonDocument.Parse(handler.LastRequestContent!).RootElement.GetProperty("model").GetString()
            .Should().Be("text-embedding-custom");

        vector.Should().BeEquivalentTo(new[] { 0.1f, 0.2f, 0.3f });
    }

    [Fact]
    public void Constructor_throws_when_api_key_missing()
    {
        using var env = EnvironmentVariableScope.Set("OPENAI_API_KEY", null);

        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK));

        Action act = () => new OpenAIEmbeddingsClient(new HttpClient(handler));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OPENAI_API_KEY*");
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHandler(HttpResponseMessage response) => _response = response;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestContent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return ReadContentAndReturnAsync(request, cancellationToken);
        }

        private async Task<HttpResponseMessage> ReadContentAndReturnAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                LastRequestContent = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                LastRequestContent = null;
            }

            return _response;
        }
    }
}
