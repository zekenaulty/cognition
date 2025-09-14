using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Cognition.Clients.Agents;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IAgentService _agents;
    public ChatController(IAgentService agents) => _agents = agents;

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
}
