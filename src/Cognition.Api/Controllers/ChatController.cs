using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    public record AskRequest(
        Guid AgentId,
        Guid ProviderId,
        Guid? ModelId,
        string Input);

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest req)
    {
        try
        {
            var personaId = await _db.Agents.AsNoTracking()
                .Where(a => a.Id == req.AgentId)
                .Select(a => (Guid?)a.PersonaId)
                .FirstOrDefaultAsync();
            if (!personaId.HasValue) return NotFound(new { code = "agent_not_found", message = "Agent not found" });
            var reply = await _agents.AskAsync(personaId.Value, req.ProviderId, req.ModelId, req.Input, HttpContext.RequestAborted);
            return Ok(new { reply });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "ask_failed", message = ex.Message });
        }
    }

    [HttpPost("ask-with-tools")]
    public async Task<IActionResult> AskWithTools([FromBody] AskRequest req)
    {
        try
        {
            var personaId = await _db.Agents.AsNoTracking()
                .Where(a => a.Id == req.AgentId)
                .Select(a => (Guid?)a.PersonaId)
                .FirstOrDefaultAsync();
            if (!personaId.HasValue) return NotFound(new { code = "agent_not_found", message = "Agent not found" });
            var reply = await _agents.AskWithToolsAsync(personaId.Value, req.ProviderId, req.ModelId, req.Input, HttpContext.RequestAborted);
            return Ok(new { reply });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "ask_with_tools_failed", message = ex.Message });
        }
    }

    public record ChatRequest(
        Guid ConversationId,
        Guid ProviderId,
        Guid? ModelId,
        string Input);

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
            return NotFound(new { code = "conversation_not_found", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "chat_failed", message = ex.Message });
        }
    }

    [HttpPost("ask-chat-v2")]
    public async Task<IActionResult> AskChatV2([FromBody] ChatV2Request req)
    {
        try
        {
            var personaId = await _db.Conversations.AsNoTracking()
                .Where(c => c.Id == req.ConversationId)
                .Join(_db.Agents.AsNoTracking(), c => c.AgentId, a => a.Id, (c, a) => a.PersonaId)
                .FirstOrDefaultAsync();
            if (personaId == Guid.Empty)
                return NotFound(new { code = "conversation_or_agent_not_found", message = "Conversation/Agent not found" });

            var assistantContent = await _agents.ChatAsync(req.ConversationId, personaId, req.ProviderId, req.ModelId, req.Input, HttpContext.RequestAborted);
            if (string.IsNullOrWhiteSpace(assistantContent.Reply))
            {
                assistantContent.Reply = "(No reply...)";
            }
            return Accepted(new { ok = true, content = assistantContent });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { code = "conversation_not_found", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "ask_chat_v2_failed", message = ex.Message });
        }
    }
