using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Cognition.Clients.LLM;

public sealed class OpenAIEmbeddingsClient : IEmbeddingsClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;

    public OpenAIEmbeddingsClient(HttpClient http)
    {
        _http = http;
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        _baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL")?.TrimEnd('/') ?? "https://api.openai.com";
        _model = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") ?? "text-embedding-3-small";
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is not set for embeddings.");
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_baseUrl), "/v1/embeddings"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        var payload = new { model = _model, input = text };
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        using var s = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct).ConfigureAwait(false);
        var root = doc.RootElement;
        var data = root.GetProperty("data");
        if (data.GetArrayLength() == 0) return Array.Empty<float>();
        var vector = data[0].GetProperty("embedding");
        var list = new List<float>(vector.GetArrayLength());
        foreach (var v in vector.EnumerateArray()) list.Add(v.GetSingle());
        return list.ToArray();
    }
}

