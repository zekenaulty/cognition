using System;
using Cognition.Data.Relational.Modules.Personas;

namespace Cognition.Api.Models.Personas;

public sealed record PersonaSummaryResponse(
    Guid Id,
    string Name,
    string? Nickname,
    string? Role,
    bool IsPublic,
    PersonaType Type,
    OwnedBy OwnedBy);

public sealed record PersonaOwnershipResponse(
    Guid Id,
    string Name,
    string? Nickname,
    string? Role,
    bool IsPublic,
    PersonaType Type,
    OwnedBy OwnedBy,
    bool IsOwner);
