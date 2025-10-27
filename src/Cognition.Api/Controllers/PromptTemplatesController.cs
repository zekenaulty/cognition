using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
            Tokens = ConvertTokens(req.Tokens),
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
        if (req.Tokens != null) t.Tokens = ConvertTokens(req.Tokens);
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

    private static Dictionary<string, JsonElement>? ConvertTokens(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is Dictionary<string, JsonElement> elementDict)
        {
            return elementDict.ToDictionary(kv => kv.Key, kv => kv.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }

        if (value is Dictionary<string, object?> objectDict)
        {
            var dict = new Dictionary<string, JsonElement>(objectDict.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var (key, obj) in objectDict)
            {
                dict[key] = ToJsonElement(obj);
            }
            return dict;
        }

        if (value is JsonElement json)
        {
            if (json.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return json.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);
        }

        var serialized = JsonSerializer.Serialize(value);
        using var doc = JsonDocument.Parse(serialized);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return doc.RootElement
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);
    }

    private static JsonElement ToJsonElement(object? value)
    {
        if (value is JsonElement element)
        {
            return element.Clone();
        }

        var json = JsonSerializer.Serialize(value);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
