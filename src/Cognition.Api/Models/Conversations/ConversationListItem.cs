using System;

namespace Cognition.Api.Models.Conversations;

public sealed record ConversationListItem(Guid Id, string? Title, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, Guid? ProviderId, Guid? ModelId);
