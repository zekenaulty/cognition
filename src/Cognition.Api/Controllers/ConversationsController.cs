using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Security;
using Cognition.Api.Infrastructure.ErrorHandling;
using Cognition.Api.Infrastructure.Validation;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Api.Services;
using Cognition.Api.Services.Conversations;
using Cognition.Data.Relational.Modules.LLM;
using Cognition.Api.Models.Conversations;
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
    private readonly IConversationSettingsService _settings;
    private readonly IConversationAccessService _access;
    private readonly IConversationFactory _factory;
    private readonly IConversationMessagesService _messages;
    public ConversationsController(CognitionDbContext db, IHubContext<ChatHub> hub, IConversationSettingsService settings, IConversationAccessService access, IConversationFactory factory, IConversationMessagesService messages) { _db = db; _hub = hub; _settings = settings; _access = access; _factory = factory; _messages = messages; }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? participantId, [FromQuery] Guid? agentId, CancellationToken cancellationToken = default)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var caller)) return Forbid();
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var isAdmin = role == nameof(Cognition.Data.Relational.Modules.Users.UserRole.Administrator);
        var items = await _access.ListAsync(caller, isAdmin, participantId, agentId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}/settings")]
    public async Task<ActionResult<ConversationSettingsResponse>> GetSettings(Guid id, CancellationToken cancellationToken = default)
    {
        var conv = await _db.Conversations
            .AsNoTracking()
            .Include(c => c.Agent)
                .ThenInclude(a => a.ClientProfile)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (conv is null)
        {
            return NotFound();
        }

        var resolved = await _settings.ResolveSettingsAsync(conv, cancellationToken).ConfigureAwait(false);
        return resolved;
    }

    [HttpPatch("{id:guid}/settings")]
    public async Task<IActionResult> UpdateSettings(Guid id, [FromBody] ConversationSettingsRequest req, CancellationToken cancellationToken = default)
    {
        var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);
        if (conv is null)
        {
            return NotFound();
        }

        if (req.ProviderId.HasValue || req.ModelId.HasValue)
        {
            var valid = await _settings.ValidateProviderModelAsync(req.ProviderId, req.ModelId, cancellationToken);
            if (!valid)
            {
                return BadRequest(ApiErrorResponse.Create("invalid_provider_or_model", "Specified provider/model is invalid, inactive, or mismatched."));
            }
        }

        conv.Metadata ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (req.ProviderId.HasValue)
        {
            conv.Metadata["providerId"] = req.ProviderId.Value;
        }
        if (req.ModelId.HasValue)
        {
            conv.Metadata["modelId"] = req.ModelId.Value;
        }
        conv.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            var id = await _factory.CreateAsync(req, User, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id }, new { Id = id });
        }
        catch (InvalidOperationException ex) when (ex.Message == "agent_not_found")
        {
            return NotFound(ApiErrorResponse.Create("agent_not_found", "Agent not found."));
        }
    }

    // Removed legacy v2 create endpoint; POST /api/conversations accepts AgentId

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var conv = await _db.Conversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (conv == null) return NotFound(ApiErrorResponse.Create("conversation_not_found", "Conversation not found."));

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
        if (conv == null) return NotFound(ApiErrorResponse.Create("conversation_not_found", "Conversation not found."));

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
        if (!await _messages.ConversationExistsAsync(id, cancellationToken)) return NotFound(ApiErrorResponse.Create("conversation_not_found", "Conversation not found."));
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();

        if (!await _messages.ValidateUserMessageAsync(userId, req, cancellationToken))
        {
            return BadRequest(ApiErrorResponse.Create("user_persona_mismatch", "User-authored messages must use the user's primary persona."));
        }

        var msgId = await _messages.AddMessageAsync(id, userId, req, cancellationToken);
        await _hub.Clients.Group(id.ToString()).SendAsync("AssistantMessageVersionAppended", new { ConversationId = id, MessageId = msgId, Content = req.Content.Trim(), VersionIndex = 0 }, cancellationToken);
        return Ok(new { Id = msgId });
    }

    [HttpPost("{conversationId:guid}/messages/{messageId:guid}/versions")]
    public async Task<IActionResult> AddVersion(Guid conversationId, Guid messageId, [FromBody] AddVersionRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            var (msgId, versionIndex) = await _messages.AddVersionAsync(conversationId, messageId, req, cancellationToken);
            await _hub.Clients.Group(conversationId.ToString()).SendAsync("AssistantMessageVersionAppended", new { ConversationId = conversationId, MessageId = messageId, Content = req.Content.Trim(), VersionIndex = versionIndex }, cancellationToken);
            return Ok(new { Id = msgId, VersionIndex = versionIndex });
        }
        catch (InvalidOperationException ex) when (ex.Message == "conversation_message_not_found")
        {
            return NotFound(ApiErrorResponse.Create("conversation_message_not_found", "Conversation message not found."));
        }
    }

    [HttpPatch("{conversationId:guid}/messages/{messageId:guid}/active-version")]
    public async Task<IActionResult> SetActiveVersion(Guid conversationId, Guid messageId, [FromBody] SetActiveVersionRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            var (msgId, versionIndex) = await _messages.SetActiveVersionAsync(conversationId, messageId, req.Index, cancellationToken);
            await _hub.Clients.Group(conversationId.ToString()).SendAsync("AssistantActiveVersionChanged", new { ConversationId = conversationId, MessageId = messageId, VersionIndex = versionIndex }, cancellationToken);
            return Ok(new { Id = msgId, ActiveVersionIndex = versionIndex });
        }
        catch (InvalidOperationException ex) when (ex.Message == "conversation_message_not_found")
        {
            return NotFound(ApiErrorResponse.Create("conversation_message_not_found", "Conversation message not found."));
        }
        catch (InvalidOperationException ex) when (ex.Message == "version_not_found")
        {
            return BadRequest(ApiErrorResponse.Create("version_not_found", "Version index not found."));
        }
    }

    private Guid? TryReadMetadataGuid(Dictionary<string, object?>? metadata, string key) => _settings.TryReadMetadataGuid(metadata, key);
}
