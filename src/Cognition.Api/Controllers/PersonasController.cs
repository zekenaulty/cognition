using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Users;
using Microsoft.AspNetCore.Authorization;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/personas")]
public class PersonasController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public PersonasController(CognitionDbContext db) => _db = db;

    public record PersonaCreateRequest(string Name, string? Nickname, string? Role, string? Gender, string? Essence,
        string? Beliefs, string? Background, string? CommunicationStyle, string? EmotionalDrivers,
        string[]? SignatureTraits, string[]? NarrativeThemes, string[]? DomainExpertise, bool? IsPublic);

    public record VisibilityRequest(bool IsPublic);
    public record GrantAccessRequest(Guid UserId, bool IsDefault = false, string? Label = null);
    public record PersonaUpdateRequest(
        string? Name,
        string? Nickname,
        string? Role,
        string? Gender,
        string? Essence,
        string? Beliefs,
        string? Background,
        string? CommunicationStyle,
        string? EmotionalDrivers,
        string[]? SignatureTraits,
        string[]? NarrativeThemes,
        string[]? DomainExpertise,
        bool? IsPublic
    );

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? publicOnly)
    {
        var q = _db.Personas.AsNoTracking().AsQueryable();
        if (publicOnly == true) q = q.Where(p => p.IsPublic);
        var items = await q
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Nickname, p.Role, p.IsPublic, p.Type })
            .ToListAsync();
        return Ok(items);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = nameof(Cognition.Data.Relational.Modules.Users.UserRole.User) + "," + nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator))]
    public async Task<IActionResult> Update(Guid id, [FromBody] PersonaUpdateRequest req)
    {
        var p = await _db.Personas.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (req.Name != null) p.Name = req.Name;
        if (req.Nickname != null) p.Nickname = req.Nickname;
        if (req.Role != null) p.Role = req.Role;
        if (req.Gender != null) p.Gender = req.Gender;
        if (req.Essence != null) p.Essence = req.Essence;
        if (req.Beliefs != null) p.Beliefs = req.Beliefs;
        if (req.Background != null) p.Background = req.Background;
        if (req.CommunicationStyle != null) p.CommunicationStyle = req.CommunicationStyle;
        if (req.EmotionalDrivers != null) p.EmotionalDrivers = req.EmotionalDrivers;
        if (req.SignatureTraits != null) p.SignatureTraits = req.SignatureTraits;
        if (req.NarrativeThemes != null) p.NarrativeThemes = req.NarrativeThemes;
        if (req.DomainExpertise != null) p.DomainExpertise = req.DomainExpertise;
        if (req.IsPublic.HasValue) p.IsPublic = req.IsPublic.Value;
        p.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator))]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await _db.Personas.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        _db.Personas.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var p = await _db.Personas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        return Ok(p);
    }

    [HttpPost]
    [Authorize(Roles = nameof(Cognition.Data.Relational.Modules.Users.UserRole.User) + "," + nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator))]
    public async Task<IActionResult> Create([FromBody] PersonaCreateRequest req)
    {
        var p = new Persona
        {
            Name = req.Name,
            Nickname = req.Nickname ?? string.Empty,
            Role = req.Role ?? string.Empty,
            Gender = req.Gender ?? string.Empty,
            Essence = req.Essence ?? string.Empty,
            Beliefs = req.Beliefs ?? string.Empty,
            Background = req.Background ?? string.Empty,
            CommunicationStyle = req.CommunicationStyle ?? string.Empty,
            EmotionalDrivers = req.EmotionalDrivers ?? string.Empty,
            SignatureTraits = req.SignatureTraits,
            NarrativeThemes = req.NarrativeThemes,
            DomainExpertise = req.DomainExpertise,
            IsPublic = req.IsPublic ?? false,
            Type = PersonaType.Assistant
        };
        _db.Personas.Add(p);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = p.Id }, new { p.Id });
    }

    [HttpPatch("{id:guid}/visibility")]
    public async Task<IActionResult> SetVisibility(Guid id, [FromBody] VisibilityRequest req)
    {
        var p = await _db.Personas.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        p.IsPublic = req.IsPublic;
        p.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/access")]
    public async Task<IActionResult> GrantAccess(Guid id, [FromBody] GrantAccessRequest req)
    {
        if (!await _db.Personas.AnyAsync(p => p.Id == id)) return NotFound("Persona not found");
        if (!await _db.Users.AnyAsync(u => u.Id == req.UserId)) return NotFound("User not found");

        var link = await _db.UserPersonas.FirstOrDefaultAsync(x => x.UserId == req.UserId && x.PersonaId == id);
        if (link == null)
        {
            link = new UserPersona { UserId = req.UserId, PersonaId = id, IsDefault = req.IsDefault, Label = req.Label };
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

    [HttpDelete("{id:guid}/access/{userId:guid}")]
    public async Task<IActionResult> RevokeAccess(Guid id, Guid userId)
    {
        var link = await _db.UserPersonas.FirstOrDefaultAsync(x => x.UserId == userId && x.PersonaId == id);
        if (link == null) return NotFound();
        _db.UserPersonas.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
