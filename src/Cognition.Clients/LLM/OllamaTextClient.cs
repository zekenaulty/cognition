using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Cognition.Clients.LLM;

public class OllamaTextClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _model;

    public OllamaTextClient(HttpClient http, string baseUrl, string model)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
        _http.Timeout = TimeSpan.FromSeconds(120);
    }

    private record OllamaRequest([property: JsonPropertyName("model")] string Model,
                                 [property: JsonPropertyName("prompt")] string Prompt,
                                 [property: JsonPropertyName("stream")] bool Stream = false);

    private record OllamaResponse([property: JsonPropertyName("response")] string Response);

    public async Task<string> GenerateAsync(string prompt, bool track = false)
    {
        var req = new OllamaRequest(_model, prompt, false);
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/api/generate", req);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<OllamaResponse>();
        return body?.Response ?? string.Empty;
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, bool track = false)
    {
        var r = await GenerateAsync(prompt, track);
        yield return r;
    }
}

