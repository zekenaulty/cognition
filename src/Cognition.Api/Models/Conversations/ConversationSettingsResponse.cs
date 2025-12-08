using System;

namespace Cognition.Api.Models.Conversations;

public sealed record ConversationSettingsResponse(Guid? ProviderId, Guid? ModelId);
