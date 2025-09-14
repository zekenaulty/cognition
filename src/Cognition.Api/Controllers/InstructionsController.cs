using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Instructions;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/instructions")]
public class InstructionsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public InstructionsController(CognitionDbContext db) => _db = db;

    public record CreateInstructionRequest(string Name, string Content, string Kind = "Other", bool RolePlay = false, string[]? Tags = null, string? Version = null);
    public record PatchInstructionRequest(string? Name, string? Content, string? Kind, bool? RolePlay, string[]? Tags, string? Version, bool? IsActive);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.Instructions.AsNoTracking().OrderBy(i => i.Name).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInstructionRequest req)
    {
        if (!Enum.TryParse<Cognition.Data.Relational.Modules.Common.InstructionKind>(req.Kind, true, out var kind)) kind = Cognition.Data.Relational.Modules.Common.InstructionKind.Other;
        var i = new Instruction
        {
            Name = req.Name,
            Content = req.Content,
            Kind = kind,
            RolePlay = req.RolePlay,
            Tags = req.Tags,
            Version = req.Version,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Instructions.Add(i);
        await _db.SaveChangesAsync();
        return Ok(new { i.Id });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchInstructionRequest req)
    {
        var i = await _db.Instructions.FirstOrDefaultAsync(x => x.Id == id);
        if (i == null) return NotFound();
        i.Name = req.Name ?? i.Name;
        i.Content = req.Content ?? i.Content;
        if (req.Kind != null && Enum.TryParse<Cognition.Data.Relational.Modules.Common.InstructionKind>(req.Kind, true, out var kind)) i.Kind = kind;
        if (req.RolePlay.HasValue) i.RolePlay = req.RolePlay.Value;
        i.Tags = req.Tags ?? i.Tags;
        i.Version = req.Version ?? i.Version;
        if (req.IsActive.HasValue) i.IsActive = req.IsActive.Value;
        i.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/instruction-sets")]
public class InstructionSetsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public InstructionSetsController(CognitionDbContext db) => _db = db;

    public record CreateSetRequest(string Name, string? Scope, Guid? ScopeRefId, string? Description);
    public record PatchSetRequest(string? Name, string? Scope, Guid? ScopeRefId, string? Description, bool? IsActive);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.InstructionSets.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSetRequest req)
    {
        var s = new InstructionSet
        {
            Name = req.Name,
            Scope = req.Scope,
            ScopeRefId = req.ScopeRefId,
            Description = req.Description,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.InstructionSets.Add(s);
        await _db.SaveChangesAsync();
        return Ok(new { s.Id });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchSetRequest req)
    {
        var s = await _db.InstructionSets.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        s.Name = req.Name ?? s.Name;
        s.Scope = req.Scope ?? s.Scope;
        s.ScopeRefId = req.ScopeRefId ?? s.ScopeRefId;
        s.Description = req.Description ?? s.Description;
        if (req.IsActive.HasValue) s.IsActive = req.IsActive.Value;
        s.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
