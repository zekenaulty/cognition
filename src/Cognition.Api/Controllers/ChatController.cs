using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Cognition.Clients.Agents;
using Cognition.Clients.Tools;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IAgentService _agents;
    public ChatController(IAgentService agents)
    { _agents = agents; }

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
        try
        {
            var reply = await _agents.AskWithPlanAsync(req.ConversationId, req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.MinSteps, req.MaxSteps, req.RolePlay, HttpContext.RequestAborted);
            return Ok(new { reply });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { code = "conversation_not_found", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "ask_with_plan_failed", message = ex.Message });
        }
    }
}
