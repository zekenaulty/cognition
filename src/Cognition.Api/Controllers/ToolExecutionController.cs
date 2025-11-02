using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools;
using Microsoft.AspNetCore.Authorization;
using Cognition.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOrHigher)]
[ApiController]
[Route("api/tools")] 
public class ToolExecutionController : ControllerBase
{
    private readonly IToolDispatcher _dispatcher;
    private readonly IServiceProvider _sp;
    public ToolExecutionController(IToolDispatcher dispatcher, IServiceProvider sp)
    { _dispatcher = dispatcher; _sp = sp; }

    public record ExecRequest(IDictionary<string, object?> Args, Guid? AgentId, Guid? ConversationId, Guid? PersonaId);

    [HttpPost("{id:guid}/execute")]
    public async Task<IActionResult> Execute(Guid id, [FromBody] ExecRequest req, CancellationToken cancellationToken = default)
    {
        var ctx = new ToolContext(req.AgentId, req.ConversationId, req.PersonaId, _sp, cancellationToken);
        var (ok, result, error) = await _dispatcher.ExecuteAsync(id, ctx, req.Args ?? new Dictionary<string, object?>(), log: true);
        if (ok) return Ok(new { result });
        return BadRequest(new { code = "tool_execution_failed", message = error ?? "Tool execution failed." });
    }
}
