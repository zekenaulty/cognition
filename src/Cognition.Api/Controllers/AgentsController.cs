using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/agents")] 
public class AgentsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public AgentsController(CognitionDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.Agents.AsNoTracking()
            .Select(a => new { a.Id, a.PersonaId, a.ClientProfileId, a.RolePlay, a.Prefix, a.Suffix })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var a = await _db.Agents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        var bindings = await _db.AgentToolBindings.AsNoTracking()
            .Where(b => b.ScopeType.ToLower() == "agent" && b.ScopeId == id)
            .Join(_db.Tools.AsNoTracking(), b => b.ToolId, t => t.Id, (b, t) => new { b.Id, b.Enabled, b.ScopeType, b.ScopeId, ToolId = t.Id, ToolName = t.Name })
            .ToListAsync();
        return Ok(new { a.Id, a.PersonaId, a.ClientProfileId, a.RolePlay, a.Prefix, a.Suffix, ToolBindings = bindings });
    }

[Swashbuckle.AspNetCore.Annotations.SwaggerOperation(
    Summary = "Set an Agent's ClientProfile",
    Description = "Set the LLM ClientProfile for the agent. Body should be the GUID of the ClientProfile.")]
    [HttpPatch("{id:guid}/client-profile")]
    public async Task<IActionResult> SetClientProfile(Guid id, [FromBody] Guid clientProfileId)
    {
        var a = await _db.Agents.FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        if (!await _db.ClientProfiles.AnyAsync(cp => cp.Id == clientProfileId)) return BadRequest("ClientProfile not found");
        a.ClientProfileId = clientProfileId;
        a.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
