using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Security;
using Cognition.Api.Infrastructure.Validation;
using Cognition.Api.Infrastructure.ErrorHandling;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOrHigher)]
[ApiController]
[Route("api/personas")]
public class PersonasController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public PersonasController(CognitionDbContext db) => _db = db;

    public record PersonaCreateRequest(
        [property: Required, StringLength(128, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Name must contain non-whitespace characters.")]
        string Name,
        [property: StringLength(128)]
        string? Nickname,
        [property: StringLength(256)]
        string? Role,
        [property: StringLength(64)]
        string? Gender,
        [property: StringLength(4000)]
        string? Essence,
        [property: StringLength(4000)]
        string? Beliefs,
        [property: StringLength(4000)]
        string? Background,
        [property: StringLength(4000)]
        string? CommunicationStyle,
        [property: StringLength(4000)]
        string? EmotionalDrivers,
        string[]? SignatureTraits,
        string[]? NarrativeThemes,
        string[]? DomainExpertise,
        bool? IsPublic,
        [property: StringLength(256)]
        string? Voice);

    public record VisibilityRequest(bool IsPublic);
    public record GrantAccessRequest(
        [property: NotEmptyGuid] Guid UserId,
        bool IsDefault = false,
        [property: StringLength(128)] string? Label = null);
    public record PersonaUpdateRequest(
        [property: StringLength(128, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Name must contain non-whitespace characters when provided.")]
        string? Name,
        [property: StringLength(128)]
        string? Nickname,
        [property: StringLength(256)]
        string? Role,
        [property: StringLength(64)]
        string? Gender,
        [property: StringLength(4000)]
        string? Essence,
        [property: StringLength(4000)]
        string? Beliefs,
        [property: StringLength(4000)]
        string? Background,
        [property: StringLength(4000)]
        string? CommunicationStyle,
        [property: StringLength(4000)]
        string? EmotionalDrivers,
        string[]? SignatureTraits,
        string[]? NarrativeThemes,
        string[]? DomainExpertise,
        bool? IsPublic,
        [property: StringLength(256)]
        string? Voice,
        Cognition.Data.Relational.Modules.Personas.PersonaType? Type
    );

    
    // Returns all personas with an IsOwner flag for the current user
    [HttpGet("with-ownership")]
    public async Task<IActionResult> ListWithOwnership([FromQuery] bool? publicOnly, CancellationToken cancellationToken = default)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var caller)) return Forbid();
        var q = _db.Personas.AsNoTracking().AsQueryable();
        if (publicOnly == true) q = q.Where(p => p.IsPublic);
        var items = await q
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Nickname,
                p.Role,
                p.IsPublic,
                p.Type,
                p.OwnedBy,
                IsOwner = _db.UserPersonas.Any(up => up.UserId == caller && up.PersonaId == p.Id && up.IsOwner)
            })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    // Returns personas owned by the system
    [HttpGet("system")]
    [Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
    public async Task<IActionResult> ListSystem([FromQuery] bool? publicOnly, CancellationToken cancellationToken = default)
    {
        var q = _db.Personas.AsNoTracking().Where(p => p.OwnedBy == OwnedBy.System);
        if (publicOnly == true) q = q.Where(p => p.IsPublic);
        var items = await q
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Nickname, p.Role, p.IsPublic, p.Type, p.OwnedBy })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    // Returns the default system assistant persona (minimal), available to all authenticated users
    [HttpGet("default-assistant")]
    public async Task<IActionResult> GetDefaultAssistant(CancellationToken cancellationToken = default)
    {
        var p = await _db.Personas.AsNoTracking()
            .Where(x => x.OwnedBy == OwnedBy.System && x.Type == PersonaType.Assistant)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (p == null) return NotFound(ApiErrorResponse.Create("default_assistant_not_found", "Default assistant persona not found."));
        return Ok(p);
    }
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? publicOnly, CancellationToken cancellationToken = default)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(sub, out var caller)) return Forbid();
        var isAdmin = role == nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator);

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

        var q = _db.Personas.AsNoTracking()
            .Where(p => ids.Contains(p.Id) || (isAdmin && p.OwnedBy == OwnedBy.System));
        if (publicOnly == true) q = q.Where(p => p.IsPublic);
        var items = await q
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Nickname, p.Role, p.IsPublic, p.Type })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

