using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Cognition.Clients.LLM;

public class OpenAITextClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OpenAITextClient(HttpClient http, string model)
    {
        _http = http;
        _model = model;
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _http.DefaultRequestHeaders.Remove("Authorization");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
        _http.Timeout = TimeSpan.FromSeconds(45);
    }

    public async Task<string> GenerateAsync(string prompt, bool track = false)
    {
        var payload = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "user", content = prompt }
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var choices = root.GetProperty("choices");
        if (choices.GetArrayLength() > 0)
        {
            return choices[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, bool track = false)
    {
        // For now, fall back to non-streaming
        var full = await GenerateAsync(prompt, track);
        yield return full;
    }
}

