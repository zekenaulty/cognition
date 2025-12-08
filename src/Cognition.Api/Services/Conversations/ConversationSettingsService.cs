using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Services.Conversations;
using Cognition.Api.Controllers;
using Cognition.Api.Models.Conversations;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.LLM;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Services;

public sealed class ConversationSettingsService : IConversationSettingsService
{
    private readonly CognitionDbContext _db;
    private readonly ILlmDefaultService _llmDefaultService;

    public ConversationSettingsService(CognitionDbContext db, ILlmDefaultService llmDefaultService)
    {
        _db = db;
        _llmDefaultService = llmDefaultService;
    }

    public async Task<ConversationSettingsResponse> ResolveSettingsAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        Guid? providerId = TryReadMetadataGuid(conversation.Metadata, "providerId");
        Guid? modelId = TryReadMetadataGuid(conversation.Metadata, "modelId");

        if (!providerId.HasValue && conversation.Agent?.ClientProfile is not null)
        {
            providerId = conversation.Agent.ClientProfile.ProviderId;
            modelId = conversation.Agent.ClientProfile.ModelId ?? modelId;
        }

        if (!providerId.HasValue || !modelId.HasValue)
        {
            var defaults = await _llmDefaultService.GetAsync(cancellationToken).ConfigureAwait(false);
            providerId ??= defaults?.Model?.ProviderId;
            modelId ??= defaults?.ModelId;

            if (!providerId.HasValue || !modelId.HasValue)
            {
                // fallback heuristic
                var providers = await _db.Providers.AsNoTracking().OrderBy(p => p.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
                providerId ??= providers.FirstOrDefault(p => p.Name.Contains("Gemini", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Google", StringComparison.OrdinalIgnoreCase))?.Id
                               ?? providers.FirstOrDefault()?.Id;
                if (providerId.HasValue)
                {
                    var models = await _db.Models.AsNoTracking().Where(m => m.ProviderId == providerId.Value).ToListAsync(cancellationToken).ConfigureAwait(false);
                    modelId ??= models.FirstOrDefault(m =>
                        m.Name.Contains("flash", StringComparison.OrdinalIgnoreCase) ||
                        m.Name.Contains("2.5", StringComparison.OrdinalIgnoreCase) ||
                        m.Name.Contains("2.0", StringComparison.OrdinalIgnoreCase))?.Id
                        ?? models.FirstOrDefault()?.Id;
                }
            }
        }

        return new ConversationSettingsResponse(providerId, modelId);
    }

    public async Task<bool> ValidateProviderModelAsync(Guid? providerId, Guid? modelId, CancellationToken cancellationToken)
    {
        Guid? providerToCheck = providerId;
        if (!providerToCheck.HasValue && modelId.HasValue)
        {
            var model = await _db.Set<Model>().AsNoTracking().FirstOrDefaultAsync(m => m.Id == modelId.Value, cancellationToken);
            if (model is null || model.IsDeprecated) return false;
            providerToCheck = model.ProviderId;
        }
        if (providerToCheck.HasValue)
        {
            var provider = await _db.Set<Provider>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == providerToCheck.Value && p.IsActive, cancellationToken);
            if (provider is null) return false;
        }
        if (modelId.HasValue)
        {
            var model = await _db.Set<Model>().AsNoTracking().FirstOrDefaultAsync(m => m.Id == modelId.Value, cancellationToken);
            if (model is null || model.IsDeprecated || (providerToCheck.HasValue && model.ProviderId != providerToCheck.Value))
            {
                return false;
            }
        }
        return true;
    }

    public Guid? TryReadMetadataGuid(System.Collections.Generic.Dictionary<string, object?>? metadata, string key)
    {
        if (metadata is null) return null;
        if (metadata.TryGetValue(key, out var value) && value is not null)
        {
            var str = value.ToString();
            if (Guid.TryParse(str, out var guid))
            {
                return guid;
            }
        }
        return null;
    }
}
