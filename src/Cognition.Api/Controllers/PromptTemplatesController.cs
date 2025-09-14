using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Prompts;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/prompt-templates")]
public class PromptTemplatesController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public PromptTemplatesController(CognitionDbContext db) => _db = db;

    public record CreateRequest(string Name, string Template, string PromptType = "None", object? Tokens = null, string? Example = null, bool IsActive = true);
    public record PatchRequest(string? Name, string? Template, string? PromptType, object? Tokens, string? Example, bool? IsActive);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.PromptTemplates.AsNoTracking().OrderBy(t => t.Name).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req)
    {
        if (!Enum.TryParse<Cognition.Data.Relational.Modules.Common.PromptType>(req.PromptType, true, out var kind)) kind = Cognition.Data.Relational.Modules.Common.PromptType.None;
        var t = new PromptTemplate
        {
            Name = req.Name,
            Template = req.Template,
            PromptType = kind,
            Tokens = req.Tokens as System.Collections.Generic.Dictionary<string, object?>,
            Example = req.Example,
            IsActive = req.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.PromptTemplates.Add(t);
        await _db.SaveChangesAsync();
        return Ok(new { t.Id });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchRequest req)
    {
        var t = await _db.PromptTemplates.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        t.Name = req.Name ?? t.Name;
        t.Template = req.Template ?? t.Template;
        if (req.PromptType != null && Enum.TryParse<Cognition.Data.Relational.Modules.Common.PromptType>(req.PromptType, true, out var kind)) t.PromptType = kind;
        if (req.Tokens != null) t.Tokens = req.Tokens as System.Collections.Generic.Dictionary<string, object?>;
        t.Example = req.Example ?? t.Example;
        if (req.IsActive.HasValue) t.IsActive = req.IsActive.Value;
        t.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var t = await _db.PromptTemplates.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        _db.PromptTemplates.Remove(t);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
