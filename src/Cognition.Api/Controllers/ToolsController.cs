using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Tools;
using Cognition.Clients.Tools;
using System.Text.Json;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/tools")]
public class ToolsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    private readonly IToolRegistry _registry;
    public ToolsController(CognitionDbContext db, IToolRegistry registry) { _db = db; _registry = registry; }

    public record CreateToolRequest(string Name, string ClassPath, string? Description, string[]? Tags, object? Metadata, string? Example, bool IsActive = true, Guid? ClientProfileId = null);
    public record PatchToolRequest(string? Name, string? ClassPath, string? Description, string[]? Tags, object? Metadata, string? Example, bool? IsActive, Guid? ClientProfileId = null);

    [HttpGet]
    public async Task<IActionResult> ListTools()
    {
        var items = await _db.Tools.AsNoTracking().OrderBy(t => t.Name).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTool([FromBody] CreateToolRequest req)
    {
        // Validate ClassPath is a resolvable ITool type
        if (!IsValidToolClassPath(req.ClassPath, out var validationError))
            return BadRequest(validationError);
        var t = new Tool { Name = req.Name, ClassPath = req.ClassPath, Description = req.Description, Tags = req.Tags, Metadata = req.Metadata as System.Collections.Generic.Dictionary<string, object?>, Example = req.Example, IsActive = req.IsActive, ClientProfileId = req.ClientProfileId };
        _db.Tools.Add(t);
        await _db.SaveChangesAsync();
        return Ok(new { t.Id });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> PatchTool(Guid id, [FromBody] PatchToolRequest req)
    {
        var t = await _db.Tools.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        t.Name = req.Name ?? t.Name;
        if (!string.IsNullOrWhiteSpace(req.ClassPath))
        {
            if (!IsValidToolClassPath(req.ClassPath!, out var validationError))
                return BadRequest(validationError);
            t.ClassPath = req.ClassPath!;
        }
        t.Description = req.Description ?? t.Description;
        t.Tags = req.Tags ?? t.Tags;
        if (req.Metadata != null) t.Metadata = req.Metadata as System.Collections.Generic.Dictionary<string, object?>;
        t.Example = req.Example ?? t.Example;
        if (req.IsActive.HasValue) t.IsActive = req.IsActive.Value;
        if (req.ClientProfileId.HasValue) t.ClientProfileId = req.ClientProfileId.Value;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private bool IsValidToolClassPath(string classPath, out string? error)
    {
        error = null;
        if (!_registry.TryResolveByClassPath(classPath, out var type))
        {
            error = "ClassPath must reference a known ITool implementation (Namespace.Type or Namespace.Type, Assembly).";
            return false;
        }
        if (!typeof(ITool).IsAssignableFrom(type))
        {
            error = "ClassPath type must implement ITool.";
            return false;
        }
        return true;
    }
}

[ApiController]
[Route("api/tool-parameters")]
public class ToolParametersController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public ToolParametersController(CognitionDbContext db) => _db = db;

    public record CreateParamRequest(Guid ToolId, string Name, string Type, string Direction = "Input", bool Required = false, object? DefaultValue = null, object? Options = null, string? Description = null);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? toolId)
    {
        var q = _db.ToolParameters.AsNoTracking().AsQueryable();
        if (toolId.HasValue) q = q.Where(p => p.ToolId == toolId.Value);
        var items = await q.OrderBy(p => p.ToolId).ThenBy(p => p.Name).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateParamRequest req)
    {
        if (!await _db.Tools.AnyAsync(t => t.Id == req.ToolId)) return BadRequest("Tool not found");
        if (!Enum.TryParse<Cognition.Data.Relational.Modules.Common.ToolParamDirection>(req.Direction, true, out var dir)) dir = Cognition.Data.Relational.Modules.Common.ToolParamDirection.Input;
        var p = new ToolParameter
        {
            ToolId = req.ToolId,
            Name = req.Name,
            Type = req.Type,
            Direction = dir,
            Required = req.Required,
            DefaultValue = NormalizeToDictionary(req.DefaultValue),
            Options = NormalizeOptions(req.Options),
            Description = req.Description
        };
        _db.ToolParameters.Add(p);
        await _db.SaveChangesAsync();
        return Ok(new { p.Id });
    }

    private static System.Collections.Generic.Dictionary<string, object?>? NormalizeToDictionary(object? value)
    {
        if (value is null) return null;
        if (value is System.Collections.Generic.Dictionary<string, object?> dict) return dict;
        if (value is JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.Object:
                    return JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object?>>(je.GetRawText());
                case JsonValueKind.Array:
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    return new System.Collections.Generic.Dictionary<string, object?> { { "value", JsonToDotNet(je) } };
            }
        }
        // Fallback: wrap scalar
        return new System.Collections.Generic.Dictionary<string, object?> { { "value", value } };
    }

    private static System.Collections.Generic.Dictionary<string, object?>? NormalizeOptions(object? value)
    {
        if (value is null) return null;
        if (value is System.Collections.Generic.Dictionary<string, object?> dict) return dict;
        if (value is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Object)
                return JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object?>>(je.GetRawText());
            // Wrap arrays/scalars under a conventional key
            return new System.Collections.Generic.Dictionary<string, object?> { { "values", JsonToDotNet(je) } };
        }
        return new System.Collections.Generic.Dictionary<string, object?> { { "values", value } };
    }

    private static object? JsonToDotNet(JsonElement je)
    {
        switch (je.ValueKind)
        {
            case JsonValueKind.String:
                return je.GetString();
            case JsonValueKind.Number:
                if (je.TryGetInt64(out var l)) return l;
                if (je.TryGetDouble(out var d)) return d;
                return je.GetRawText();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Array:
                var list = new System.Collections.Generic.List<object?>();
                foreach (var el in je.EnumerateArray()) list.Add(JsonToDotNet(el));
                return list.ToArray();
            case JsonValueKind.Object:
                return JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object?>>(je.GetRawText());
            default:
                return je.GetRawText();
        }
    }
}

[ApiController]
[Route("api/tool-provider-supports")]
public class ToolProviderSupportsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public ToolProviderSupportsController(CognitionDbContext db) => _db = db;

    public record CreateSupportRequest(Guid ToolId, Guid ProviderId, Guid? ModelId, string SupportLevel = "Full", string? Notes = null);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? toolId, [FromQuery] Guid? providerId)
    {
        var q = _db.ToolProviderSupports.AsNoTracking().AsQueryable();
        if (toolId.HasValue) q = q.Where(s => s.ToolId == toolId.Value);
        if (providerId.HasValue) q = q.Where(s => s.ProviderId == providerId.Value);
        var items = await q.OrderBy(s => s.ToolId).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupportRequest req)
    {
        if (!await _db.Tools.AnyAsync(t => t.Id == req.ToolId)) return BadRequest("Tool not found");
        if (!await _db.Providers.AnyAsync(p => p.Id == req.ProviderId)) return BadRequest("Provider not found");
        if (req.ModelId.HasValue && !await _db.Models.AnyAsync(m => m.Id == req.ModelId.Value)) return BadRequest("Model not found");
        if (!Enum.TryParse<Cognition.Data.Relational.Modules.Common.SupportLevel>(req.SupportLevel, true, out var lvl)) lvl = Cognition.Data.Relational.Modules.Common.SupportLevel.Full;
        var s = new ToolProviderSupport { ToolId = req.ToolId, ProviderId = req.ProviderId, ModelId = req.ModelId, SupportLevel = lvl, Notes = req.Notes };
        _db.ToolProviderSupports.Add(s);
        await _db.SaveChangesAsync();
        return Ok(new { s.Id });
    }
}
