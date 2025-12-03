using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Cognition.Api.Infrastructure.Security;
using Cognition.Api.Infrastructure.Diagnostics;
using Cognition.Api.Infrastructure.Validation;
using Cognition.Api.Infrastructure.ErrorHandling;
using Microsoft.AspNetCore.Mvc;
using Cognition.Clients.Agents;
using Cognition.Clients.Tools;
using Cognition.Contracts.Events;
using Rebus.Bus;

using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOrHigher)]
[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IAgentService _agents;
    private readonly IBus _bus;
    private readonly CognitionDbContext _db;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<ChatHub> _hubContext;
    public ChatController(IAgentService agents, IBus bus, CognitionDbContext db, Microsoft.AspNetCore.SignalR.IHubContext<ChatHub> hubContext)
    {
        _agents = agents;
        _bus = bus;
        _db = db;
        _hubContext = hubContext;
    }

    public sealed class AskRequest
    {
        [NotEmptyGuid]
        public Guid AgentId { get; init; }

        [NotEmptyGuid]
        public Guid ProviderId { get; init; }

        public Guid? ModelId { get; init; }

        [Required, StringLength(4000, MinimumLength = 1)]
        public string Input { get; init; } = string.Empty;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            var agentExists = await _db.Agents.AsNoTracking()
                .AnyAsync(a => a.Id == req.AgentId, cancellationToken);
            if (!agentExists) return NotFound(ApiErrorResponse.Create("agent_not_found", "Agent not found."));
            var reply = await _agents.AskAsync(req.AgentId, req.ProviderId, req.ModelId, req.Input, cancellationToken);
            return Ok(new { reply });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiErrorResponse.Create("ask_failed", ex.Message));
        }
    }

    [HttpPost("ask-with-tools")]
    public async Task<IActionResult> AskWithTools([FromBody] AskRequest req, CancellationToken cancellationToken = default)
    {
        try
        {
            var agentExists = await _db.Agents.AsNoTracking()
                .AnyAsync(a => a.Id == req.AgentId, cancellationToken);
            if (!agentExists) return NotFound(ApiErrorResponse.Create("agent_not_found", "Agent not found."));
            var reply = await _agents.AskWithToolsAsync(req.AgentId, req.ProviderId, req.ModelId, req.Input, cancellationToken);
            return Ok(new { reply });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiErrorResponse.Create("ask_with_tools_failed", ex.Message));
        }
    }

    public sealed class ChatRequest
    {
        [NotEmptyGuid]
        public Guid ConversationId { get; init; }

        [NotEmptyGuid]
        public Guid ProviderId { get; init; }

        public Guid? ModelId { get; init; }

        [Required, StringLength(4000, MinimumLength = 1)]
        public string Input { get; init; } = string.Empty;
    }

