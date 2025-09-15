using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Common;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public ConversationsController(CognitionDbContext db) => _db = db;

    public record CreateConversationRequest(string? Title, Guid[] ParticipantIds);
    public record AddMessageRequest(Guid FromPersonaId, Guid? ToPersonaId, ChatRole Role, string Content);
    public record ConversationListItem(Guid Id, string? Title, DateTime CreatedAtUtc);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? participantId)
    {
        var q = _db.Conversations.AsNoTracking().AsQueryable();
        if (participantId.HasValue)
        {
            q = q.Where(c => _db.ConversationParticipants.Any(p => p.ConversationId == c.Id && p.PersonaId == participantId.Value));
        }
        var items = await q
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ConversationListItem(c.Id, c.Title, c.CreatedAtUtc))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequest req)
    {
        var conv = new Conversation { Title = req.Title, CreatedAtUtc = DateTime.UtcNow };
        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync();
        // participants
        foreach (var pid in req.ParticipantIds.Distinct())
        {
            _db.ConversationParticipants.Add(new ConversationParticipant
            {
                ConversationId = conv.Id,
                PersonaId = pid,
                JoinedAtUtc = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = conv.Id }, new { conv.Id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var conv = await _db.Conversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (conv == null) return NotFound();
        return Ok(new { conv.Id, conv.Title, conv.CreatedAtUtc });
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> ListMessages(Guid id)
    {
        var msgs = await _db.ConversationMessages.AsNoTracking()
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.Timestamp)
            .Select(m => new { m.Id, m.FromPersonaId, m.ToPersonaId, m.Role, m.Content, m.Timestamp })
            .ToListAsync();
        return Ok(msgs);
    }

    [HttpPost("{id:guid}/messages")]
    [Authorize]
    public async Task<IActionResult> AddMessage(Guid id, [FromBody] AddMessageRequest req)
    {
        if (!await _db.Conversations.AnyAsync(c => c.Id == id)) return NotFound("Conversation not found");
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();

        if (req.Role == ChatRole.User)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.PrimaryPersonaId == null || user.PrimaryPersonaId.Value != req.FromPersonaId)
            {
                return BadRequest("User-authored messages must use the user's primary persona.");
            }
        }
        var msg = new ConversationMessage
        {
            ConversationId = id,
            FromPersonaId = req.FromPersonaId,
            ToPersonaId = req.ToPersonaId,
            Role = req.Role,
            Content = req.Content,
            Timestamp = DateTime.UtcNow,
            CreatedByUserId = userId
        };
        _db.ConversationMessages.Add(msg);
        await _db.SaveChangesAsync();
        return Ok(new { msg.Id });
    }
}
