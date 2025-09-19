using Cognition.Data.Relational;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Clients.LLM;

public interface ILLMProviderResolver
{
    Task<(ILLMClient Client, Guid ProviderId, string ProviderName, Guid? ModelId, string? ModelName)> ResolveAsync(Guid? providerId, Guid? modelId, CancellationToken ct);
}

public sealed class LLMProviderResolver : ILLMProviderResolver
{
    private readonly CognitionDbContext _db;
    private readonly ILLMClientFactory _factory;

    public LLMProviderResolver(CognitionDbContext db, ILLMClientFactory factory)
    { _db = db; _factory = factory; }

    public async Task<(ILLMClient Client, Guid ProviderId, string ProviderName, Guid? ModelId, string? ModelName)> ResolveAsync(Guid? providerId, Guid? modelId, CancellationToken ct)
    {
        Guid resolvedProviderId;
        string providerName;
        string? modelName = null;
        Guid? resolvedModelId = modelId;

        if (providerId.HasValue)
        {
            resolvedProviderId = providerId.Value;
            providerName = await _db.Providers.AsNoTracking().Where(p => p.Id == resolvedProviderId).Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "unknown";
        }
        else
        {
            var providers = await _db.Providers.AsNoTracking().Where(p => p.IsActive).ToListAsync(ct);
            resolvedProviderId = providers.FirstOrDefault(p => p.Name.Equals("ollama", StringComparison.OrdinalIgnoreCase))?.Id
                                 ?? providers.FirstOrDefault(p => p.Name.Equals("openai", StringComparison.OrdinalIgnoreCase))?.Id
                                 ?? providers.FirstOrDefault(p => p.Name.Equals("gemini", StringComparison.OrdinalIgnoreCase))?.Id
                                 ?? throw new InvalidOperationException("No active LLM provider configured");
            providerName = providers.First(p => p.Id == resolvedProviderId).Name;
        }

        if (resolvedModelId.HasValue)
        {
            modelName = await _db.Models.AsNoTracking().Where(m => m.Id == resolvedModelId.Value).Select(m => m.Name).FirstOrDefaultAsync(ct);
        }

        var client = await _factory.CreateAsync(resolvedProviderId, resolvedModelId);
        return (client, resolvedProviderId, providerName, resolvedModelId, modelName);
    }
}

