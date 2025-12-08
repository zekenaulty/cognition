using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools;
using Microsoft.AspNetCore.Authorization;
using Cognition.Api.Infrastructure.Security;
using Cognition.Api.Infrastructure.ErrorHandling;
using Microsoft.AspNetCore.Mvc;
using Cognition.Api.Infrastructure.Validation;
using Cognition.Data.Relational;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOrHigher)]
[ApiController]
[Route("api/tools")] 
public class ToolExecutionController : ControllerBase
{
    private readonly IToolDispatcher _dispatcher;
    private readonly IServiceProvider _sp;
    private readonly CognitionDbContext _db;
    public ToolExecutionController(IToolDispatcher dispatcher, IServiceProvider sp, CognitionDbContext db)
    { _dispatcher = dispatcher; _sp = sp; _db = db; }

    public sealed class ExecRequest
    {
        public IDictionary<string, object?>? Args { get; init; }

        [NotEmptyGuid]
        public Guid? AgentId { get; init; }

        public Guid? ConversationId { get; init; }
        public Guid? PersonaId { get; init; }
        public Guid? FictionPlanId { get; init; }
    }

    [HttpPost("{id:guid}/execute")]
    public async Task<IActionResult> Execute(Guid id, [FromBody] ExecRequest req, CancellationToken cancellationToken = default)
    {
        var tool = await _db.Tools.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tool is null) return NotFound(ApiErrorResponse.Create("tool_not_found", "Tool not found."));
        if (!tool.IsActive) return BadRequest(ApiErrorResponse.Create("tool_inactive", "Tool is inactive."));

        if (!req.AgentId.HasValue)
        {
            return BadRequest(ApiErrorResponse.Create("agent_required", "AgentId is required for tool execution."));
        }

        Guid? personaId = req.PersonaId;
        if (!personaId.HasValue)
        {
            personaId = await _db.Agents.AsNoTracking()
                .Where(a => a.Id == req.AgentId.Value)
                .Select(a => (Guid?)a.PersonaId)
                .FirstOrDefaultAsync(cancellationToken);
        }

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

        // Scope token must include agent; persona optional; conversation optional
        var scope = new Cognition.Contracts.ScopeToken(
            TenantId: null,
            AppId: null,
            PersonaId: personaId,
            AgentId: req.AgentId,
            ConversationId: req.ConversationId,
            PlanId: req.FictionPlanId,
            ProjectId: null,
            WorldId: null);

        // TODO: enforce sandbox policy here (isolation/runtime)
        var ctx = new ToolContext(scope.AgentId, scope.ConversationId, scope.PersonaId, _sp, cancellationToken, metadata);
        var (ok, result, error) = await _dispatcher.ExecuteAsync(id, ctx, args, log: true);
        if (ok) return Ok(new { result });
        return BadRequest(ApiErrorResponse.Create("tool_execution_failed", error ?? "Tool execution failed."));
    }
}
