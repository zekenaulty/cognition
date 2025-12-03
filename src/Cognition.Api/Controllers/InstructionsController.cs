using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Cognition.Api.Infrastructure.Security;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Instructions;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
[ApiController]
[Route("api/instructions")]
public class InstructionsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public InstructionsController(CognitionDbContext db) => _db = db;

    public sealed class CreateInstructionRequest
    {
        public string Name { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public string Kind { get; init; } = "Other";
        public bool RolePlay { get; init; } = false;
        public string[]? Tags { get; init; }
        public string? Version { get; init; }
        public CreateInstructionRequest() { }
        public CreateInstructionRequest(string name, string content, string kind = "Other", bool rolePlay = false, string[]? tags = null, string? version = null)
        {
            Name = name;
            Content = content;
            Kind = kind;
            RolePlay = rolePlay;
            Tags = tags;
            Version = version;
        }
    }
    public sealed class PatchInstructionRequest
    {
        public string? Name { get; init; }
        public string? Content { get; init; }
        public string? Kind { get; init; }
        public bool? RolePlay { get; init; }
        public string[]? Tags { get; init; }
        public string? Version { get; init; }
        public bool? IsActive { get; init; }
        public PatchInstructionRequest() { }
        public PatchInstructionRequest(string? name, string? content, string? kind, bool? rolePlay, string[]? tags, string? version, bool? isActive)
        {
            Name = name;
            Content = content;
            Kind = kind;
            RolePlay = rolePlay;
            Tags = tags;
            Version = version;
            IsActive = isActive;
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default)
    {
        var items = await _db.Instructions.AsNoTracking().OrderBy(i => i.Name).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInstructionRequest req, CancellationToken cancellationToken = default)
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
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { i.Id });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchInstructionRequest req, CancellationToken cancellationToken = default)
    {
        var i = await _db.Instructions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (i == null) return NotFound();
        i.Name = req.Name ?? i.Name;
        i.Content = req.Content ?? i.Content;
        if (req.Kind != null && Enum.TryParse<Cognition.Data.Relational.Modules.Common.InstructionKind>(req.Kind, true, out var kind)) i.Kind = kind;
        if (req.RolePlay.HasValue) i.RolePlay = req.RolePlay.Value;
        i.Tags = req.Tags ?? i.Tags;
        i.Version = req.Version ?? i.Version;
        if (req.IsActive.HasValue) i.IsActive = req.IsActive.Value;
        i.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
[ApiController]
[Route("api/instruction-sets")]
public class InstructionSetsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public InstructionSetsController(CognitionDbContext db) => _db = db;

    public sealed class CreateSetRequest
    {
        public string Name { get; init; } = string.Empty;
        public string? Scope { get; init; }
        public Guid? ScopeRefId { get; init; }
        public string? Description { get; init; }
        public CreateSetRequest() { }
        public CreateSetRequest(string name, string? scope, Guid? scopeRefId, string? description)
        {
            Name = name;
            Scope = scope;
            ScopeRefId = scopeRefId;
            Description = description;
        }
    }
    public sealed class PatchSetRequest
    {
        public string? Name { get; init; }
        public string? Scope { get; init; }
        public Guid? ScopeRefId { get; init; }
        public string? Description { get; init; }
        public bool? IsActive { get; init; }
        public PatchSetRequest() { }
        public PatchSetRequest(string? name, string? scope, Guid? scopeRefId, string? description, bool? isActive)
        {
            Name = name;
            Scope = scope;
            ScopeRefId = scopeRefId;
            Description = description;
            IsActive = isActive;
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default)
    {
        var items = await _db.InstructionSets.AsNoTracking().OrderBy(s => s.Name).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSetRequest req, CancellationToken cancellationToken = default)
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
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { s.Id });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchSetRequest req, CancellationToken cancellationToken = default)
    {
        var s = await _db.InstructionSets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (s == null) return NotFound();
        s.Name = req.Name ?? s.Name;
        s.Scope = req.Scope ?? s.Scope;
        s.ScopeRefId = req.ScopeRefId ?? s.ScopeRefId;
        s.Description = req.Description ?? s.Description;
        if (req.IsActive.HasValue) s.IsActive = req.IsActive.Value;
        s.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
