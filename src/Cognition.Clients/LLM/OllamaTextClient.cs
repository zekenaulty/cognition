using System.Globalization;
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
    private readonly OllamaOptions _options;

    public OllamaTextClient(HttpClient http, string baseUrl, string model, OllamaOptions? options = null)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
        _http.Timeout = TimeSpan.FromSeconds(120);
        _options = options ?? OllamaOptions.FromEnvironment();
    }

    private record OllamaChatMessage([property: JsonPropertyName("role")] string Role,
                                     [property: JsonPropertyName("content")] string Content);
    private record OllamaChatRequest([property: JsonPropertyName("model")] string Model,
                                     [property: JsonPropertyName("messages")] List<OllamaChatMessage> Messages,
                                     [property: JsonPropertyName("stream")] bool Stream = false,
                                     [property: JsonPropertyName("options")] OllamaRequestOptions? Options = null);
    internal record OllamaRequestOptions(
        [property: JsonPropertyName("temperature"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Temperature = null,
        [property: JsonPropertyName("top_p"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? TopP = null,
        [property: JsonPropertyName("presence_penalty"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? PresencePenalty = null,
       [property: JsonPropertyName("frequency_penalty"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? FrequencyPenalty = null,
        [property: JsonPropertyName("num_predict"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? MaxTokens = null,
        [property: JsonPropertyName("num_ctx"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ContextWindow = null);
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
        var payload = new OllamaChatRequest(_model,
            messages.Select(m => new OllamaChatMessage(m.Role, m.Content)).ToList(),
            Stream: false,
            Options: _options.ToRequestOptions());
        var resp = await SendWithRetryAsync(() => CreateChatRequest(payload, stream: false), HttpCompletionOption.ResponseContentRead);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<OllamaChatChunk>();
        var content = NormalizeResponse(body?.Message?.Content, body?.Response);
        return content;
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(IEnumerable<ChatMessage> messages, bool track = false)
    {
        var payload = new OllamaChatRequest(_model,
            messages.Select(m => new OllamaChatMessage(m.Role, m.Content)).ToList(),
            Stream: true,
            Options: _options.ToRequestOptions());
        using var resp = await SendWithRetryAsync(() => CreateChatRequest(payload, stream: true), HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        string? line;
        bool emittedAny = false;
        string? finalResponse = null;
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
                    toYield = chunk.Message?.Content;
                    finalResponse = chunk.Response ?? finalResponse;
                }
            }
            catch { /* ignore malformed */ }

            if (!string.IsNullOrEmpty(toYield))
            {
                emittedAny = true;
                yield return StripThink(toYield!);
            }

            if (done)
            {
                if (!emittedAny && !string.IsNullOrEmpty(finalResponse))
                {
                    emittedAny = true;
                    yield return StripThink(finalResponse!);
                }
                yield break;
            }
        }
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

    private HttpRequestMessage CreateChatRequest(OllamaChatRequest request, bool stream)
    {
        var payload = request with { Stream = stream };
        return new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
        {
            Content = JsonContent.Create(payload)
        };
    }

    private static string NormalizeResponse(string? primary, string? fallback)
    {
        var text = !string.IsNullOrWhiteSpace(primary) ? primary : fallback ?? string.Empty;
        return StripThink(text);
    }

    private static string StripThink(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var start = value.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
        var end = value.IndexOf("</think>", StringComparison.OrdinalIgnoreCase);
        if (start >= 0 && end > start)
        {
            value = value.Substring(end + "</think>".Length);
        }
        value = value.Replace("</think>", string.Empty, StringComparison.OrdinalIgnoreCase);
        return value.Trim();
    }

    public sealed record OllamaOptions(
        double? Temperature = null,
        double? TopP = null,
        double? PresencePenalty = null,
        double? FrequencyPenalty = null,
        int? MaxTokens = null,
        int? ContextWindow = null)
    {
        public static OllamaOptions FromEnvironment()
        {
            return new OllamaOptions(
                ParseDouble(Environment.GetEnvironmentVariable("OLLAMA_TEMPERATURE")),
                ParseDouble(Environment.GetEnvironmentVariable("OLLAMA_TOP_P")),
                ParseDouble(Environment.GetEnvironmentVariable("OLLAMA_PRESENCE_PENALTY")),
                ParseDouble(Environment.GetEnvironmentVariable("OLLAMA_FREQUENCY_PENALTY")),
                ParseInt(Environment.GetEnvironmentVariable("OLLAMA_MAX_TOKENS")),
                ParseInt(Environment.GetEnvironmentVariable("OLLAMA_CONTEXT_WINDOW")));
        }

        internal OllamaRequestOptions? ToRequestOptions()
        {
            if (Temperature is null && TopP is null && PresencePenalty is null && FrequencyPenalty is null && MaxTokens is null && ContextWindow is null)
            {
                return null;
            }

            return new OllamaRequestOptions(Temperature, TopP, PresencePenalty, FrequencyPenalty, MaxTokens, ContextWindow);
        }

        private static double? ParseDouble(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        }

        private static int? ParseInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        }
    }
}
