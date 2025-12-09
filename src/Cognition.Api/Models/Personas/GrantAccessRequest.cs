using System;
using System.ComponentModel.DataAnnotations;
using Cognition.Api.Infrastructure.Validation;

namespace Cognition.Api.Models.Personas;

public sealed class GrantAccessRequest
{
    [NotEmptyGuid]
    public Guid UserId { get; init; }
    public bool IsDefault { get; init; }
    [StringLength(128)]
    public string? Label { get; init; }

    public GrantAccessRequest() { }

    public GrantAccessRequest(Guid userId, bool isDefault = false, string? label = null)
    {
        UserId = userId;
        IsDefault = isDefault;
        Label = label;
    }
}
