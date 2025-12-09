using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Models.Personas;
using Cognition.Data.Relational.Modules.Personas;

namespace Cognition.Api.Services.Personas;

public interface IPersonaAccessService
{
    Task<IReadOnlyList<PersonaOwnershipResponse>> ListWithOwnershipAsync(Guid caller, bool publicOnly, CancellationToken cancellationToken);
    Task<IReadOnlyList<PersonaSummaryResponse>> ListSystemAsync(bool publicOnly, CancellationToken cancellationToken);
    Task<IReadOnlyList<PersonaSummaryResponse>> ListForUserAsync(Guid caller, bool isAdmin, bool publicOnly, CancellationToken cancellationToken);
    Task<Persona?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CanEditAsync(Persona persona, Guid caller, bool isAdmin, CancellationToken cancellationToken);
    Task SetVisibilityAsync(Guid personaId, bool isPublic, Guid caller, bool isAdmin, CancellationToken cancellationToken);
    Task<Guid> GrantAccessAsync(Guid personaId, Guid userId, bool isDefault, string? label, Guid caller, bool isAdmin, CancellationToken cancellationToken);
    Task RevokeAccessAsync(Guid personaId, Guid userId, Guid caller, bool isAdmin, CancellationToken cancellationToken);
}
