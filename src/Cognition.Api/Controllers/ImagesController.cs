using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Cognition.Clients.Images;
using Cognition.Data.Relational;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/images")]
public class ImagesController : ControllerBase
{
    private readonly CognitionDbContext _db;
    private readonly IImageService _images;
    public ImagesController(CognitionDbContext db, IImageService images)
    { _db = db; _images = images; }

    public record GenerateImageRequest(Guid? ConversationId, Guid? PersonaId, string Prompt,
        int Width = 1024, int Height = 1024, string? StyleName = null, Guid? StyleId = null,
        string? NegativePrompt = null, int Steps = 30, float Guidance = 7.5f, int? Seed = null,
        string Provider = "OpenAI", string Model = "dall-e-3");

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateImageRequest req)
    {
        var style = req.StyleId.HasValue
            ? await _db.ImageStyles.AsNoTracking().FirstOrDefaultAsync(s => s.Id == req.StyleId.Value)
            : (!string.IsNullOrWhiteSpace(req.StyleName)
                ? await _db.ImageStyles.AsNoTracking().FirstOrDefaultAsync(s => s.Name == req.StyleName)
                : null);

        var p = new ImageParameters(req.Width, req.Height, style?.Name, req.NegativePrompt, req.Steps, req.Guidance, req.Seed, req.Model);
        var asset = await _images.GenerateAndSaveAsync(req.ConversationId, req.PersonaId, req.Prompt, p, req.Provider, req.Model);

        if (style != null)
        {
            asset.StyleId = style.Id;
            await _db.SaveChangesAsync();
        }
        return Ok(new { asset.Id });
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var a = await _db.ImageAssets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        return File(a.Bytes, a.MimeType);
    }

    // Querystring-friendly variant for use in <img src="..."> tags
    [AllowAnonymous]
    [HttpGet("content")]
    public async Task<IActionResult> ContentById([FromQuery] Guid id)
    {
        var a = await _db.ImageAssets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        return File(a.Bytes, a.MimeType);
    }

    [HttpGet("by-conversation/{conversationId:guid}")]
    [Authorize]
    public async Task<IActionResult> ListByConversation(Guid conversationId)
    {
        // Verify caller can see this conversation (participant or author)
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var caller)) return Forbid();
        var linkedIds = await _db.UserPersonas.AsNoTracking().Where(up => up.UserId == caller).Select(up => up.PersonaId).ToListAsync();
        var primaryId = await _db.Users.AsNoTracking().Where(u => u.Id == caller).Select(u => u.PrimaryPersonaId).FirstOrDefaultAsync();
        var allowedPersonaIds = new HashSet<Guid>(linkedIds);
        if (primaryId.HasValue) allowedPersonaIds.Add(primaryId.Value);
        var participants = await _db.ConversationParticipants.AsNoTracking().Where(p => p.ConversationId == conversationId).Select(p => p.PersonaId).ToListAsync();
        var userHasMsg = await _db.ConversationMessages.AsNoTracking().AnyAsync(m => m.ConversationId == conversationId && m.CreatedByUserId == caller);
        var allowed = userHasMsg || participants.Any(pid => allowedPersonaIds.Contains(pid));
        if (!allowed) return Forbid();

        var items = await _db.ImageAssets.AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.Provider, x.Model, x.Width, x.Height, x.MimeType, x.CreatedAtUtc, x.StyleId, x.Prompt, StyleName = x.Style != null ? x.Style.Name : null })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("by-persona/{personaId:guid}")]
    [Authorize]
    public async Task<IActionResult> ListByPersona(Guid personaId)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var caller)) return Forbid();
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var isAdmin = role == nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator);
        var linkedIds = await _db.UserPersonas.AsNoTracking().Where(up => up.UserId == caller).Select(up => up.PersonaId).ToListAsync();
        var primaryId = await _db.Users.AsNoTracking().Where(u => u.Id == caller).Select(u => u.PrimaryPersonaId).FirstOrDefaultAsync();
        var allowedPersonaIds = new HashSet<Guid>(linkedIds);
        if (primaryId.HasValue) allowedPersonaIds.Add(primaryId.Value);
        Guid? defaultSystemId = await _db.Personas.AsNoTracking()
            .Where(p => p.OwnedBy == Cognition.Data.Relational.Modules.Personas.OwnedBy.System && p.Type == Cognition.Data.Relational.Modules.Personas.PersonaType.Assistant)
            .Select(p => (Guid?)p.Id).FirstOrDefaultAsync();

        var isDefaultSystem = defaultSystemId.HasValue && personaId == defaultSystemId.Value;
        if (!(allowedPersonaIds.Contains(personaId) || (isAdmin && isDefaultSystem)))
        {
            // For non-admins viewing default system persona, allow only images from conversations they participated in
            if (!isDefaultSystem) return Forbid();
        }

        var q = _db.ImageAssets.AsNoTracking()
            .Where(x => x.CreatedByPersonaId == personaId);
        if (isDefaultSystem && !isAdmin)
        {
            q = q.Where(x => x.ConversationId != null && _db.ConversationMessages.Any(m => m.ConversationId == x.ConversationId && m.CreatedByUserId == caller));
        }
        var items = await q
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.Provider, x.Model, x.Width, x.Height, x.MimeType, x.CreatedAtUtc, x.StyleId, x.ConversationId })
            .ToListAsync();
        return Ok(items);
    }
}
