using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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

    private record OllamaChatMessage([property: JsonPropertyName("role")] string Role,
                                     [property: JsonPropertyName("content")] string Content);
    private record OllamaChatRequest([property: JsonPropertyName("model")] string Model,
                                     [property: JsonPropertyName("messages")] List<OllamaChatMessage> Messages,
                                     [property: JsonPropertyName("stream")] bool Stream = false);
    private record OllamaChatChunk([property: JsonPropertyName("message")] OllamaChatMessage Message,
                                   [property: JsonPropertyName("done")] bool Done,
                                   [property: JsonPropertyName("response")] string? Response);

    public async Task<string> GenerateAsync(string prompt, bool track = false)
        => await ChatAsync(new[] { new ChatMessage("user", prompt) }, track);

    public async IAsyncEnumerable<string> GenerateStreamAsync(string prompt, bool track = false)
    {
        await foreach (var chunk in ChatStreamAsync(new[] { new ChatMessage("user", prompt) }, track))
        {
            yield return chunk;
        }
    }

    public async Task<string> ChatAsync(IEnumerable<ChatMessage> messages, bool track = false)
    {
        var req = new OllamaChatRequest(_model,
            messages.Select(m => new OllamaChatMessage(m.Role, m.Content)).ToList(),
            Stream: false);
        var httpReq = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
        {
            Content = JsonContent.Create(req)
        };
        var resp = await SendWithRetryAsync(() => _http.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead));
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<OllamaChatChunk>();
        if (body?.Message?.Content != null) return body.Message.Content;
        if (!string.IsNullOrEmpty(body?.Response)) return body!.Response!;
        return string.Empty;
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(IEnumerable<ChatMessage> messages, bool track = false)
    {
        var reqObj = new OllamaChatRequest(_model,
            messages.Select(m => new OllamaChatMessage(m.Role, m.Content)).ToList(),
            Stream: true);
        var httpReq = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
        {
            Content = JsonContent.Create(reqObj)
        };
        using var resp = await SendWithRetryAsync(() => _http.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead));
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(":")) continue; // keep-alives/comments
            string? toYield = null;
            bool done = false;
            try
            {
                var chunk = JsonSerializer.Deserialize<OllamaChatChunk>(line);
                if (chunk != null)
                {
                    done = chunk.Done;
                    toYield = chunk.Message?.Content ?? chunk.Response;
                }
            }
            catch { /* ignore malformed */ }

            if (done) yield break;
            if (!string.IsNullOrEmpty(toYield)) yield return toYield!;
        }
    }

    private static bool IsTransient(HttpResponseMessage r)
        => r.StatusCode == System.Net.HttpStatusCode.RequestTimeout
           || (int)r.StatusCode == 429
           || ((int)r.StatusCode >= 500 && (int)r.StatusCode <= 599);

    private static async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> send)
    {
        int attempt = 0;
        Exception? lastEx = null;
        while (attempt < 3)
        {
            try
            {
                var resp = await send();
                if (IsTransient(resp))
                {
                    attempt++;
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                    continue;
                }
                return resp;
            }
            catch (HttpRequestException ex)
            {
                lastEx = ex;
                attempt++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }
        if (lastEx != null) throw lastEx;
        return await send();
    }
}
