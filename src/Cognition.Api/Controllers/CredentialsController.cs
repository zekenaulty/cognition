using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Cognition.Api.Infrastructure.Security;
using Cognition.Api.Infrastructure.ErrorHandling;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.LLM;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
[ApiController]
[Route("api/credentials")]
public class CredentialsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public CredentialsController(CognitionDbContext db) => _db = db;

    public record CreateRequest(Guid ProviderId, string KeyRef, string? Notes);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? providerId, CancellationToken cancellationToken)
    {
        var q = _db.ApiCredentials.AsNoTracking().AsQueryable();
        if (providerId.HasValue) q = q.Where(c => c.ProviderId == providerId.Value);
        var items = await q.OrderBy(c => c.ProviderId).Select(c => new
        {
            c.Id, c.ProviderId, c.KeyRef, c.IsValid, c.LastUsedAtUtc, c.Notes
        }).ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken cancellationToken)
    {
        if (!await _db.Providers.AnyAsync(p => p.Id == req.ProviderId, cancellationToken))
            return BadRequest(ApiErrorResponse.Create("provider_not_found", "Provider not found."));
        var cred = new ApiCredential
        {
            ProviderId = req.ProviderId,
            KeyRef = req.KeyRef,
            IsValid = true,
            Notes = req.Notes
        };
        _db.ApiCredentials.Add(cred);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { cred.Id });
    }

    [HttpPost("validate/{id:guid}")]
    public async Task<IActionResult> Validate(Guid id, CancellationToken cancellationToken)
    {
        var cred = await _db.ApiCredentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cred == null) return NotFound(ApiErrorResponse.Create("credential_not_found", "Credential not found."));
        // Minimal validation stub: checks presence of env var only
        var value = Environment.GetEnvironmentVariable(cred.KeyRef);
        cred.LastUsedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { ok = !string.IsNullOrWhiteSpace(value) });
    }
}