/*

    public record ChatRequest(
        Guid ConversationId,
        Guid PersonaId,
        Guid ProviderId,
        Guid? ModelId,
        string Input);

    [HttpPost("ask-chat")]
    public async Task<IActionResult> AskChat([FromBody] ChatRequest req)
    {
        try
        {
            var reply = await _agents.ChatAsync(req.ConversationId, req.PersonaId, req.ProviderId, req.ModelId, req.Input, HttpContext.RequestAborted);
            return Ok(new { reply });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiErrorResponse.Create("conversation_not_found", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiErrorResponse.Create("chat_failed", ex.Message));
        }
    }

    [HttpPost("ask-chat-v2")]
    public async Task<IActionResult> AskChatV2([FromBody] ChatV2Request req, CancellationToken cancellationToken = default)
    {
        try
        {
            var personaId = await _db.Conversations.AsNoTracking()
                .Where(c => c.Id == req.ConversationId)
                .Join(_db.Agents.AsNoTracking(), c => c.AgentId, a => a.Id, (c, a) => a.PersonaId)
                .FirstOrDefaultAsync(cancellationToken);
            if (personaId == Guid.Empty)
                return NotFound(ApiErrorResponse.Create("conversation_or_agent_not_found", "Conversation/Agent not found."));

            var assistantContent = await _agents.ChatAsync(req.ConversationId, personaId, req.ProviderId, req.ModelId, req.Input, cancellationToken);
            if (string.IsNullOrWhiteSpace(assistantContent.Reply))
            {
                assistantContent.Reply = "(No reply...)";
            }
            return Accepted(new { ok = true, content = assistantContent });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiErrorResponse.Create("conversation_not_found", ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiErrorResponse.Create("ask_chat_v2_failed", ex.Message));
        }
    }
*/
    [HttpPost("ask-chat")]
    public async Task<IActionResult> AskChat([FromBody] ChatRequest req, CancellationToken cancellationToken = default)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Agent)
            .FirstOrDefaultAsync(c => c.Id == req.ConversationId, cancellationToken);
        if (conversation == null || conversation.Agent == null)
        {
            return NotFound(ApiErrorResponse.Create("conversation_or_agent_not_found", "Conversation/Agent not found."));
        }

        var agentId = conversation.AgentId;
        var personaId = conversation.Agent.PersonaId;

        // Publish user message event (DB persistence handled by AgentService.ChatAsync)
        await _bus.Publish(new UserMessageAppended(req.ConversationId, personaId, req.Input));

        var (reply, messageId) = await _agents.ChatAsync(req.ConversationId, agentId, req.ProviderId, req.ModelId, req.Input, cancellationToken);
        var safeReply = string.IsNullOrWhiteSpace(reply) ? "(No reply...)" : reply;

        await _bus.Publish(new AssistantMessageAppended(req.ConversationId, personaId, safeReply));

        // If conversation has a title now (AgentService may have set it), broadcast update via hub
        try
        {
            var convo = await _db.Conversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == req.ConversationId, cancellationToken);
            if (convo != null && !string.IsNullOrWhiteSpace(convo.Title))
            {
                await _hubContext.Clients.Group(req.ConversationId.ToString()).SendAsync("ConversationUpdated", new { ConversationId = req.ConversationId, Title = convo.Title }, cancellationToken);
            }
        }
        catch { }

        return Accepted(new
        {
            ok = true,
            correlationId = messageId,
            content = safeReply,
        });
    }

    public record AskWithPlanRequest(
        [property: NotEmptyGuid] Guid ConversationId,
        [property: NotEmptyGuid] Guid FictionPlanId,
        [property: StringLength(64)] string? BranchSlug,
        [property: NotEmptyGuid] Guid ProviderId,
        Guid? ModelId,
        [property: Required, StringLength(4000, MinimumLength = 1)] string Input,
        [property: Range(0, 100)] int MinSteps,
        [property: Range(0, 100)] int MaxSteps);

    public record RememberRequest(
        Guid? ConversationId,
        Guid? AgentId,
        Guid? FictionPlanId,
        [property: Required, StringLength(4000, MinimumLength = 1)] string Content,
        Dictionary<string, object?>? Metadata);

    [HttpPost("ask-with-plan")]
    public async Task<IActionResult> AskWithPlan([FromBody] AskWithPlanRequest req, CancellationToken cancellationToken = default)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Agent)
            .FirstOrDefaultAsync(c => c.Id == req.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return NotFound(ApiErrorResponse.Create("conversation_not_found", "Conversation not found."));
        }

        var branchSlug = string.IsNullOrWhiteSpace(req.BranchSlug) ? "main" : req.BranchSlug!.Trim();

        var fictionPlan = await _db.Set<FictionPlan>()
            .Include(p => p.FictionProject)
            .FirstOrDefaultAsync(p => p.Id == req.FictionPlanId, cancellationToken);
        if (fictionPlan is null)
        {
            return NotFound(ApiErrorResponse.Create("fiction_plan_not_found", "Fiction plan not found."));
        }

        var planFromAgentId = conversation.AgentId;
        var planFromPersonaId = conversation.Agent.PersonaId;

        conversation.Metadata ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        conversation.Metadata["fictionPlanId"] = req.FictionPlanId;
        conversation.Metadata["fictionBranchSlug"] = branchSlug;
        conversation.Metadata["plannerProviderId"] = req.ProviderId;
        if (req.ModelId.HasValue)
        {
            conversation.Metadata["plannerModelId"] = req.ModelId.Value;
        }
        else if (conversation.Metadata.ContainsKey("plannerModelId"))
        {
            conversation.Metadata.Remove("plannerModelId");
        }

        var message = new ConversationMessage
        {
            ConversationId = req.ConversationId,
            FromPersonaId = planFromPersonaId,
            FromAgentId = planFromAgentId,
            Content = req.Input,
            Role = Data.Relational.Modules.Common.ChatRole.User,
            CreatedAtUtc = DateTime.UtcNow,
            Metatype = "UserMessage"
        };
        _db.ConversationMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        await _bus.Publish(new UserMessageAppended(req.ConversationId, planFromPersonaId, req.Input));

        var correlationId = HttpContext.GetCorrelationId();

        var metadata = new Dictionary<string, object?>
        {
            ["conversationMessageId"] = message.Id,
            ["source"] = "ask-with-plan"
        };

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            metadata["correlationId"] = correlationId;
        }

        await _bus.Publish(new PlanRequested(
            req.ConversationId,
            planFromAgentId,
            planFromPersonaId,
            req.ProviderId,
            req.ModelId,
            req.Input,
            req.MinSteps,
            req.MaxSteps,
            req.FictionPlanId,
            branchSlug,
            metadata));

        return Accepted(new { ok = true, correlationId = message.Id });
    }

    [HttpPost("remember")]
    public async Task<IActionResult> Remember([FromServices] Cognition.Clients.Retrieval.IRetrievalService retrieval, [FromBody] RememberRequest req, CancellationToken cancellationToken = default)
    {
        Guid? agentId = req.AgentId;
        if (!agentId.HasValue && req.ConversationId.HasValue)
        {
            agentId = await _db.Conversations.AsNoTracking()
                .Where(c => c.Id == req.ConversationId.Value)
                .Select(c => (Guid?)c.AgentId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        if (!agentId.HasValue) return BadRequest(ApiErrorResponse.Create("agent_missing", "Provide AgentId or a ConversationId bound to an Agent."));

        var fictionPlanId = req.FictionPlanId;

        if (!fictionPlanId.HasValue && req.ConversationId.HasValue)
        {
            var conversationPlanId = await _db.ConversationPlans.AsNoTracking()
                .Where(cp => cp.ConversationId == req.ConversationId.Value)
                .OrderByDescending(cp => cp.CreatedAt)
                .Select(cp => (Guid?)cp.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (conversationPlanId.HasValue)
            {
                fictionPlanId = await _db.Set<FictionPlan>()
                    .AsNoTracking()
                    .Where(fp => fp.CurrentConversationPlanId == conversationPlanId.Value)
                    .Select(fp => (Guid?)fp.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        var scope = new Cognition.Contracts.ScopeToken(
            TenantId: null,
            AppId: null,
            PersonaId: null,
            AgentId: agentId.Value,
            ConversationId: req.ConversationId,
            PlanId: fictionPlanId,
            ProjectId: null,
            WorldId: null);
        var ok = await retrieval.WriteAsync(scope, req.Content, req.Metadata ?? new(), cancellationToken);
        return Ok(new { ok });
    }
}






