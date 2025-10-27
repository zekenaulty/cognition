using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Cognition.Clients.LLM;
using FluentAssertions;
using Xunit;

namespace Cognition.Clients.Tests.LLM;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class OllamaFactAttribute : FactAttribute
{
    public OllamaFactAttribute()
    {
        if (!OllamaTestContext.Enabled)
        {
            Skip = OllamaTestContext.SkipReason ?? "Ollama integration tests are disabled.";
        }
    }
}

internal static class OllamaTestContext
{
    public static readonly bool Enabled;
    public static readonly string? SkipReason;
    public static readonly string BaseUrl;
    public static readonly string Model;

    static OllamaTestContext()
    {
        const string defaultBase = "http://localhost:11434";
        const string defaultModel = "gpt-oss:20b";

        var enabled = Environment.GetEnvironmentVariable("OLLAMA_TEST_ENABLED");
        if (!string.Equals(enabled, "1", StringComparison.OrdinalIgnoreCase))
        {
            Enabled = false;
            SkipReason = "Set OLLAMA_TEST_ENABLED=1 to run Ollama integration tests.";
            BaseUrl = defaultBase;
            Model = defaultModel;
            return;
        }

        BaseUrl = (Environment.GetEnvironmentVariable("OLLAMA_TEST_BASE_URL")
                    ?? Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")
                    ?? defaultBase).TrimEnd('/');
        Model = Environment.GetEnvironmentVariable("OLLAMA_TEST_MODEL") ?? defaultModel;

        try
        {
            using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var response = probe.GetAsync($"{BaseUrl}/api/tags").GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            Enabled = true;
        }
        catch (Exception ex)
        {
            Enabled = false;
            SkipReason = $"Ollama server not reachable at {BaseUrl}: {ex.Message}";
        }
    }
}

internal static class OllamaTestHelper
{
    public static Task<OllamaTextClient> CreateClientAsync()
    {
        if (!OllamaTestContext.Enabled)
        {
            throw new InvalidOperationException("Ollama integration tests are not enabled.");
        }

        var http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        return Task.FromResult(new OllamaTextClient(
            http,
            OllamaTestContext.BaseUrl,
            OllamaTestContext.Model,
            OllamaTextClient.OllamaOptions.FromEnvironment()));
    }
}

public class OllamaTextClientIntegrationTests
{
    [OllamaFact]
    public async Task ChatAsync_ReturnsResponse_WhenOllamaAvailable()
    {
        var client = await OllamaTestHelper.CreateClientAsync();
        var reply = await client.ChatAsync(new[]
        {
            new ChatMessage("system", "You are running an integration test. Respond with the exact phrase 'integration-check' and nothing else."),
            new ChatMessage("user", "Please respond now.")
        });

        reply.Should().NotBeNullOrWhiteSpace();
        reply.Trim().Should().Be("integration-check");
    }

    [OllamaFact]
    public async Task ChatStreamAsync_StreamsChunks_WhenOllamaAvailable()
    {
        var client = await OllamaTestHelper.CreateClientAsync();
        var chunks = new List<string>();

        await foreach (var chunk in client.ChatStreamAsync(new[]
        {
            new ChatMessage("system", "You are running an integration test. Respond with the exact phrase 'integration-check' and nothing else."),
            new ChatMessage("user", "Please respond now.")
        }))
        {
            chunks.Add(chunk);
        }

        chunks.Should().NotBeEmpty();
        string combined = string.Concat(chunks).Trim();
        combined.Should().Be("integration-check");
    }
}
