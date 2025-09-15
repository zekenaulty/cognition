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
        var provider = await _db.Providers.AsNoTracking().FirstAsync(p => p.Id == providerId);
        Model? model = null;
        if (modelId.HasValue)
        {
            model = await _db.Models.AsNoTracking().FirstOrDefaultAsync(m => m.Id == modelId.Value);
        }

        // Prefer a matching client profile (by provider and model) if present
        var profile = await _db.ClientProfiles.AsNoTracking()
            .Include(p => p.ApiCredential)
            .Where(p => p.ProviderId == providerId && (!modelId.HasValue || p.ModelId == modelId))
            .OrderByDescending(p => p.ModelId != null) // prefer model-specific profile
            .FirstOrDefaultAsync();

        // Resolve credential value from profile.ApiCredential.KeyRef; fallback to provider-level credential
        string? apiKey = null;
        if (profile?.ApiCredential != null && profile.ApiCredential.IsValid)
        {
            apiKey = ResolveSecret(profile.ApiCredential.KeyRef);
        }
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var providerCred = await _db.ApiCredentials.AsNoTracking()
                .Where(c => c.ProviderId == providerId && c.IsValid)
                .OrderByDescending(c => c.UpdatedAtUtc)
                .FirstOrDefaultAsync();
            if (providerCred != null) apiKey = ResolveSecret(providerCred.KeyRef);
        }

        // Base URL selection: profile override -> provider -> env -> default
        string? baseUrlOverride = profile?.BaseUrlOverride ?? provider.BaseUrl;

        var http = _httpClientFactory.CreateClient("llm");
        switch (provider.Name.ToLowerInvariant())
        {
            case "openai":
                return new OpenAITextClient(
                    http,
                    model?.Name ?? "gpt-4o-mini",
                    apiKey,
                    baseUrlOverride);
            case "ollama":
                return new OllamaTextClient(
                    http,
                    baseUrlOverride ?? Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434",
                    model?.Name ?? "llama3.2:3b");
            case "gemini":
                return new GeminiTextClient(
                    http,
                    model?.Name ?? "gemini-2.5-flash",
                    apiKey,
                    baseUrlOverride);
            default:
                throw new NotSupportedException($"Unknown provider: {provider.Name}");
        }
    }

    private static string? ResolveSecret(string keyRef)
    {
        if (string.IsNullOrWhiteSpace(keyRef)) return null;
        // For now, treat KeyRef as environment variable name.
        // Future: support "env:NAME", secret manager, etc.
        var envName = keyRef.StartsWith("env:", StringComparison.OrdinalIgnoreCase)
            ? keyRef.Substring(4)
            : keyRef;
        return Environment.GetEnvironmentVariable(envName);
    }
}
