using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Cognition.Clients.Images;

public class OpenAIImageClient : IImageClient
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OpenAIImageClient(HttpClient http, string model = "gpt-image-1")
    {
        _http = http;
        _model = model;
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _http.DefaultRequestHeaders.Remove("Authorization");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
    }

    public async Task<ImageResult> GenerateAsync(string prompt, ImageParameters parameters)
    {
        // Use new OpenAI "images/edits" style or compatible endpoint when available
        var payload = new
        {
            model = _model,
            prompt,
            size = $"{parameters.Width}x{parameters.Height}",
            n = 1,
            response_format = "b64_json"
        };
        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var b64 = doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString() ?? string.Empty;
        var bytes = Convert.FromBase64String(b64);
        return new ImageResult(bytes, "image/png", parameters.Width, parameters.Height, "OpenAI", _model);
    }
}

