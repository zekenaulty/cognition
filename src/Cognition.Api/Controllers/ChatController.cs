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
    private readonly IToolDispatcher _tools;
    private readonly IServiceProvider _sp;
    public ChatController(IAgentService agents, IToolDispatcher tools, IServiceProvider sp)
    { _agents = agents; _tools = tools; _sp = sp; }

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
        if (_agents is not AgentService impl)
            return BadRequest("AgentService implementation not available.");
        var reply = await impl.AskWithToolsAsync(req.PersonaId, req.ProviderId, req.ModelId, req.Input, _tools, _sp, req.RolePlay);
        return Ok(new { reply });
    }
}
