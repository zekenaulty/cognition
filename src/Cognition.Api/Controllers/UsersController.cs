using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Users;
using Cognition.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public UsersController(CognitionDbContext db) => _db = db;

    public record RegisterRequest(string Username, string Password, string? Email);
    public record LoginRequest(string Username, string Password);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    public record UpdateProfileRequest(string? Email, string? Username);
    public record MeResponse(Guid Id, string Username, string? Email, Guid? PrimaryPersonaId);
    public record SetPrimaryPersonaRequest(Guid PersonaId);
    public record LinkPersonaRequest(Guid PersonaId, bool IsDefault = false, string? Label = null);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var norm = req.Username.Trim().ToUpperInvariant();
        if (await _db.Users.AnyAsync(u => u.NormalizedUsername == norm))
            return Conflict("Username already exists");

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
        await _db.SaveChangesAsync();

        // Create default user persona and link
        var persona = new Data.Relational.Modules.Personas.Persona
        {
            Name = req.Username.Trim(),
            Nickname = req.Username.Trim(),
            Role = "User",
            Type = Data.Relational.Modules.Personas.PersonaType.User,
            OwnerUserId = user.Id,
            IsPublic = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Personas.Add(persona);
        await _db.SaveChangesAsync();

        user.PrimaryPersonaId = persona.Id;
        _db.UserPersonas.Add(new Data.Relational.Modules.Users.UserPersona
        {
            UserId = user.Id,
            PersonaId = persona.Id,
            IsDefault = true,
            Label = persona.Nickname,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Username, user.PrimaryPersonaId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var norm = req.Username.Trim().ToUpperInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == norm);
        if (user == null) return Unauthorized();
        var ok = PasswordHasher.Verify(req.Password, user.PasswordSalt, user.PasswordHash);
        if (!ok) return Unauthorized();
        var (accessToken, expiresAt) = JwtTokenHelper.IssueAccessToken(user);
        var refresh = await JwtTokenHelper.IssueRefreshTokenAsync(_db, user);
        return Ok(new { user.Id, user.Username, user.PrimaryPersonaId, accessToken, expiresAt, refreshToken = refresh.Token });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var result = await JwtTokenHelper.RotateRefreshAsync(_db, refreshToken);
        if (result == null) return Unauthorized();
        return Ok(new { accessToken = result.Value.AccessToken, expiresAt = result.Value.ExpiresAt, refreshToken = result.Value.RefreshToken });
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.Users.AsNoTracking()
            .OrderBy(u => u.Username)
            .Select(u => new { u.Id, u.Username, u.Email, u.PrimaryPersonaId, u.CreatedAtUtc, u.IsActive })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();
        return Ok(new { u.Id, u.Username, u.Email, u.PrimaryPersonaId, u.IsActive, u.CreatedAtUtc, u.UpdatedAtUtc });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (u == null) return Unauthorized();
        return Ok(new MeResponse(u.Id, u.Username, u.Email, u.PrimaryPersonaId));
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateProfileRequest req)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(sub, out var caller) || (caller != id && role != nameof(UserRole.Administrator)))
            return Forbid();
        if (!string.IsNullOrWhiteSpace(req.Username) && !string.Equals(req.Username, u.Username, StringComparison.Ordinal))
        {
            var norm = req.Username.Trim().ToUpperInvariant();
            var exists = await _db.Users.AnyAsync(x => x.NormalizedUsername == norm && x.Id != id);
            if (exists) return Conflict("Username already exists");
            u.Username = req.Username.Trim();
            u.NormalizedUsername = norm;
        }
        if (req.Email != null) // allow clearing email by passing empty string
        {
            var normEmail = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim().ToUpperInvariant();
            // Only enforce uniqueness if provided
            if (!string.IsNullOrWhiteSpace(req.Email))
            {
                var taken = await _db.Users.AnyAsync(x => x.NormalizedEmail == normEmail && x.Id != id);
                if (taken) return Conflict("Email already in use");
            }
            u.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
            u.NormalizedEmail = normEmail;
        }
        u.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest req)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();
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
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/primary-persona")]
    [Authorize]
    public async Task<IActionResult> SetPrimaryPersona(Guid id, [FromBody] SetPrimaryPersonaRequest req)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(sub, out var caller) || (caller != id && role != nameof(UserRole.Administrator)))
            return Forbid();
        var persona = await _db.Personas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.PersonaId);
        if (persona == null) return BadRequest("Persona not found");
        if (persona.Type != Data.Relational.Modules.Personas.PersonaType.User)
            return BadRequest("Primary persona must be a User persona.");
        if (persona.OwnerUserId != id)
            return BadRequest("Primary persona must be owned by the user.");
        u.PrimaryPersonaId = req.PersonaId;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/personas")]
    public async Task<IActionResult> LinkPersona(Guid id, [FromBody] LinkPersonaRequest req)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == id)) return NotFound("User not found");
        if (!await _db.Personas.AnyAsync(p => p.Id == req.PersonaId)) return NotFound("Persona not found");
        var link = await _db.UserPersonas.FirstOrDefaultAsync(up => up.UserId == id && up.PersonaId == req.PersonaId);
        if (link == null)
        {
            link = new UserPersona { UserId = id, PersonaId = req.PersonaId, IsDefault = req.IsDefault, Label = req.Label, CreatedAtUtc = DateTime.UtcNow };
            _db.UserPersonas.Add(link);
        }
        else
        {
            link.IsDefault = req.IsDefault;
            link.Label = req.Label;
            link.UpdatedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return Ok(new { link.Id });
    }

    [HttpDelete("{id:guid}/personas/{personaId:guid}")]
    public async Task<IActionResult> UnlinkPersona(Guid id, Guid personaId)
    {
        var link = await _db.UserPersonas.FirstOrDefaultAsync(up => up.UserId == id && up.PersonaId == personaId);
        if (link == null) return NotFound();
        _db.UserPersonas.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
