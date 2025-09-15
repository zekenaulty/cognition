using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Cognition.Clients.LLM;

public class GeminiTextClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _apiBase;

    public GeminiTextClient(HttpClient http, string model, string? apiKey = null, string? apiBase = null)
    {
        _http = http;
        _model = model;
        _apiBase = string.IsNullOrWhiteSpace(apiBase)
            ? Environment.GetEnvironmentVariable("GEMINI_BASE_URL")?.TrimEnd('/') ?? "https://generativelanguage.googleapis.com"
            : apiBase.TrimEnd('/');
        var key = !string.IsNullOrWhiteSpace(apiKey)
            ? apiKey
            : Environment.GetEnvironmentVariable("GOOGLE_API_KEY") ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(key))
        {
            _http.DefaultRequestHeaders.Remove("x-goog-api-key");
            _http.DefaultRequestHeaders.Add("x-goog-api-key", key);
        }
        _http.Timeout = TimeSpan.FromSeconds(90);
    }

    public async Task<string> GenerateAsync(string prompt, bool track = false)
        => await ChatAsync(new[] { new ChatMessage("user", prompt) }, track);

    public async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, bool track = false)
    {
        await foreach (var c in ChatStreamAsync(new[] { new ChatMessage("user", prompt) }, track))
            yield return c;
    }

    private static object ToGeminiContent(IEnumerable<ChatMessage> messages)
    {
        // Gemini expects list of contents with role and parts [{text:...}]
        var contents = messages.Select(m => new
        {
            role = m.Role == "assistant" ? "model" : "user",
            parts = new object[] { new { text = m.Content } }
        });
        return new { contents };
    }

    public async Task<string> ChatAsync(IEnumerable<ChatMessage> messages, bool track = false)
    {
        var url = $"{_apiBase}/v1beta/models/{_model}:generateContent";
        var body = ToGeminiContent(messages);
        var resp = await _http.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        // Extract candidates[0].content.parts[0].text
        if (root.TryGetProperty("candidates", out var cands) && cands.GetArrayLength() > 0)
        {
            var parts = cands[0].GetProperty("content").GetProperty("parts");
            if (parts.GetArrayLength() > 0 && parts[0].TryGetProperty("text", out var t))
                return t.GetString() ?? string.Empty;
        }
        return string.Empty;
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(IEnumerable<ChatMessage> messages, bool track = false)
    {
        var url = $"{_apiBase}/v1beta/models/{_model}:streamGenerateContent";
        var body = ToGeminiContent(messages);
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            // Accept both raw JSON lines and SSE-style "data: {json}"
            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                line = line.Substring("data:".Length).Trim();
            string? toYield = null;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (root.TryGetProperty("candidates", out var cands))
                {
                    foreach (var cand in cands.EnumerateArray())
                    {
                        if (cand.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts))
                        {
                            foreach (var part in parts.EnumerateArray())
                            {
                                if (part.TryGetProperty("text", out var t))
                                {
                                    var txt = t.GetString();
                                    if (!string.IsNullOrEmpty(txt)) { toYield = txt; break; }
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(toYield)) break;
                    }
                }
            }
            catch { /* ignore malformed */ }
            if (!string.IsNullOrEmpty(toYield)) yield return toYield!;
        }
    }
}