*/
    [HttpPost("ask-chat")]
    public async Task<IActionResult> AskChat([FromBody] ChatRequest req)
    {
        // Resolve personaId/agentId for this conversation
        var agentPersona = await _db.Conversations.AsNoTracking()
            .Where(c => c.Id == req.ConversationId)
            .Join(_db.Agents.AsNoTracking(), c => c.AgentId, a => a.Id, (c, a) => new { a.Id, a.PersonaId })
            .FirstOrDefaultAsync();
        if (agentPersona == null) return NotFound(new { code = "conversation_or_agent_not_found", message = "Conversation/Agent not found" });
        var fromAgentId = agentPersona.Id;
        var fromPersonaId = agentPersona.PersonaId;
        var message = new ConversationMessage
        {
            ConversationId = req.ConversationId,
            FromPersonaId = fromPersonaId,
            FromAgentId = fromAgentId,
            Content = req.Input,
            Role = Data.Relational.Modules.Common.ChatRole.User,
            CreatedAtUtc = DateTime.UtcNow,
            Metatype = "UserMessage"
        };
        _db.ConversationMessages.Add(message);
        await _db.SaveChangesAsync();

        // Emit UserMessageAppended
        var userMsgEvt = new UserMessageAppended(req.ConversationId, fromPersonaId, req.Input);
        await _bus.Publish(userMsgEvt);

        var assistantContent = await _agents.ChatAsync(req.ConversationId, fromPersonaId, req.ProviderId, req.ModelId, req.Input, HttpContext.RequestAborted);
        if (string.IsNullOrWhiteSpace(assistantContent.Reply))
        {
            assistantContent.Reply = "(No reply...)";
        }
        var assistantAgentId = fromAgentId;
        var assistantMessage = new ConversationMessage
        {
            ConversationId = req.ConversationId,
            FromPersonaId = fromPersonaId,
            FromAgentId = assistantAgentId,
            Content = assistantContent.Reply,
            Role = Data.Relational.Modules.Common.ChatRole.Assistant,
            CreatedAtUtc = DateTime.UtcNow,
            Metatype = "TextResponse"
        };


        _db.ConversationMessages.Add(assistantMessage);
        await _db.SaveChangesAsync();
        var assistantEvt = new AssistantMessageAppended(req.ConversationId, fromPersonaId, assistantContent.Reply);
        await _bus.Publish(assistantEvt);

        // Touch conversation updated timestamp
        try
        {
            var conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == req.ConversationId);
            if (conv != null)
            {
                conv.UpdatedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
        catch { }

        // If conversation has a title now (AgentService may have set it), broadcast update via hub
        try
        {
            var convo = await _db.Conversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == req.ConversationId);
            if (convo != null && !string.IsNullOrWhiteSpace(convo.Title))
            {
                await _hubContext.Clients.Group(req.ConversationId.ToString()).SendAsync("ConversationUpdated", new { ConversationId = req.ConversationId, Title = convo.Title });
            }
        }
        catch { }

        // Return 202 Accepted
        return Accepted(new { 
            ok = true, 
            correlationId = message.Id,
            content = assistantContent,
        });
    }

    public record AskWithPlanRequest(
        Guid ConversationId,
        Guid FictionPlanId,
        string? BranchSlug,
        Guid ProviderId,
        Guid? ModelId,
        string Input,
        int MinSteps,
        int MaxSteps);

    public record RememberRequest(
        Guid? ConversationId,
        Guid? AgentId,
        string Content,
        Dictionary<string, object?>? Metadata);

    [HttpPost("ask-with-plan")]
    public async Task<IActionResult> AskWithPlan([FromBody] AskWithPlanRequest req)
    {
        var conversation = await _db.Conversations
            .Include(c => c.Agent)
            .FirstOrDefaultAsync(c => c.Id == req.ConversationId);
        if (conversation is null)
        {
            return NotFound(new { code = "conversation_not_found", message = "Conversation not found" });
        }

        var branchSlug = string.IsNullOrWhiteSpace(req.BranchSlug) ? "main" : req.BranchSlug!.Trim();

        var fictionPlan = await _db.Set<FictionPlan>()
            .Include(p => p.FictionProject)
            .FirstOrDefaultAsync(p => p.Id == req.FictionPlanId);
        if (fictionPlan is null)
        {
            return NotFound(new { code = "fiction_plan_not_found", message = "Fiction plan not found" });
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
        await _db.SaveChangesAsync();

        await _bus.Publish(new UserMessageAppended(req.ConversationId, planFromPersonaId, req.Input));

        var metadata = new Dictionary<string, object?>
        {
            ["conversationMessageId"] = message.Id,
            ["source"] = "ask-with-plan"
        };

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
    public async Task<IActionResult> Remember([FromServices] Cognition.Clients.Retrieval.IRetrievalService retrieval, [FromBody] RememberRequest req)
    {
        Guid? agentId = req.AgentId;
        if (!agentId.HasValue && req.ConversationId.HasValue)
        {
            agentId = await _db.Conversations.AsNoTracking()
                .Where(c => c.Id == req.ConversationId.Value)
                .Select(c => (Guid?)c.AgentId)
                .FirstOrDefaultAsync();
        }
        if (!agentId.HasValue) return BadRequest(new { code = "agent_missing", message = "Provide AgentId or a ConversationId bound to an Agent" });

        var scope = new Cognition.Contracts.ScopeToken(null, null, null, agentId.Value, null, null, null);
        var ok = await retrieval.WriteAsync(scope, req.Content, req.Metadata ?? new(), HttpContext.RequestAborted);
        return Ok(new { ok });
    }
}
