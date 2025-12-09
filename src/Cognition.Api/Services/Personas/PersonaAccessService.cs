using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Models.Personas;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Services.Personas;

public sealed class PersonaAccessService : IPersonaAccessService
{
    private readonly CognitionDbContext _db;
    public PersonaAccessService(CognitionDbContext db) => _db = db;

    public async Task<IReadOnlyList<PersonaOwnershipResponse>> ListWithOwnershipAsync(Guid caller, bool publicOnly, CancellationToken cancellationToken)
    {
        var query = _db.Personas.AsNoTracking().AsQueryable();
        if (publicOnly) query = query.Where(p => p.IsPublic);

        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                Persona = p,
                IsOwner = _db.UserPersonas.Any(up => up.UserId == caller && up.PersonaId == p.Id && up.IsOwner)
            })
            .ToListAsync(cancellationToken);

        return items.Select(x => new PersonaOwnershipResponse(
            x.Persona.Id,
            x.Persona.Name,
            x.Persona.Nickname,
            x.Persona.Role,
            x.Persona.IsPublic,
            x.Persona.Type,
            x.Persona.OwnedBy,
            x.IsOwner)).ToList();
    }

    public async Task<IReadOnlyList<PersonaSummaryResponse>> ListSystemAsync(bool publicOnly, CancellationToken cancellationToken)
    {
        var query = _db.Personas.AsNoTracking().Where(p => p.OwnedBy == OwnedBy.System);
        if (publicOnly) query = query.Where(p => p.IsPublic);

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new PersonaSummaryResponse(p.Id, p.Name, p.Nickname, p.Role, p.IsPublic, p.Type, p.OwnedBy))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PersonaSummaryResponse>> ListForUserAsync(Guid caller, bool isAdmin, bool publicOnly, CancellationToken cancellationToken)
    {
        var linkedIds = await _db.UserPersonas.AsNoTracking()
            .Where(up => up.UserId == caller)
            .Select(up => up.PersonaId)
            .ToListAsync(cancellationToken);

        var primaryId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == caller)
            .Select(u => u.PrimaryPersonaId)
            .FirstOrDefaultAsync(cancellationToken);

        var ids = new HashSet<Guid>(linkedIds);
        if (primaryId.HasValue) ids.Add(primaryId.Value);

        var query = _db.Personas.AsNoTracking()
            .Where(p => ids.Contains(p.Id) || (isAdmin && p.OwnedBy == OwnedBy.System));

        if (publicOnly) query = query.Where(p => p.IsPublic);

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new PersonaSummaryResponse(p.Id, p.Name, p.Nickname, p.Role, p.IsPublic, p.Type, p.OwnedBy))
            .ToListAsync(cancellationToken);
    }

    public Task<Persona?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Personas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> CanEditAsync(Persona persona, Guid caller, bool isAdmin, CancellationToken cancellationToken)
    {
        if (isAdmin) return true;
        if (persona.OwnedBy != OwnedBy.User) return false;
        return await _db.UserPersonas.AsNoTracking()
            .AnyAsync(up => up.UserId == caller && up.PersonaId == persona.Id && up.IsOwner, cancellationToken);
    }

    public async Task SetVisibilityAsync(Guid personaId, bool isPublic, Guid caller, bool isAdmin, CancellationToken cancellationToken)
    {
        var persona = await _db.Personas.FirstOrDefaultAsync(x => x.Id == personaId, cancellationToken);
        if (persona == null) throw new KeyNotFoundException("persona_not_found");

        var canEdit = await CanEditAsync(persona, caller, isAdmin, cancellationToken);
        if (!canEdit) throw new UnauthorizedAccessException("forbidden");

        persona.IsPublic = isPublic;
        persona.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> GrantAccessAsync(Guid personaId, Guid userId, bool isDefault, string? label, Guid caller, bool isAdmin, CancellationToken cancellationToken)
    {
        if (!await _db.Personas.AnyAsync(p => p.Id == personaId, cancellationToken))
            throw new KeyNotFoundException("persona_not_found");

        if (!await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken))
            throw new KeyNotFoundException("user_not_found");

        var persona = await _db.Personas.AsNoTracking().FirstAsync(p => p.Id == personaId, cancellationToken);
        var canManage = await CanEditAsync(persona, caller, isAdmin, cancellationToken);
        if (!canManage) throw new UnauthorizedAccessException("forbidden");

        var link = await _db.UserPersonas.FirstOrDefaultAsync(x => x.UserId == userId && x.PersonaId == personaId, cancellationToken);
        if (link == null)
        {
            link = new UserPersonas
            {
                UserId = userId,
                PersonaId = personaId,
                IsDefault = isDefault,
                Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim()
            };
            _db.UserPersonas.Add(link);
        }
        else
        {
            link.IsDefault = isDefault;
            link.Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
            link.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return link.Id;
    }

    public async Task RevokeAccessAsync(Guid personaId, Guid userId, Guid caller, bool isAdmin, CancellationToken cancellationToken)
    {
        var link = await _db.UserPersonas.FirstOrDefaultAsync(x => x.UserId == userId && x.PersonaId == personaId, cancellationToken);
        if (link == null) throw new KeyNotFoundException("persona_access_link_not_found");

        var persona = await _db.Personas.AsNoTracking().FirstAsync(p => p.Id == personaId, cancellationToken);
        var canManage = await CanEditAsync(persona, caller, isAdmin, cancellationToken);
        if (!canManage) throw new UnauthorizedAccessException("forbidden");

        _db.UserPersonas.Remove(link);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
