using System.Net.Http;

namespace Cognition.Clients.LLM;

public class GeminiTextClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly string _model;

    public GeminiTextClient(HttpClient http, string model)
    {
        _http = http;
        _model = model;
        // API key: GOOGLE_API_KEY
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _http.DefaultRequestHeaders.Remove("x-goog-api-key");
            _http.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
        }
    }

    public async Task<string> GenerateAsync(string prompt, bool track = false)
    {
        // Minimal placeholder to avoid external dependency specifics
        await Task.Delay(0);
        return $"[Gemini:{_model}] {prompt}";
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, bool track = false)
    {
        yield return await GenerateAsync(prompt, track);
    }
}

