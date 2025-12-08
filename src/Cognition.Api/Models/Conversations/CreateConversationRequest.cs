using System;
using System.ComponentModel.DataAnnotations;
using Cognition.Api.Infrastructure.Validation;

namespace Cognition.Api.Models.Conversations;

public sealed class CreateConversationRequest
{
    [NotEmptyGuid]
    public Guid AgentId { get; init; }

    [StringLength(256)]
    public string? Title { get; init; }

    [MaxLength(32)]
    public Guid[]? ParticipantIds { get; init; }
}
