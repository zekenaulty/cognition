using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Cognition.Clients.LLM;

public class OpenAITextClient : ILLMClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _apiBase;

    public OpenAITextClient(HttpClient http, string model, string? apiKey = null, string? apiBase = null)
    {
        _http = http;
        _model = model;
        _apiBase = string.IsNullOrWhiteSpace(apiBase)
            ? Environment.GetEnvironmentVariable("OPENAI_BASE_URL")?.TrimEnd('/') ?? "https://api.openai.com"
            : apiBase.TrimEnd('/');

        var key = !string.IsNullOrWhiteSpace(apiKey)
            ? apiKey
            : Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? Environment.GetEnvironmentVariable("OPENAI_KEY");
        if (!string.IsNullOrWhiteSpace(key))
        {
            _http.DefaultRequestHeaders.Remove("Authorization");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");
        }
        _http.Timeout = TimeSpan.FromSeconds(90);
    }

    public async Task<string> GenerateAsync(string prompt, bool track = false)
    {
        return await ChatAsync(new[] { new ChatMessage("user", prompt) }, track);
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, bool track = false)
    {
        await foreach (var chunk in ChatStreamAsync(new[] { new ChatMessage("user", prompt) }, track))
        {
            yield return chunk;
        }
    }

    public async Task<string> ChatAsync(IEnumerable<ChatMessage> messages, bool track = false)
    {
        var payload = BuildPayload(messages, stream: false);
        var resp = await SendWithRetryAsync(() => CreateRequest(payload), HttpCompletionOption.ResponseContentRead);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return content ?? string.Empty;
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(IEnumerable<ChatMessage> messages, bool track = false)
    {
        var payload = BuildPayload(messages, stream: true);
        using var resp = await SendWithRetryAsync(() => CreateRequest(payload), HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.Length == 0 || line.StartsWith(":")) continue; // keep-alive/comments
            if (!line.StartsWith("data:")) continue;
            var data = line.Substring("data:".Length).Trim();
            if (data == "[DONE]") yield break;
            if (string.IsNullOrWhiteSpace(data)) continue; // empty keep-alive
            string? chunkToYield = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                var delta = doc.RootElement.GetProperty("choices")[0].GetProperty("delta");
                if (delta.TryGetProperty("content", out var c))
                {
                    var chunk = c.GetString();
                    if (!string.IsNullOrEmpty(chunk)) chunkToYield = chunk;
                }
            }
            catch { /* ignore malformed */ }

            if (!string.IsNullOrEmpty(chunkToYield)) yield return chunkToYield!;
        }
    }

    private object BuildPayload(IEnumerable<ChatMessage> messages, bool stream)
    {
        return new
        {
            model = _model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            stream
        };
    }

    private HttpRequestMessage CreateRequest(object payload)
    {
        return new HttpRequestMessage(HttpMethod.Post, $"{_apiBase}/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private static bool IsTransient(HttpResponseMessage r)
        => r.StatusCode == System.Net.HttpStatusCode.RequestTimeout
           || (int)r.StatusCode == 429
           || ((int)r.StatusCode >= 500 && (int)r.StatusCode <= 599);

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> requestFactory, HttpCompletionOption completionOption)
    {
        const int maxAttempts = 3;
        Exception? lastEx = null;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var request = requestFactory();
                var resp = await _http.SendAsync(request, completionOption);
                if (!IsTransient(resp) || attempt == maxAttempts - 1)
                {
                    return resp;
                }
                resp.Dispose();
            }
            catch (HttpRequestException ex)
            {
                lastEx = ex;
                if (attempt == maxAttempts - 1) throw;
            }
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)));
        }
        if (lastEx != null) throw lastEx;
        using var finalRequest = requestFactory();
        return await _http.SendAsync(finalRequest, completionOption);
    }
}
