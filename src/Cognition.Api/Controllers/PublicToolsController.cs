using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Security;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.UserOrHigher)]
[ApiController]
[Route("api/public/tools")]
public class PublicToolsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public PublicToolsController(CognitionDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> ListActiveTools(CancellationToken cancellationToken = default)
    {
        var tools = await _db.Tools
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                t.Tags,
                t.ClientProfileId
            })
            .ToListAsync(cancellationToken);
        return Ok(tools);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTool(Guid id, CancellationToken cancellationToken = default)
    {
        var tool = await _db.Tools.AsNoTracking()
            .Where(t => t.Id == id && t.IsActive)
            .Select(t => new { t.Id, t.Name, t.Description, t.Tags, t.ClientProfileId })
            .FirstOrDefaultAsync(cancellationToken);
        if (tool is null) return NotFound();
        return Ok(tool);
    }
}
