using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Api.Infrastructure.Security;
using Cognition.Api.Infrastructure.Validation;
using Cognition.Api.Infrastructure.ErrorHandling;
using Cognition.Clients.Tools;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Tools;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
[ApiController]
[Route("api/tools")]
public class ToolsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    private readonly IToolRegistry _registry;
    public ToolsController(CognitionDbContext db, IToolRegistry registry) { _db = db; _registry = registry; }

    public record CreateToolRequest(
        [property: Required, StringLength(128, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Name must contain non-whitespace characters.")]
        string Name,
        [property: Required, StringLength(256, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "ClassPath must contain non-whitespace characters.")]
        string ClassPath,
        [property: StringLength(2048)]
        string? Description,
        string[]? Tags,
        object? Metadata,
        [property: StringLength(2048)]
        string? Example,
        bool IsActive = true,
        [property: NotEmptyGuid]
        Guid? ClientProfileId = null);

    public record PatchToolRequest(
        [property: StringLength(128, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Name must contain non-whitespace characters when provided.")]
        string? Name,
        [property: StringLength(256, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "ClassPath must contain non-whitespace characters when provided.")]
        string? ClassPath,
        [property: StringLength(2048)]
        string? Description,
        string[]? Tags,
        object? Metadata,
        [property: StringLength(2048)]
        string? Example,
        bool? IsActive,
        [property: NotEmptyGuid]
        Guid? ClientProfileId = null);

    [HttpGet]
    public async Task<IActionResult> ListTools(CancellationToken cancellationToken = default)
    {
        var items = await _db.Tools.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTool([FromBody] CreateToolRequest req, CancellationToken cancellationToken = default)
    {
        var normalizedClassPath = req.ClassPath.Trim();
        var normalizedName = req.Name.Trim();
        // Validate ClassPath is a resolvable ITool type
        if (!IsValidToolClassPath(normalizedClassPath, out var validationError))
            return BadRequest(ApiErrorResponse.Create("invalid_tool_class_path", validationError ?? "Invalid tool class path."));
        var tags = req.Tags is null
            ? null
            : req.Tags
                .Select(tag => tag?.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var t = new Tool
        {
            Name = normalizedName,
            ClassPath = normalizedClassPath,
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            Tags = tags is { Length: > 0 } ? tags : null,
            Metadata = req.Metadata as System.Collections.Generic.Dictionary<string, object?>,
            Example = string.IsNullOrWhiteSpace(req.Example) ? null : req.Example.Trim(),
            IsActive = req.IsActive,
            ClientProfileId = req.ClientProfileId
        };
        _db.Tools.Add(t);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { t.Id });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> PatchTool(Guid id, [FromBody] PatchToolRequest req, CancellationToken cancellationToken = default)
    {
        var t = await _db.Tools.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (t == null) return NotFound();
        if (req.Name != null)
        {
            var trimmed = req.Name.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed)) t.Name = trimmed;
        }
        if (!string.IsNullOrWhiteSpace(req.ClassPath))
        {
            var trimmed = req.ClassPath!.Trim();
            if (!IsValidToolClassPath(trimmed, out var validationError))
                return BadRequest(ApiErrorResponse.Create("invalid_tool_class_path", validationError ?? "Invalid tool class path."));
            t.ClassPath = trimmed;
        }
        if (req.Description != null)
        {
            t.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        }
        if (req.Tags != null)
        {
            var sanitized = req.Tags
                .Select(tag => tag?.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            t.Tags = sanitized.Length > 0 ? sanitized : null;
        }
        if (req.Metadata != null) t.Metadata = req.Metadata as System.Collections.Generic.Dictionary<string, object?>;
        if (req.Example != null) t.Example = string.IsNullOrWhiteSpace(req.Example) ? null : req.Example.Trim();
        if (req.IsActive.HasValue) t.IsActive = req.IsActive.Value;
        if (req.ClientProfileId.HasValue) t.ClientProfileId = req.ClientProfileId.Value;
        await _db.SaveChangesAsync(cancellationToken);
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

[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
[ApiController]
[Route("api/tool-parameters")]
public class ToolParametersController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public ToolParametersController(CognitionDbContext db) => _db = db;

    public record CreateParamRequest(
        [property: NotEmptyGuid]
        Guid ToolId,
        [property: Required, StringLength(128, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Name must contain non-whitespace characters.")]
        string Name,
        [property: Required, StringLength(128, MinimumLength = 1), RegularExpression(@".*\S.*", ErrorMessage = "Type must contain non-whitespace characters.")]
        string Type,
        ToolParamDirection Direction = ToolParamDirection.Input,
        bool Required = false,
        object? DefaultValue = null,
        object? Options = null,
        [property: StringLength(1024)]
        string? Description = null);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? toolId, CancellationToken cancellationToken = default)
    {
        var q = _db.ToolParameters.AsNoTracking().AsQueryable();
        if (toolId.HasValue) q = q.Where(p => p.ToolId == toolId.Value);
        var items = await q.OrderBy(p => p.ToolId).ThenBy(p => p.Name).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateParamRequest req, CancellationToken cancellationToken = default)
    {
        if (!await _db.Tools.AnyAsync(t => t.Id == req.ToolId, cancellationToken)) return BadRequest(ApiErrorResponse.Create("tool_not_found", "Tool not found."));
        var p = new ToolParameter
        {
            ToolId = req.ToolId,
            Name = req.Name.Trim(),
            Type = req.Type.Trim(),
            Direction = req.Direction,
            Required = req.Required,
            DefaultValue = NormalizeToDictionary(req.DefaultValue),
            Options = NormalizeOptions(req.Options),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim()
        };
        _db.ToolParameters.Add(p);
        await _db.SaveChangesAsync(cancellationToken);
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

[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
[ApiController]
[Route("api/tool-provider-supports")]
public class ToolProviderSupportsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public ToolProviderSupportsController(CognitionDbContext db) => _db = db;

    public record CreateSupportRequest(
        [property: NotEmptyGuid]
        Guid ToolId,
        [property: NotEmptyGuid]
        Guid ProviderId,
        [property: NotEmptyGuid]
        Guid? ModelId,
        SupportLevel SupportLevel = SupportLevel.Full,
        [property: StringLength(1024)]
        string? Notes = null);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? toolId, [FromQuery] Guid? providerId, CancellationToken cancellationToken = default)
    {
        var q = _db.ToolProviderSupports.AsNoTracking().AsQueryable();
        if (toolId.HasValue) q = q.Where(s => s.ToolId == toolId.Value);
        if (providerId.HasValue) q = q.Where(s => s.ProviderId == providerId.Value);
        var items = await q.OrderBy(s => s.ToolId).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupportRequest req, CancellationToken cancellationToken = default)
    {
        if (!await _db.Tools.AnyAsync(t => t.Id == req.ToolId, cancellationToken)) return BadRequest(ApiErrorResponse.Create("tool_not_found", "Tool not found."));
        if (!await _db.Providers.AnyAsync(p => p.Id == req.ProviderId, cancellationToken)) return BadRequest(ApiErrorResponse.Create("provider_not_found", "Provider not found."));
        if (req.ModelId.HasValue && !await _db.Models.AnyAsync(m => m.Id == req.ModelId.Value, cancellationToken)) return BadRequest(ApiErrorResponse.Create("model_not_found", "Model not found."));
        var s = new ToolProviderSupport
        {
            ToolId = req.ToolId,
            ProviderId = req.ProviderId,
            ModelId = req.ModelId,
            SupportLevel = req.SupportLevel,
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim()
        };
        _db.ToolProviderSupports.Add(s);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { s.Id });
    }
}
