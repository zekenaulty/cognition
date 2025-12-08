using System;

namespace Cognition.Api.Models.Conversations;

public sealed class ConversationSettingsRequest
{
    public Guid? ProviderId { get; init; }
    public Guid? ModelId { get; init; }
}
