using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Cognition.Clients.Images;

public class OpenAIImageClient : IImageClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _apiBase;

    public OpenAIImageClient(HttpClient http, string model = "dall-e-3")
    {
        _http = http;
        _model = model;
        _apiBase = Environment.GetEnvironmentVariable("OPENAI_BASE_URL")?.TrimEnd('/') ?? "https://api.openai.com";
        // Ensure generous timeout for long-running generations
        if (_http.Timeout < TimeSpan.FromMinutes(10))
            _http.Timeout = TimeSpan.FromMinutes(10);
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? Environment.GetEnvironmentVariable("OPENAI_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _http.DefaultRequestHeaders.Remove("Authorization");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
    }

    public async Task<ImageResult> GenerateAsync(string prompt, ImageParameters parameters)
    {
        // Choose model per-call if provided (allows DALLE-3 vs gpt-image-1)
        var initialModel = string.IsNullOrWhiteSpace(parameters.Model) ? _model : parameters.Model!;

        async Task<HttpResponseMessage> SendAsync(string modelToUse)
        {
            var payload = new
            {
                model = modelToUse,
                prompt,
                size = $"{parameters.Width}x{parameters.Height}",
                n = 1,
                response_format = "b64_json"
            };
            var req = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase}/v1/images/generations")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            return await SendWithRetryAsync(() => _http.SendAsync(req));
        }

        var resp = await SendAsync(initialModel);
        if (!resp.IsSuccessStatusCode)
        {
            var errBody = await resp.Content.ReadAsStringAsync();
            // Graceful fallback: if using gpt-image-1 and org is not verified, retry with DALLE-3
            if (string.Equals(initialModel, "gpt-image-1", StringComparison.OrdinalIgnoreCase)
                && (int)resp.StatusCode == 400
                && errBody.IndexOf("must be verified", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var fallbackModel = "dall-e-3";
                var resp2 = await SendAsync(fallbackModel);
                if (!resp2.IsSuccessStatusCode)
                {
                    var err2 = await resp2.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"OpenAI image error {(int)resp2.StatusCode} {resp2.StatusCode}: {err2}");
                }
                using var doc2 = JsonDocument.Parse(await resp2.Content.ReadAsStringAsync());
                var data0b = doc2.RootElement.GetProperty("data")[0];
                byte[] bytes2;
                if (data0b.TryGetProperty("b64_json", out var b64El2))
                {
                    var b64 = b64El2.GetString() ?? string.Empty;
                    bytes2 = Convert.FromBase64String(b64);
                }
                else if (data0b.TryGetProperty("url", out var urlEl2))
                {
                    var url = urlEl2.GetString() ?? string.Empty;
                    using var imgResp2 = await _http.GetAsync(url);
                    imgResp2.EnsureSuccessStatusCode();
                    bytes2 = await imgResp2.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    throw new HttpRequestException("OpenAI image response missing 'b64_json' and 'url'.");
                }
                return new ImageResult(bytes2, "image/png", parameters.Width, parameters.Height, "OpenAI", fallbackModel);
            }

            throw new HttpRequestException($"OpenAI image error {(int)resp.StatusCode} {resp.StatusCode}: {errBody}");
        }
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var data0 = doc.RootElement.GetProperty("data")[0];
        byte[] bytes;
        if (data0.TryGetProperty("b64_json", out var b64El))
        {
            var b64 = b64El.GetString() ?? string.Empty;
            bytes = Convert.FromBase64String(b64);
        }
        else if (data0.TryGetProperty("url", out var urlEl))
        {
            var url = urlEl.GetString() ?? string.Empty;
            using var imgResp = await _http.GetAsync(url);
            imgResp.EnsureSuccessStatusCode();
            bytes = await imgResp.Content.ReadAsByteArrayAsync();
        }
        else
        {
            throw new HttpRequestException("OpenAI image response missing 'b64_json' and 'url'.");
        }
        return new ImageResult(bytes, "image/png", parameters.Width, parameters.Height, "OpenAI", initialModel);
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
