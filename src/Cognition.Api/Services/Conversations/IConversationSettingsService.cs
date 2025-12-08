using System;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Api.Services.Conversations;
using Cognition.Api.Models.Conversations;

namespace Cognition.Api.Services;

public interface IConversationSettingsService
{
    Task<ConversationSettingsResponse> ResolveSettingsAsync(Conversation conversation, CancellationToken cancellationToken);
    Task<bool> ValidateProviderModelAsync(Guid? providerId, Guid? modelId, CancellationToken cancellationToken);
    Guid? TryReadMetadataGuid(System.Collections.Generic.Dictionary<string, object?>? metadata, string key);
}
