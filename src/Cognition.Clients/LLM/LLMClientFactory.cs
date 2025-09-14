using System;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.LLM;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Clients.LLM;

public interface ILLMClientFactory
{
    Task<ILLMClient> CreateAsync(Guid providerId, Guid? modelId = null);
}

public class LLMClientFactory : ILLMClientFactory
{
    private readonly CognitionDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public LLMClientFactory(CognitionDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ILLMClient> CreateAsync(Guid providerId, Guid? modelId = null)
    {
        var provider = await _db.Providers.FirstAsync(p => p.Id == providerId);
        Model? model = null;
        if (modelId.HasValue)
        {
            model = await _db.Models.FirstOrDefaultAsync(m => m.Id == modelId.Value);
        }

        var http = _httpClientFactory.CreateClient();
        switch (provider.Name.ToLowerInvariant())
        {
            case "openai":
                return new OpenAITextClient(http, model?.Name ?? "gpt-4o-mini");
            case "ollama":
                return new OllamaTextClient(http, provider.BaseUrl ?? "http://localhost:11434", model?.Name ?? "llama3.2:3b");
            case "gemini":
                return new GeminiTextClient(http, model?.Name ?? "gemini-2.5-flash");
            default:
                throw new NotSupportedException($"Unknown provider: {provider.Name}");
        }
    }
}

