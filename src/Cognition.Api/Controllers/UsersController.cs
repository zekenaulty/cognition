using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Users;
using Cognition.Api.Infrastructure;
using Cognition.Api.Infrastructure.Validation;
using Cognition.Api.Infrastructure.Security;
using Cognition.Api.Infrastructure.ErrorHandling;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOrHigher)]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public UsersController(CognitionDbContext db) => _db = db;

    public record RegisterRequest(
        [property: Required, StringLength(64, MinimumLength = 1)] string Username,
        [property: Required, MinLength(8)] string Password,
        [property: EmailAddress] string? Email);
    public sealed class LoginRequest
    {
        [Required, StringLength(64, MinimumLength = 1)]
        public string Username { get; init; } = string.Empty;

        [Required]
        public string Password { get; init; } = string.Empty;
    }
    public record ChangePasswordRequest(
        [property: Required] string CurrentPassword,
        [property: Required, MinLength(8)] string NewPassword);
    public record UpdateProfileRequest(
        string? Email,
        string? Username);
    public record MeResponse(Guid Id, string Username, string? Email, Guid? PrimaryPersonaId);
    public record SetPrimaryPersonaRequest([property: NotEmptyGuid] Guid PersonaId);
    public record LinkPersonaRequest([property: NotEmptyGuid] Guid PersonaId, bool IsDefault = false, [property: StringLength(128)] string? Label = null);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken cancellationToken = default)
    {
        var norm = req.Username.Trim().ToUpperInvariant();
        if (await _db.Users.AnyAsync(u => u.NormalizedUsername == norm, cancellationToken))
            return Conflict(ApiErrorResponse.Create("username_conflict", "Username already exists."));

        var (hash, salt, algo, ver) = PasswordHasher.Hash(req.Password);
        var user = new User
        {
            Username = req.Username.Trim(),
            NormalizedUsername = norm,
            Email = req.Email,
            NormalizedEmail = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim().ToUpperInvariant(),
            EmailConfirmed = false,
            PasswordHash = hash,
            PasswordSalt = salt,
            PasswordAlgo = algo,
            PasswordHashVersion = ver,
            PasswordUpdatedAtUtc = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        // Create default user persona and link
        var persona = new Data.Relational.Modules.Personas.Persona
        {
            Name = req.Username.Trim(),
            Nickname = req.Username.Trim(),
            Role = "User",
            Type = Data.Relational.Modules.Personas.PersonaType.User,
            OwnedBy = Data.Relational.Modules.Personas.OwnedBy.User,
            IsPublic = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Personas.Add(persona);
        await _db.SaveChangesAsync(cancellationToken);

        user.PrimaryPersonaId = persona.Id;
        _db.UserPersonas.Add(new Data.Relational.Modules.Users.UserPersonas
        {
            UserId = user.Id,
            PersonaId = persona.Id,
            IsDefault = true,
            IsOwner = true,
            Label = persona.Nickname,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { user.Id, user.Username, user.PrimaryPersonaId });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken cancellationToken = default)
    {
        var norm = req.Username.Trim().ToUpperInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == norm, cancellationToken);
        if (user == null) return Unauthorized();
        var ok = PasswordHasher.Verify(req.Password, user.PasswordSalt, user.PasswordHash);
        if (!ok) return Unauthorized();
        var (accessToken, expiresAt) = JwtTokenHelper.IssueAccessToken(user);
        var refresh = await JwtTokenHelper.IssueRefreshTokenAsync(_db, user, cancellationToken);
        return Ok(new { user.Id, user.Username, user.PrimaryPersonaId, accessToken, expiresAt, refreshToken = refresh.Token });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken, CancellationToken cancellationToken = default)
    {
        var result = await JwtTokenHelper.RotateRefreshAsync(_db, refreshToken, cancellationToken);
        if (result == null) return Unauthorized();
        return Ok(new { accessToken = result.Value.AccessToken, expiresAt = result.Value.ExpiresAt, refreshToken = result.Value.RefreshToken });
    }

    [Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default)
    {
        var items = await _db.Users.AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new { u.Id, u.Username, u.Email, u.PrimaryPersonaId, u.CreatedAtUtc, u.IsActive })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (u == null) return NotFound(ApiErrorResponse.Create("user_not_found", "User not found."));
        return Ok(new { u.Id, u.Username, u.Email, u.PrimaryPersonaId, u.IsActive, u.CreatedAtUtc, u.UpdatedAtUtc });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken = default)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (u == null) return Unauthorized();
        return Ok(new MeResponse(u.Id, u.Username, u.Email, u.PrimaryPersonaId));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateProfileRequest req, CancellationToken cancellationToken = default)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (u == null) return NotFound(ApiErrorResponse.Create("user_not_found", "User not found."));
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(sub, out var caller) || (caller != id && role != nameof(UserRole.Administrator)))
            return Forbid();
        if (!string.IsNullOrWhiteSpace(req.Username) && !string.Equals(req.Username, u.Username, StringComparison.Ordinal))
        {
            var norm = req.Username.Trim().ToUpperInvariant();
            var exists = await _db.Users.AnyAsync(x => x.NormalizedUsername == norm && x.Id != id, cancellationToken);
            if (exists) return Conflict(ApiErrorResponse.Create("username_conflict", "Username already exists."));
            u.Username = req.Username.Trim();
            u.NormalizedUsername = norm;
        }
        if (req.Email != null) // allow clearing email by passing empty string
        {
            var normEmail = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim().ToUpperInvariant();
            // Only enforce uniqueness if provided
            if (!string.IsNullOrWhiteSpace(req.Email))
            {
                var taken = await _db.Users.AnyAsync(x => x.NormalizedEmail == normEmail && x.Id != id, cancellationToken);
                if (taken) return Conflict(ApiErrorResponse.Create("email_conflict", "Email already in use."));
            }
            u.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
            u.NormalizedEmail = normEmail;
        }
        u.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/password")]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest req, CancellationToken cancellationToken = default)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (u == null) return NotFound(ApiErrorResponse.Create("user_not_found", "User not found."));
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(sub, out var caller) || (caller != id && role != nameof(UserRole.Administrator)))
            return Forbid();
        if (!PasswordHasher.Verify(req.CurrentPassword, u.PasswordSalt, u.PasswordHash)) return Unauthorized();
        var (hash, salt, algo, ver) = PasswordHasher.Hash(req.NewPassword);
        u.PasswordHash = hash;
        u.PasswordSalt = salt;
        u.PasswordAlgo = algo;
        u.PasswordHashVersion = ver;
        u.PasswordUpdatedAtUtc = DateTime.UtcNow;
        u.SecurityStamp = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/primary-persona")]
    public async Task<IActionResult> SetPrimaryPersona(Guid id, [FromBody] SetPrimaryPersonaRequest req, CancellationToken cancellationToken = default)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (u == null) return NotFound(ApiErrorResponse.Create("user_not_found", "User not found."));
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(sub, out var caller) || (caller != id && role != nameof(UserRole.Administrator)))
            return Forbid();
        var persona = await _db.Personas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.PersonaId, cancellationToken);
        if (persona == null) return BadRequest(ApiErrorResponse.Create("persona_not_found", "Persona not found."));
        if (persona.Type != Data.Relational.Modules.Personas.PersonaType.User)
            return BadRequest(ApiErrorResponse.Create("primary_persona_type_invalid", "Primary persona must be a User persona."));
        var isOwner = await _db.UserPersonas.AsNoTracking().AnyAsync(up => up.UserId == id && up.PersonaId == req.PersonaId && up.IsOwner, cancellationToken);
        if (!isOwner) return BadRequest(ApiErrorResponse.Create("primary_persona_not_owned", "Primary persona must be owned by the user."));
        u.PrimaryPersonaId = req.PersonaId;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/personas")]
    public async Task<IActionResult> LinkPersona(Guid id, [FromBody] LinkPersonaRequest req, CancellationToken cancellationToken = default)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(sub, out var caller) || (caller != id && role != nameof(UserRole.Administrator)))
            return Forbid();
        if (!await _db.Users.AnyAsync(u => u.Id == id, cancellationToken)) return NotFound(ApiErrorResponse.Create("user_not_found", "User not found."));
        if (!await _db.Personas.AnyAsync(p => p.Id == req.PersonaId, cancellationToken)) return NotFound(ApiErrorResponse.Create("persona_not_found", "Persona not found."));
        var link = await _db.UserPersonas.FirstOrDefaultAsync(up => up.UserId == id && up.PersonaId == req.PersonaId, cancellationToken);
        if (link == null)
        {
            link = new UserPersonas { UserId = id, PersonaId = req.PersonaId, IsDefault = req.IsDefault, Label = req.Label, CreatedAtUtc = DateTime.UtcNow };
            _db.UserPersonas.Add(link);
        }
        else
        {
            link.IsDefault = req.IsDefault;
            link.Label = req.Label;
            link.UpdatedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { link.Id });
    }

    [HttpDelete("{id:guid}/personas/{personaId:guid}")]
    public async Task<IActionResult> UnlinkPersona(Guid id, Guid personaId, CancellationToken cancellationToken = default)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(sub, out var caller) || (caller != id && role != nameof(UserRole.Administrator)))
            return Forbid();
        var link = await _db.UserPersonas.FirstOrDefaultAsync(up => up.UserId == id && up.PersonaId == personaId, cancellationToken);
        if (link == null) return NotFound(ApiErrorResponse.Create("persona_link_not_found", "Persona link not found."));
        _db.UserPersonas.Remove(link);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/personas")]
    public async Task<IActionResult> ListUserPersonas(Guid id, CancellationToken cancellationToken = default)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(sub, out var caller) || (caller != id && role != nameof(UserRole.Administrator)))
            return Forbid();
        var items = await _db.UserPersonas.AsNoTracking()
            .Where(up => up.UserId == id)
            .Join(_db.Personas.AsNoTracking(), up => up.PersonaId, p => p.Id, (up, p) => new { p.Id, p.Name, p.Type, p.IsPublic })
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return Ok(items);
    }
}





