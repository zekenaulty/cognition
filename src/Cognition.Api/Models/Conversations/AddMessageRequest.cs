using System;
using System.ComponentModel.DataAnnotations;
using Cognition.Api.Infrastructure.Validation;
using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Api.Models.Conversations;

public sealed class AddMessageRequest
{
    [NotEmptyGuid]
    public Guid FromPersonaId { get; init; }

    [NotEmptyGuid]
    public Guid? ToPersonaId { get; init; }

    public ChatRole Role { get; init; }

    [Required, StringLength(4000, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Content must contain non-whitespace characters.")]
    public string Content { get; init; } = string.Empty;

    [StringLength(128)]
    public string? Metatype { get; init; }

    public AddMessageRequest() { }

    public AddMessageRequest(Guid fromPersonaId, Guid? toPersonaId, ChatRole role, string content, string? metatype = null)
    {
        FromPersonaId = fromPersonaId;
        ToPersonaId = toPersonaId;
        Role = role;
        Content = content;
        Metatype = metatype;
    }
}
