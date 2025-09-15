using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/system/env")] 
public class EnvStatusController : ControllerBase
{
    private static string Mask(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var last = s.Length >= 4 ? s[^4..] : s;
        return new string('*', Math.Max(0, s.Length - last.Length)) + last;
    }

    [HttpGet("status")]
    [Authorize]
    public IActionResult GetStatus()
    {
        string? openaiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? Environment.GetEnvironmentVariable("OPENAI_KEY");
        string? openaiBase = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
        string? geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        string? geminiBase = Environment.GetEnvironmentVariable("GEMINI_BASE_URL");
        string? ollamaBase = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL");
        string? githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        return Ok(new
        {
            openai = new { hasKey = !string.IsNullOrWhiteSpace(openaiKey), keySample = Mask(openaiKey), baseUrl = openaiBase },
            gemini = new { hasKey = !string.IsNullOrWhiteSpace(geminiKey), keySample = Mask(geminiKey), baseUrl = geminiBase },
            ollama = new { baseUrl = ollamaBase },
            github = new { hasToken = !string.IsNullOrWhiteSpace(githubToken), tokenSample = Mask(githubToken) }
        });
    }
}

