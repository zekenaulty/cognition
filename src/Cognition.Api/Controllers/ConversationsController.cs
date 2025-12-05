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
    private readonly ILlmDefaultService _llmDefaultService;
    public ConversationsController(CognitionDbContext db, IHubContext<ChatHub> hub, ILlmDefaultService llmDefaultService) { _db = db; _hub = hub; _llmDefaultService = llmDefaultService; }

    public sealed class CreateConversationRequest
    {
        [NotEmptyGuid]
        public Guid AgentId { get; init; }

        [StringLength(256)]
        public string? Title { get; init; }

        [MaxLength(32)]
        public Guid[]? ParticipantIds { get; init; }
    }
    public sealed class AddMessageRequest
    {
        [NotEmptyGuid]
        public Guid FromPersonaId { get; init; }

        [NotEmptyGuid]
        public Guid? ToPersonaId { get; init; }

        public ChatRole Role { get; init; }

        [Required, StringLength(4000, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Content must contain non-whitespace characters.")]
        public string Content { get; init; } = string.Empty;

        [StringLength(128)]
        public string? Metatype { get; init; }

        public AddMessageRequest() { }

        public AddMessageRequest(Guid fromPersonaId, Guid? toPersonaId, ChatRole role, string content, string? metatype = null)
        {
            FromPersonaId = fromPersonaId;
            ToPersonaId = toPersonaId;
            Role = role;
            Content = content;
            Metatype = metatype;
        }
    }
    public record ConversationListItem(Guid Id, string? Title, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, Guid? ProviderId, Guid? ModelId);
    public sealed class AddVersionRequest
    {
        [Required, StringLength(4000, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Content must contain non-whitespace characters.")]
        public string Content { get; init; } = string.Empty;

        public AddVersionRequest() { }
        public AddVersionRequest(string content)
        {
            Content = content;
        }
    }

    public sealed class ConversationSettingsRequest
    {
        public Guid? ProviderId { get; init; }
        public Guid? ModelId { get; init; }
    }

    public sealed record ConversationSettingsResponse(Guid? ProviderId, Guid? ModelId);

    public sealed class SetActiveVersionRequest
    {
        [Range(0, int.MaxValue)]
        public int Index { get; init; }
    }

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

        var conversations = await q
            .OrderByDescending(c => c.UpdatedAtUtc ?? c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var items = conversations
            .Select(c =>
            {
                var provider = TryReadMetadataGuid(c.Metadata, "providerId");
                var model = TryReadMetadataGuid(c.Metadata, "modelId");
                return new ConversationListItem(c.Id, c.Title, c.CreatedAtUtc, c.UpdatedAtUtc, provider, model);
            })
            .ToList();

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

        Guid? providerId = TryReadMetadataGuid(conv.Metadata, "providerId");
        Guid? modelId = TryReadMetadataGuid(conv.Metadata, "modelId");

        if (!providerId.HasValue && conv.Agent?.ClientProfile is not null)
        {
            providerId = conv.Agent.ClientProfile.ProviderId;
            modelId = conv.Agent.ClientProfile.ModelId ?? modelId;
        }

        if (!providerId.HasValue || !modelId.HasValue)
        {
            var defaults = await ResolveDefaultLlmAsync(cancellationToken).ConfigureAwait(false);
            providerId ??= defaults.providerId;
            modelId ??= defaults.modelId;
        }

        return new ConversationSettingsResponse(providerId, modelId);
    }

    [HttpPatch("{id:guid}/settings")]
    public async Task<IActionResult> UpdateSettings(Guid id, [FromBody] ConversationSettingsRequest req, CancellationToken cancellationToken = default)
    {
        var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);
        if (conv is null)
        {
            return NotFound();
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
        var conv = new Conversation
        {
            Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            AgentId = req.AgentId,
            Metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        };

        // Seed provider/model from agent's client profile if available
        var agentProfile = await _db.Agents
            .AsNoTracking()
            .Include(a => a.ClientProfile)
            .Where(a => a.Id == req.AgentId)
            .Select(a => a.ClientProfile)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (agentProfile is not null)
        {
            conv.Metadata["providerId"] = agentProfile.ProviderId;
            if (agentProfile.ModelId.HasValue)
            {
                conv.Metadata["modelId"] = agentProfile.ModelId.Value;
            }
        }
        else
        {
            var defaults = await ResolveDefaultLlmAsync(cancellationToken).ConfigureAwait(false);
            if (defaults.providerId.HasValue)
            {
                conv.Metadata["providerId"] = defaults.providerId.Value;
            }
            if (defaults.modelId.HasValue)
            {
                conv.Metadata["modelId"] = defaults.modelId.Value;
            }
        }

        _db.Conversations.Add(conv);
        await _db.SaveChangesAsync(cancellationToken);
        // participants (optional)
        var participants = (req.ParticipantIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
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
        if (!await _db.Conversations.AnyAsync(c => c.Id == id, cancellationToken)) return NotFound(ApiErrorResponse.Create("conversation_not_found", "Conversation not found."));
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var userId)) return Unauthorized();

        if (req.Role == ChatRole.User)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null || user.PrimaryPersonaId == null || user.PrimaryPersonaId.Value != req.FromPersonaId)
            {
                return BadRequest(ApiErrorResponse.Create("user_persona_mismatch", "User-authored messages must use the user's primary persona."));
            }
        }
        var content = req.Content.Trim();
        var metatype = string.IsNullOrWhiteSpace(req.Metatype) ? null : req.Metatype.Trim();
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
            Content = content,
            Timestamp = DateTime.UtcNow,
            CreatedByUserId = userId,
            Metatype = metatype
        };
        _db.ConversationMessages.Add(msg);
        await _db.SaveChangesAsync(cancellationToken);
        // Initialize version 0
        var versionEntity = new ConversationMessageVersion
        {
            ConversationMessageId = msg.Id,
            VersionIndex = 0,
            Content = content,
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
        if (msg == null) return NotFound(ApiErrorResponse.Create("conversation_message_not_found", "Conversation message not found."));
        var maxIndex = await _db.ConversationMessageVersions.Where(v => v.ConversationMessageId == messageId).Select(v => (int?)v.VersionIndex).MaxAsync(cancellationToken) ?? -1;
        var next = maxIndex + 1;
        var content = req.Content.Trim();
        var v = new ConversationMessageVersion
        {
            ConversationMessageId = messageId,
            VersionIndex = next,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.ConversationMessageVersions.Add(v);
        msg.ActiveVersionIndex = next;
        msg.Content = content;
        await _db.SaveChangesAsync(cancellationToken);
        await _hub.Clients.Group(conversationId.ToString()).SendAsync("AssistantMessageVersionAppended", new { ConversationId = conversationId, MessageId = messageId, Content = v.Content, VersionIndex = next }, cancellationToken);
        return Ok(new { msg.Id, VersionIndex = next });
    }

    [HttpPatch("{conversationId:guid}/messages/{messageId:guid}/active-version")]
    public async Task<IActionResult> SetActiveVersion(Guid conversationId, Guid messageId, [FromBody] SetActiveVersionRequest req, CancellationToken cancellationToken = default)
    {
        var msg = await _db.ConversationMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken);
        if (msg == null) return NotFound(ApiErrorResponse.Create("conversation_message_not_found", "Conversation message not found."));
        var target = await _db.ConversationMessageVersions.AsNoTracking().FirstOrDefaultAsync(v => v.ConversationMessageId == messageId && v.VersionIndex == req.Index, cancellationToken);
        if (target == null) return BadRequest(ApiErrorResponse.Create("version_not_found", "Version index not found."));
        msg.ActiveVersionIndex = req.Index;
        msg.Content = target.Content;
        await _db.SaveChangesAsync(cancellationToken);
        await _hub.Clients.Group(conversationId.ToString()).SendAsync("AssistantActiveVersionChanged", new { ConversationId = conversationId, MessageId = messageId, VersionIndex = req.Index }, cancellationToken);
        return Ok(new { msg.Id, msg.ActiveVersionIndex });
    }

    private static Guid? TryReadMetadataGuid(Dictionary<string, object?>? metadata, string key)
    {
        if (metadata is null)
        {
            return null;
        }

        if (metadata.TryGetValue(key, out var value) && value is not null)
        {
            var str = value.ToString();
            if (Guid.TryParse(str, out var guid))
            {
                return guid;
            }
        }

        return null;
    }

    private async Task<(Guid? providerId, Guid? modelId)> ResolveDefaultLlmAsync(CancellationToken cancellationToken)
    {
        var defaultRow = await _db.LlmGlobalDefaults
            .AsNoTracking()
            .Include(d => d.Model)
            .ThenInclude(m => m.Provider)
            .Where(d => d.IsActive)
            .OrderBy(d => d.Priority)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (defaultRow != null && defaultRow.Model is not null && defaultRow.Model.Provider is not null && defaultRow.Model.Provider.IsActive)
        {
            return (defaultRow.Model.ProviderId, defaultRow.ModelId);
        }

        // Fallback: prefer Gemini/Google providers and models containing flash/2.5/2.0
        var providers = await _db.Providers
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Guid? preferredProviderId = providers
            .FirstOrDefault(p => p.Name.Contains("Gemini", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Google", StringComparison.OrdinalIgnoreCase))
            ?.Id ?? providers.FirstOrDefault()?.Id;

        Guid? preferredModelId = null;
        if (preferredProviderId.HasValue)
        {
            var models = await _db.Models
                .AsNoTracking()
                .Where(m => m.ProviderId == preferredProviderId.Value)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            preferredModelId = models
                .FirstOrDefault(m =>
                    m.Name.Contains("flash", StringComparison.OrdinalIgnoreCase) ||
                    m.Name.Contains("2.5", StringComparison.OrdinalIgnoreCase) ||
                    m.Name.Contains("2.0", StringComparison.OrdinalIgnoreCase))
                ?.Id ?? models.FirstOrDefault()?.Id;
        }

        return (preferredProviderId, preferredModelId);
    }
}
