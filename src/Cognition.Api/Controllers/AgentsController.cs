using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Security;
using Cognition.Api.Infrastructure.ErrorHandling;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOrHigher)]
[ApiController]
[Route("api/agents")]
public class AgentsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public AgentsController(CognitionDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default)
    {
        var query = _db.Agents.AsNoTracking().AsQueryable();

        if (!IsAdmin())
        {
            var callerId = GetCallerUserId();
            if (!callerId.HasValue) return Forbid();
            var personaIds = await ResolvePersonaIdsForCallerAsync(callerId.Value, cancellationToken);
            if (personaIds.Count == 0)
            {
                return Ok(Array.Empty<object>());
            }
            query = query.Where(a => personaIds.Contains(a.PersonaId));
        }

        var items = await query
            .Select(a => new { a.Id, a.PersonaId, a.ClientProfileId, a.RolePlay, a.Prefix, a.Suffix })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var a = await _db.Agents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (a == null) return NotFound(ApiErrorResponse.Create("agent_not_found", "Agent not found."));

        if (!IsAdmin())
        {
            var callerId = GetCallerUserId();
            if (!callerId.HasValue) return Forbid();
            var personaIds = await ResolvePersonaIdsForCallerAsync(callerId.Value, cancellationToken);
            if (!personaIds.Contains(a.PersonaId)) return Forbid();
        }

        var bindings = await _db.AgentToolBindings.AsNoTracking()
            .Where(b => b.ScopeType.ToLower() == "agent" && b.ScopeId == id)
            .Join(_db.Tools.AsNoTracking(), b => b.ToolId, t => t.Id, (b, t) => new { b.Id, b.Enabled, b.ScopeType, b.ScopeId, ToolId = t.Id, ToolName = t.Name })
            .ToListAsync(cancellationToken);
        return Ok(new { a.Id, a.PersonaId, a.ClientProfileId, a.RolePlay, a.Prefix, a.Suffix, ToolBindings = bindings });
    }

[Swashbuckle.AspNetCore.Annotations.SwaggerOperation(
    Summary = "Set an Agent's ClientProfile",
    Description = "Set the LLM ClientProfile for the agent. Body should be the GUID of the ClientProfile.")]
    [HttpPatch("{id:guid}/client-profile")]
    public async Task<IActionResult> SetClientProfile(Guid id, [FromBody] Guid clientProfileId, CancellationToken cancellationToken = default)
    {
        var a = await _db.Agents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (a == null) return NotFound(ApiErrorResponse.Create("agent_not_found", "Agent not found."));
        if (!IsAdmin())
        {
            var callerId = GetCallerUserId();
            if (!callerId.HasValue) return Forbid();
            var personaIds = await ResolvePersonaIdsForCallerAsync(callerId.Value, cancellationToken);
            if (!personaIds.Contains(a.PersonaId)) return Forbid();
        }
        if (!await _db.ClientProfiles.AnyAsync(cp => cp.Id == clientProfileId, cancellationToken))
            return BadRequest(ApiErrorResponse.Create("client_profile_not_found", "Client profile not found."));
        a.ClientProfileId = clientProfileId;
        a.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool IsAdmin() => User.IsInRole(nameof(UserRole.Administrator));

    private Guid? GetCallerUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var caller) ? caller : null;
    }

    private async Task<HashSet<Guid>> ResolvePersonaIdsForCallerAsync(Guid callerId, CancellationToken ct)
    {
        var personaIds = await _db.UserPersonas.AsNoTracking()
            .Where(up => up.UserId == callerId)
            .Select(up => up.PersonaId)
            .ToListAsync(ct);

        var primaryId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == callerId)
            .Select(u => u.PrimaryPersonaId)
            .FirstOrDefaultAsync(ct);

        if (primaryId.HasValue && !personaIds.Contains(primaryId.Value))
        {
            personaIds.Add(primaryId.Value);
        }

        return personaIds.ToHashSet();
    }
}