[HttpPatch("{id:guid}")]
public async Task<IActionResult> Update(Guid id, [FromBody] PersonaUpdateRequest req, CancellationToken cancellationToken = default)
{
    var p = await _db.Personas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (p == null) return NotFound(ApiErrorResponse.Create("persona_not_found", "Persona not found."));
    // Only owner or admin may edit
    var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
    if (!Guid.TryParse(sub, out var caller)) return Forbid();

    var canEdit = role == nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator)
        || (p.OwnedBy == OwnedBy.User && await _db.UserPersonas.AsNoTracking()
            .AnyAsync(up => up.UserId == caller && up.PersonaId == id && up.IsOwner, cancellationToken));
    if (!canEdit) return Forbid();

    var isAdmin = role == nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator);
    var isOwner = p.OwnedBy == OwnedBy.User && await _db.UserPersonas.AsNoTracking()
        .AnyAsync(up => up.PersonaId == id && up.UserId == caller && up.IsOwner, cancellationToken);

    // Allow non-owners to update Voice only if they have a user-persona link
    var voiceOnly =
        req.Name == null &&
        req.Nickname == null &&
        req.Role == null &&
        req.Gender == null &&
        req.Essence == null &&
        req.Beliefs == null &&
        req.Background == null &&
        req.CommunicationStyle == null &&
        req.EmotionalDrivers == null &&
        req.SignatureTraits == null &&
        req.NarrativeThemes == null &&
        req.DomainExpertise == null &&
        !req.IsPublic.HasValue &&
        req.Voice != null;

    if (!isOwner && !isAdmin)
    {
        if (voiceOnly)
        {
            var allow = p.IsPublic || await _db.UserPersonas.AsNoTracking()
                .AnyAsync(up => up.UserId == caller && up.PersonaId == id, cancellationToken);
            if (!allow) return Forbid();
            p.Voice = req.Voice ?? p.Voice;
            p.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        return Forbid();
    }
    if (req.Name != null) p.Name = req.Name.Trim();
    if (req.Nickname != null) p.Nickname = string.IsNullOrWhiteSpace(req.Nickname) ? string.Empty : req.Nickname.Trim();
    if (req.Role != null) p.Role = string.IsNullOrWhiteSpace(req.Role) ? string.Empty : req.Role.Trim();
    if (req.Gender != null) p.Gender = string.IsNullOrWhiteSpace(req.Gender) ? string.Empty : req.Gender.Trim();
    if (req.Essence != null) p.Essence = string.IsNullOrWhiteSpace(req.Essence) ? string.Empty : req.Essence.Trim();
    if (req.Beliefs != null) p.Beliefs = string.IsNullOrWhiteSpace(req.Beliefs) ? string.Empty : req.Beliefs.Trim();
    if (req.Background != null) p.Background = string.IsNullOrWhiteSpace(req.Background) ? string.Empty : req.Background.Trim();
    if (req.CommunicationStyle != null) p.CommunicationStyle = string.IsNullOrWhiteSpace(req.CommunicationStyle) ? string.Empty : req.CommunicationStyle.Trim();
    if (req.EmotionalDrivers != null) p.EmotionalDrivers = string.IsNullOrWhiteSpace(req.EmotionalDrivers) ? string.Empty : req.EmotionalDrivers.Trim();
    if (req.SignatureTraits != null)
    {
        p.SignatureTraits = req.SignatureTraits
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
    if (req.NarrativeThemes != null)
    {
        p.NarrativeThemes = req.NarrativeThemes
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
    if (req.DomainExpertise != null)
    {
        p.DomainExpertise = req.DomainExpertise
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
    if (req.IsPublic.HasValue) p.IsPublic = req.IsPublic.Value;
    if (req.Voice != null) p.Voice = string.IsNullOrWhiteSpace(req.Voice) ? string.Empty : req.Voice.Trim();
    // Persona type guards:
    // - Never allow setting type to User via update
    // - If persona is already User, its type is locked and cannot change
    if (req.Type.HasValue)
    {
        if (req.Type.Value == Cognition.Data.Relational.Modules.Personas.PersonaType.User)
            return BadRequest(ApiErrorResponse.Create("persona_type_invalid", "Cannot change persona type to User."));
        if (p.Type == Cognition.Data.Relational.Modules.Personas.PersonaType.User && req.Type.Value != Cognition.Data.Relational.Modules.Personas.PersonaType.User)
            return BadRequest(ApiErrorResponse.Create("persona_type_locked", "User persona type is locked and cannot be changed."));
        p.Type = req.Type.Value;
    }
    p.UpdatedAtUtc = DateTime.UtcNow;
    await _db.SaveChangesAsync(cancellationToken);
    return NoContent();
}

[HttpDelete("{id:guid}")]
[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
{
    var p = await _db.Personas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (p == null) return NotFound(ApiErrorResponse.Create("persona_not_found", "Persona not found."));
    _db.Personas.Remove(p);
    await _db.SaveChangesAsync(cancellationToken);
    return NoContent();
}

[HttpGet("{id:guid}")]
public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
{
    var p = await _db.Personas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (p == null) return NotFound(ApiErrorResponse.Create("persona_not_found", "Persona not found."));
    var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    var isOwner = false;
    if (Guid.TryParse(sub, out var caller) && p.OwnedBy == OwnedBy.User)
    {
        isOwner = await _db.UserPersonas.AsNoTracking()
            .AnyAsync(up => up.UserId == caller && up.PersonaId == id && up.IsOwner, cancellationToken);
    }
    return Ok(new
    {
        p.Id,
        p.Name,
        p.Nickname,
        p.Role,
        p.Gender,
        p.Essence,
        p.Beliefs,
        p.Background,
        p.CommunicationStyle,
        p.EmotionalDrivers,
        p.SignatureTraits,
        p.NarrativeThemes,
        p.DomainExpertise,
        p.IsPublic,
        p.Voice,
        p.Type,
        p.OwnedBy,
        IsOwner = isOwner
    });
}

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PersonaCreateRequest req, CancellationToken cancellationToken = default)
    {
        var signatureTraits = req.SignatureTraits?
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var narrativeThemes = req.NarrativeThemes?
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var domainExpertise = req.DomainExpertise?
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var p = new Persona
        {
            Name = req.Name.Trim(),
            Nickname = string.IsNullOrWhiteSpace(req.Nickname) ? string.Empty : req.Nickname.Trim(),
            Role = string.IsNullOrWhiteSpace(req.Role) ? string.Empty : req.Role.Trim(),
            Gender = string.IsNullOrWhiteSpace(req.Gender) ? string.Empty : req.Gender.Trim(),
            Essence = string.IsNullOrWhiteSpace(req.Essence) ? string.Empty : req.Essence.Trim(),
            Beliefs = string.IsNullOrWhiteSpace(req.Beliefs) ? string.Empty : req.Beliefs.Trim(),
            Background = string.IsNullOrWhiteSpace(req.Background) ? string.Empty : req.Background.Trim(),
            CommunicationStyle = string.IsNullOrWhiteSpace(req.CommunicationStyle) ? string.Empty : req.CommunicationStyle.Trim(),
            EmotionalDrivers = string.IsNullOrWhiteSpace(req.EmotionalDrivers) ? string.Empty : req.EmotionalDrivers.Trim(),
            SignatureTraits = signatureTraits,
            NarrativeThemes = narrativeThemes,
            DomainExpertise = domainExpertise,
            IsPublic = req.IsPublic ?? false,
            Voice = string.IsNullOrWhiteSpace(req.Voice) ? string.Empty : req.Voice.Trim(),
            Type = PersonaType.Assistant
        };
        // Set ownership model
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (Guid.TryParse(sub, out var caller))
        {
            if (role == nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator))
            {
                p.OwnedBy = OwnedBy.System;
            }
            else
            {
                p.OwnedBy = OwnedBy.User;
            }
        }
        _db.Personas.Add(p);
        await _db.SaveChangesAsync(cancellationToken);
        // If owned by user, mark link as owner
        if (p.OwnedBy == OwnedBy.User && Guid.TryParse(sub, out var creator))
        {
            var link = await _db.UserPersonas.FirstOrDefaultAsync(up => up.UserId == creator && up.PersonaId == p.Id, cancellationToken);
            if (link == null)
            {
                _db.UserPersonas.Add(new UserPersonas { UserId = creator, PersonaId = p.Id, IsDefault = false, IsOwner = true, CreatedAtUtc = DateTime.UtcNow });
            }
            else
            {
                link.IsOwner = true;
                link.UpdatedAtUtc = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
        return CreatedAtAction(nameof(Get), new { id = p.Id }, new { p.Id });
    }

    [HttpPatch("{id:guid}/visibility")]
    public async Task<IActionResult> SetVisibility(Guid id, [FromBody] VisibilityRequest req, CancellationToken cancellationToken = default)
    {
        var p = await _db.Personas.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (p == null) return NotFound(ApiErrorResponse.Create("persona_not_found", "Persona not found."));
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(sub, out var caller)) return Forbid();
        var can = role == nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator)
                  || (p.OwnedBy == OwnedBy.User && await _db.UserPersonas.AsNoTracking()
                      .AnyAsync(up => up.UserId == caller && up.PersonaId == id && up.IsOwner, cancellationToken));
        if (!can) return Forbid();
        p.IsPublic = req.IsPublic;
        p.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/access")]
    public async Task<IActionResult> GrantAccess(Guid id, [FromBody] GrantAccessRequest req, CancellationToken cancellationToken = default)
    {
        if (!await _db.Personas.AnyAsync(p => p.Id == id, cancellationToken)) return NotFound(ApiErrorResponse.Create("persona_not_found", "Persona not found."));
        if (!await _db.Users.AnyAsync(u => u.Id == req.UserId, cancellationToken)) return NotFound(ApiErrorResponse.Create("user_not_found", "User not found."));
        var persona = await _db.Personas.AsNoTracking().FirstAsync(p => p.Id == id, cancellationToken);
        var sub2 = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var role2 = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(sub2, out var caller2)) return Forbid();
        var canAccess = role2 == nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator)
                        || (persona.OwnedBy == OwnedBy.User && await _db.UserPersonas.AsNoTracking()
                            .AnyAsync(up => up.UserId == caller2 && up.PersonaId == id && up.IsOwner, cancellationToken));
        if (!canAccess) return Forbid();

        var link = await _db.UserPersonas.FirstOrDefaultAsync(x => x.UserId == req.UserId && x.PersonaId == id, cancellationToken);
        if (link == null)
        {
            link = new UserPersonas
            {
                UserId = req.UserId,
                PersonaId = id,
                IsDefault = req.IsDefault,
                Label = string.IsNullOrWhiteSpace(req.Label) ? null : req.Label.Trim()
            };
            _db.UserPersonas.Add(link);
        }
        else
        {
            link.IsDefault = req.IsDefault;
            link.Label = string.IsNullOrWhiteSpace(req.Label) ? null : req.Label.Trim();
            link.UpdatedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { link.Id });
    }

    [HttpDelete("{id:guid}/access/{userId:guid}")]
    public async Task<IActionResult> RevokeAccess(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var link = await _db.UserPersonas.FirstOrDefaultAsync(x => x.UserId == userId && x.PersonaId == id, cancellationToken);
        if (link == null) return NotFound(ApiErrorResponse.Create("persona_access_link_not_found", "Persona access link not found."));
        var persona = await _db.Personas.AsNoTracking().FirstAsync(p => p.Id == id, cancellationToken);
        var sub3 = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var role3 = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(sub3, out var caller3)) return Forbid();
        var canAccess3 = role3 == nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator)
                         || (persona.OwnedBy == OwnedBy.User && await _db.UserPersonas.AsNoTracking()
                             .AnyAsync(up => up.UserId == caller3 && up.PersonaId == id && up.IsOwner, cancellationToken));
        if (!canAccess3) return Forbid();
        _db.UserPersonas.Remove(link);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}







