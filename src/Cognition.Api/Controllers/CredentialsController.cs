using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.LLM;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/credentials")]
public class CredentialsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public CredentialsController(CognitionDbContext db) => _db = db;

    public record CreateRequest(Guid ProviderId, string KeyRef, string? Notes);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? providerId)
    {
        var q = _db.ApiCredentials.AsNoTracking().AsQueryable();
        if (providerId.HasValue) q = q.Where(c => c.ProviderId == providerId.Value);
        var items = await q.OrderBy(c => c.ProviderId).Select(c => new
        {
            c.Id, c.ProviderId, c.KeyRef, c.IsValid, c.LastUsedAtUtc, c.Notes
        }).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req)
    {
        if (!await _db.Providers.AnyAsync(p => p.Id == req.ProviderId)) return BadRequest("Provider not found");
        var cred = new ApiCredential
        {
            ProviderId = req.ProviderId,
            KeyRef = req.KeyRef,
            IsValid = true,
            Notes = req.Notes
        };
        _db.ApiCredentials.Add(cred);
        await _db.SaveChangesAsync();
        return Ok(new { cred.Id });
    }

    [HttpPost("validate/{id:guid}")]
    public async Task<IActionResult> Validate(Guid id)
    {
        var cred = await _db.ApiCredentials.FirstOrDefaultAsync(c => c.Id == id);
        if (cred == null) return NotFound();
        // Minimal validation stub: checks presence of env var only
        var value = Environment.GetEnvironmentVariable(cred.KeyRef);
        cred.LastUsedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { ok = !string.IsNullOrWhiteSpace(value) });
    }
}
