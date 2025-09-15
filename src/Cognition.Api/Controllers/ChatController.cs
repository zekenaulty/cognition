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
        var reply = await _agents.AskAsync(req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay);
        return Ok(new { reply });
    }

    [HttpPost("ask-with-tools")]
    public async Task<IActionResult> AskWithTools([FromBody] AskRequest req)
    {
        var reply = await _agents.AskWithToolsAsync(req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay);
        return Ok(new { reply });
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
        var reply = await _agents.ChatAsync(req.ConversationId, req.PersonaId, req.ProviderId, req.ModelId, req.Input, req.RolePlay);
        return Ok(new { reply });
    }
}
