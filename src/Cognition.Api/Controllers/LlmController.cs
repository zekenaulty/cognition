using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.LLM;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/llm")]
public class LlmController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public LlmController(CognitionDbContext db) => _db = db;

    [HttpGet("providers")]
    public async Task<IActionResult> GetProviders()
    {
        var providers = await _db.Providers
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.DisplayName, p.BaseUrl, p.IsActive })
            .ToListAsync();
        return Ok(providers);
    }

    [HttpGet("providers/{id:guid}/models")]
    public async Task<IActionResult> GetModelsForProvider(Guid id)
    {
        var models = await _db.Models
            .AsNoTracking()
            .Where(m => m.ProviderId == id)
            .OrderBy(m => m.Name)
            .Select(m => new {
                m.Id, m.Name, m.DisplayName, m.SupportsVision, m.SupportsStreaming,
                m.InputCostPer1M, m.CachedInputCostPer1M, m.OutputCostPer1M, m.IsDeprecated
            })
            .ToListAsync();
        return Ok(models);
    }

    [HttpGet("models")]
    public async Task<IActionResult> GetModels([FromQuery] Guid? providerId)
    {
        var q = _db.Models.AsNoTracking().AsQueryable();
        if (providerId.HasValue) q = q.Where(m => m.ProviderId == providerId.Value);
        var models = await q
            .OrderBy(m => m.Name)
            .Select(m => new {
                m.Id, m.Name, m.DisplayName, m.ProviderId, m.SupportsVision, m.SupportsStreaming
            })
            .ToListAsync();
        return Ok(models);
    }

    public record PatchProviderRequest(bool? IsActive, string? BaseUrl);
    public record PatchModelRequest(bool? IsDeprecated);

    [HttpPatch("providers/{id:guid}")]
    public async Task<IActionResult> PatchProvider(Guid id, [FromBody] PatchProviderRequest req)
    {
        var p = await _db.Providers.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (req.IsActive.HasValue) p.IsActive = req.IsActive.Value;
        if (req.BaseUrl != null) p.BaseUrl = string.IsNullOrWhiteSpace(req.BaseUrl) ? null : req.BaseUrl;
        p.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("models/{id:guid}")]
    public async Task<IActionResult> PatchModel(Guid id, [FromBody] PatchModelRequest req)
    {
        var m = await _db.Models.FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();
        if (req.IsDeprecated.HasValue) m.IsDeprecated = req.IsDeprecated.Value;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
