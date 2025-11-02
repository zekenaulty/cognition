using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Security;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOrHigher)]
[ApiController]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    private readonly IHubContext<ChatHub> _hub;
    public ConversationsController(CognitionDbContext db, IHubContext<ChatHub> hub) { _db = db; _hub = hub; }

    public record CreateConversationRequest(Guid AgentId, string? Title, Guid[] ParticipantIds);
    public record AddMessageRequest(Guid FromPersonaId, Guid? ToPersonaId, ChatRole Role, string Content, string? Metatype = null);
    public record ConversationListItem(Guid Id, string? Title, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
    public record AddVersionRequest(string Content);
    public record SetActiveVersionRequest(int Index);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? participantId, [FromQuery] Guid? agentId, CancellationToken cancellationToken = default)
    {
        var q = _db.Conversations.AsNoTracking().AsQueryable();

        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var caller)) return Forbid();
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var isAdmin = role == nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator);

        var linkedIds = await _db.UserPersonas.AsNoTracking()
            .Where(up => up.UserId == caller)
            .Select(up => up.PersonaId)
            .ToListAsync(cancellationToken);
        var primaryId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == caller)
            .Select(u => u.PrimaryPersonaId)
            .FirstOrDefaultAsync(cancellationToken);
        var allowedPersonaIds = new HashSet<Guid>(linkedIds);
        if (primaryId.HasValue) allowedPersonaIds.Add(primaryId.Value);

        Guid? defaultSystemId = await _db.Personas.AsNoTracking()
            .Where(p => p.OwnedBy == Cognition.Data.Relational.Modules.Personas.OwnedBy.System && p.Type == Cognition.Data.Relational.Modules.Personas.PersonaType.Assistant)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (agentId.HasValue)
        {
            q = q.Where(c => c.AgentId == agentId.Value);
        }
        else if (participantId.HasValue)
        {
            var pid = participantId.Value;
            var pidIsAllowed = allowedPersonaIds.Contains(pid) || (defaultSystemId.HasValue && pid == defaultSystemId.Value);
            if (!pidIsAllowed) return Ok(Array.Empty<ConversationListItem>());

            if (!isAdmin && defaultSystemId.HasValue && pid == defaultSystemId.Value)
            {
                q = q.Where(c =>
                    _db.ConversationParticipants.Any(p => p.ConversationId == c.Id && p.PersonaId == pid)
                    && _db.ConversationMessages.Any(m => m.ConversationId == c.Id && m.CreatedByUserId == caller));
            }
            else
            {
                q = q.Where(c => _db.ConversationParticipants.Any(p => p.ConversationId == c.Id && p.PersonaId == pid));
            }
        }
        else
        {
            q = q.Where(c =>
                _db.ConversationMessages.Any(m => m.ConversationId == c.Id && m.CreatedByUserId == caller)
                || _db.ConversationParticipants.Any(p => p.ConversationId == c.Id && allowedPersonaIds.Contains(p.PersonaId))
            );
        }

        var items = await q
            .OrderByDescending(c => c.UpdatedAtUtc ?? c.CreatedAtUtc)
            .Select(c => new ConversationListItem(c.Id, c.Title, c.CreatedAtUtc, c.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequest req, CancellationToken cancellationToken = default)
    {
        var conv = new Conversation { Title = req.Title, CreatedAtUtc = DateTime.UtcNow, AgentId = req.AgentId };
        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync(cancellationToken);
        // participants (optional)
        var participants = (req.ParticipantIds ?? Array.Empty<Guid>()).Distinct().ToList();
        // Always include the caller's primary persona as a participant if available
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(sub, out var caller))
        {
            var primaryId = await _db.Users.AsNoTracking().Where(u => u.Id == caller).Select(u => u.PrimaryPersonaId).FirstOrDefaultAsync(cancellationToken);
            if (primaryId.HasValue && !participants.Contains(primaryId.Value)) participants.Add(primaryId.Value);
        }
        if (participants.Count == 0)
        {
            var defaultPid = await _db.Personas
                .AsNoTracking()
                .Where(p => p.OwnedBy == Cognition.Data.Relational.Modules.Personas.OwnedBy.System && p.Type == Cognition.Data.Relational.Modules.Personas.PersonaType.Assistant)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (defaultPid.HasValue) participants.Add(defaultPid.Value);
        }
        foreach (var pid in participants)
        {
            _db.ConversationParticipants.Add(new ConversationParticipant
            {
                ConversationId = conv.Id,
                PersonaId = pid,
                JoinedAtUtc = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = conv.Id }, new { conv.Id });
    }

    // Removed legacy v2 create endpoint; POST /api/conversations accepts AgentId

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var conv = await _db.Conversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (conv == null) return NotFound();

        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var caller)) return Forbid();

        var linkedIds = await _db.UserPersonas.AsNoTracking()
            .Where(up => up.UserId == caller)
            .Select(up => up.PersonaId)
            .ToListAsync(cancellationToken);
        var primaryId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == caller)
            .Select(u => u.PrimaryPersonaId)
            .FirstOrDefaultAsync(cancellationToken);
        var allowedPersonaIds = new HashSet<Guid>(linkedIds);
        if (primaryId.HasValue) allowedPersonaIds.Add(primaryId.Value);

        var participants = await _db.ConversationParticipants.AsNoTracking()
            .Where(p => p.ConversationId == id)
            .Select(p => p.PersonaId)
            .ToListAsync(cancellationToken);

        var userHasMsg = await _db.ConversationMessages.AsNoTracking().AnyAsync(m => m.ConversationId == id && m.CreatedByUserId == caller, cancellationToken);
        var allowed = userHasMsg || participants.Any(pid => allowedPersonaIds.Contains(pid));
        if (!allowed) return Forbid();

        return Ok(new { conv.Id, conv.Title, conv.CreatedAtUtc });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (conv == null) return NotFound();

        // Delete related rows explicitly
        var msgIds = await _db.ConversationMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == id)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);
        if (msgIds.Count > 0)
        {
            var versions = _db.ConversationMessageVersions.Where(v => msgIds.Contains(v.ConversationMessageId));
            _db.ConversationMessageVersions.RemoveRange(versions);
            await _db.SaveChangesAsync(cancellationToken);
        }
        var messages = _db.ConversationMessages.Where(m => m.ConversationId == id);
        _db.ConversationMessages.RemoveRange(messages);

        var participants = _db.ConversationParticipants.Where(p => p.ConversationId == id);
        _db.ConversationParticipants.RemoveRange(participants);

        var summaries = _db.ConversationSummaries.Where(s => s.ConversationId == id);
        _db.ConversationSummaries.RemoveRange(summaries);

        var plans = _db.ConversationPlans.Where(p => p.ConversationId == id);
        var planIds = await plans.Select(p => p.Id).ToListAsync(cancellationToken);
        if (planIds.Count > 0)
        {
            var tasks = _db.ConversationTasks.Where(t => planIds.Contains(t.ConversationPlanId));
            _db.ConversationTasks.RemoveRange(tasks);
        }
        _db.ConversationPlans.RemoveRange(plans);

        var thoughts = _db.ConversationThoughts.Where(t => t.ConversationId == id);
        _db.ConversationThoughts.RemoveRange(thoughts);

        var wfState = await _db.ConversationWorkflowStates.FirstOrDefaultAsync(s => s.ConversationId == id, cancellationToken);
        if (wfState != null) _db.ConversationWorkflowStates.Remove(wfState);

        var wfEvents = _db.WorkflowEvents.Where(e => e.ConversationId == id);
        _db.WorkflowEvents.RemoveRange(wfEvents);

        // Finally remove conversation
        _db.Conversations.Remove(conv);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> ListMessages(Guid id, CancellationToken cancellationToken = default)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var caller)) return Forbid();
        var linkedIds = await _db.UserPersonas.AsNoTracking().Where(up => up.UserId == caller).Select(up => up.PersonaId).ToListAsync(cancellationToken);
        var primaryId = await _db.Users.AsNoTracking().Where(u => u.Id == caller).Select(u => u.PrimaryPersonaId).FirstOrDefaultAsync(cancellationToken);
        var allowedPersonaIds = new HashSet<Guid>(linkedIds);
        if (primaryId.HasValue) allowedPersonaIds.Add(primaryId.Value);
        var participants = await _db.ConversationParticipants.AsNoTracking().Where(p => p.ConversationId == id).Select(p => p.PersonaId).ToListAsync(cancellationToken);
        var userHasMsg = await _db.ConversationMessages.AsNoTracking().AnyAsync(m => m.ConversationId == id && m.CreatedByUserId == caller, cancellationToken);
        var allowed = userHasMsg || participants.Any(pid => allowedPersonaIds.Contains(pid));
        if (!allowed) return Forbid();

        var msgs = await _db.ConversationMessages.AsNoTracking()
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.Timestamp)
            .Select(m => new {
                m.Id,
                m.FromPersonaId,
                m.ToPersonaId,
                m.Role,
                m.Content,
                m.Timestamp,
                Versions = _db.ConversationMessageVersions
                    .AsNoTracking()
                    .Where(v => v.ConversationMessageId == m.Id)
                    .OrderBy(v => v.VersionIndex)
                    .Select(v => v.Content)
                    .ToList(),
                VersionIndex = m.ActiveVersionIndex
            })
            .ToListAsync(cancellationToken);
        return Ok(msgs);
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> AddMessage(Guid id, [FromBody] AddMessageRequest req, CancellationToken cancellationToken = default)
    {
        if (!await _db.Conversations.AnyAsync(c => c.Id == id, cancellationToken)) return NotFound("Conversation not found");
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();

        if (req.Role == ChatRole.User)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null || user.PrimaryPersonaId == null || user.PrimaryPersonaId.Value != req.FromPersonaId)
            {
                return BadRequest("User-authored messages must use the user's primary persona.");
            }
        }
        var fromAgentId = await _db.Agents.AsNoTracking()
            .Where(a => a.PersonaId == req.FromPersonaId)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var msg = new ConversationMessage
        {
            ConversationId = id,
            FromPersonaId = req.FromPersonaId,
            FromAgentId = fromAgentId,
            ToPersonaId = req.ToPersonaId,
            Role = req.Role,
            Content = req.Content,
            Timestamp = DateTime.UtcNow,
            CreatedByUserId = userId,
            Metatype = req.Metatype
        };
        _db.ConversationMessages.Add(msg);
        await _db.SaveChangesAsync(cancellationToken);
        // Initialize version 0
        var versionEntity = new ConversationMessageVersion
        {
            ConversationMessageId = msg.Id,
            VersionIndex = 0,
            Content = req.Content,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.ConversationMessageVersions.Add(versionEntity);
        msg.ActiveVersionIndex = 0;
        await _db.SaveChangesAsync(cancellationToken);
        await _hub.Clients.Group(id.ToString()).SendAsync("AssistantMessageVersionAppended", new { ConversationId = id, MessageId = msg.Id, Content = versionEntity.Content, VersionIndex = versionEntity.VersionIndex }, cancellationToken);
        // Mark conversation as updated
        try
        {
            var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (conv != null)
            {
                conv.UpdatedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        catch { }
        return Ok(new { msg.Id });
    }

    [HttpPost("{conversationId:guid}/messages/{messageId:guid}/versions")]
    public async Task<IActionResult> AddVersion(Guid conversationId, Guid messageId, [FromBody] AddVersionRequest req, CancellationToken cancellationToken = default)
    {
        var msg = await _db.ConversationMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken);
        if (msg == null) return NotFound();
        var maxIndex = await _db.ConversationMessageVersions.Where(v => v.ConversationMessageId == messageId).Select(v => (int?)v.VersionIndex).MaxAsync(cancellationToken) ?? -1;
        var next = maxIndex + 1;
        var v = new ConversationMessageVersion
        {
            ConversationMessageId = messageId,
            VersionIndex = next,
            Content = req.Content,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.ConversationMessageVersions.Add(v);
        msg.ActiveVersionIndex = next;
        msg.Content = req.Content;
        await _db.SaveChangesAsync(cancellationToken);
        await _hub.Clients.Group(conversationId.ToString()).SendAsync("AssistantMessageVersionAppended", new { ConversationId = conversationId, MessageId = messageId, Content = v.Content, VersionIndex = next }, cancellationToken);
        return Ok(new { msg.Id, VersionIndex = next });
    }

    [HttpPatch("{conversationId:guid}/messages/{messageId:guid}/active-version")]
    public async Task<IActionResult> SetActiveVersion(Guid conversationId, Guid messageId, [FromBody] SetActiveVersionRequest req, CancellationToken cancellationToken = default)
    {
        var msg = await _db.ConversationMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken);
        if (msg == null) return NotFound();
        var target = await _db.ConversationMessageVersions.AsNoTracking().FirstOrDefaultAsync(v => v.ConversationMessageId == messageId && v.VersionIndex == req.Index, cancellationToken);
        if (target == null) return BadRequest("Version index not found");
        msg.ActiveVersionIndex = req.Index;
        msg.Content = target.Content;
        await _db.SaveChangesAsync(cancellationToken);
        await _hub.Clients.Group(conversationId.ToString()).SendAsync("AssistantActiveVersionChanged", new { ConversationId = conversationId, MessageId = messageId, VersionIndex = req.Index }, cancellationToken);
        return Ok(new { msg.Id, msg.ActiveVersionIndex });
    }
}
