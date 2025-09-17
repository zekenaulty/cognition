using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Cognition.Clients.Agents;
using Cognition.Clients.Tools;
using Cognition.Contracts.Events;
using Rebus.Bus;

using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Microsoft.AspNetCore.SignalR;

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
        Guid PersonaId,
        Guid ProviderId,
        Guid? ModelId,
        string Input,
        bool RolePlay = false);

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest req)
    {
        try
        {
            var reply = await _agents.AskAsync(req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay, HttpContext.RequestAborted);
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
            var reply = await _agents.AskWithToolsAsync(req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay, HttpContext.RequestAborted);
            return Ok(new { reply });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "ask_with_tools_failed", message = ex.Message });
        }
    }

    public record ChatRequest(
        Guid ConversationId,
        Guid PersonaId,
        Guid ProviderId,
        Guid? ModelId,
        string Input,
        bool RolePlay = false);

/*

    public record ChatRequest(
        Guid ConversationId,
        Guid PersonaId,
        Guid ProviderId,
        Guid? ModelId,
        string Input,
        bool RolePlay = false);

    [HttpPost("ask-chat")]
    public async Task<IActionResult> AskChat([FromBody] ChatRequest req)
    {
        try
        {
            var reply = await _agents.ChatAsync(req.ConversationId, req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay, HttpContext.RequestAborted);
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
*/
    [HttpPost("ask-chat")]
    public async Task<IActionResult> AskChat([FromBody] ChatRequest req)
    {
        // Persist user message
        var message = new ConversationMessage
        {
            ConversationId = req.ConversationId,
            FromPersonaId = req.PersonaId,
            Content = req.Input,
            Role = Data.Relational.Modules.Common.ChatRole.User,
            CreatedAtUtc = DateTime.UtcNow,
            Metatype = "UserMessage"
        };
        _db.ConversationMessages.Add(message);
        await _db.SaveChangesAsync();

        // Emit UserMessageAppended
        var userMsgEvt = new UserMessageAppended(req.ConversationId, req.PersonaId, req.Input, req.RolePlay);
        await _bus.Publish(userMsgEvt);

        var assistantContent = await _agents.ChatAsync(req.ConversationId, req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay, HttpContext.RequestAborted);
        if (string.IsNullOrWhiteSpace(assistantContent))
        {
            assistantContent = "(No reply...)";
        }
        var assistantMessage = new ConversationMessage
        {
            ConversationId = req.ConversationId,
            FromPersonaId = req.PersonaId,
            Content = assistantContent,
            Role = Data.Relational.Modules.Common.ChatRole.Assistant,
            CreatedAtUtc = DateTime.UtcNow,
            Metatype = "TextResponse"
        };


        _db.ConversationMessages.Add(assistantMessage);
        await _db.SaveChangesAsync();
        var assistantEvt = new AssistantMessageAppended(req.ConversationId, req.PersonaId, assistantContent);
        await _bus.Publish(assistantEvt);

        // Return 202 Accepted
        return Accepted(new { 
            ok = true, 
            correlationId = message.Id,
            content = assistantContent,
        });
    }

    public record AskWithPlanRequest(
        Guid ConversationId,
        Guid PersonaId,
        Guid ProviderId,
        Guid? ModelId,
        string Input,
        int MinSteps,
        int MaxSteps,
        bool RolePlay = false);

    [HttpPost("ask-with-plan")]
    public async Task<IActionResult> AskWithPlan([FromBody] AskWithPlanRequest req)
    {
        // Persist user message
        var message = new ConversationMessage
        {
            ConversationId = req.ConversationId,
            FromPersonaId = req.PersonaId,
            Content = req.Input,
            Role = Data.Relational.Modules.Common.ChatRole.User,
            CreatedAtUtc = DateTime.UtcNow,
            Metatype = "UserMessage"
        };
        _db.ConversationMessages.Add(message);
        await _db.SaveChangesAsync();

        // Publish UserMessageAppended and PlanRequested events
        await _bus.Publish(new UserMessageAppended(req.ConversationId, req.PersonaId, req.Input, req.RolePlay));
        await _bus.Publish(new PlanRequested(req.ConversationId, req.PersonaId, req.Input));

        // Return 202 Accepted
        return Accepted(new { ok = true, correlationId = message.Id });
    }
}
