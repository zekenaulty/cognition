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
