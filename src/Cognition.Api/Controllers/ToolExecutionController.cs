using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools;
using Microsoft.AspNetCore.Authorization;
using Cognition.Api.Infrastructure.Security;
using Cognition.Api.Infrastructure.ErrorHandling;
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

    public record ExecRequest(IDictionary<string, object?>? Args, Guid? AgentId, Guid? ConversationId, Guid? PersonaId, Guid? FictionPlanId);

    [HttpPost("{id:guid}/execute")]
    public async Task<IActionResult> Execute(Guid id, [FromBody] ExecRequest req, CancellationToken cancellationToken = default)
    {
        var args = req.Args is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(req.Args, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, object?>? metadata = null;
        if (req.FictionPlanId.HasValue)
        {
            metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["planId"] = req.FictionPlanId.Value
            };

            if (!args.ContainsKey("planId"))
            {
                args["planId"] = req.FictionPlanId.Value;
            }
        }

        var ctx = new ToolContext(req.AgentId, req.ConversationId, req.PersonaId, _sp, cancellationToken, metadata);
        var (ok, result, error) = await _dispatcher.ExecuteAsync(id, ctx, args, log: true);
        if (ok) return Ok(new { result });
        return BadRequest(ApiErrorResponse.Create("tool_execution_failed", error ?? "Tool execution failed."));
    }
}
