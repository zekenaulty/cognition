using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.LLM;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/client-profiles")]
public class ClientProfilesController : ControllerBase
{
    private readonly CognitionDbContext _db;
    public ClientProfilesController(CognitionDbContext db) => _db = db;

    public record CreateRequest(string Name, Guid ProviderId, Guid? ModelId, Guid? ApiCredentialId,
        string? UserName, string? BaseUrlOverride, int MaxTokens = 8192, double Temperature = 0.7, double TopP = 0.95,
        double PresencePenalty = 0, double FrequencyPenalty = 0, bool Stream = true, bool LoggingEnabled = false);

    public record UpdateRequest(Guid? ModelId, Guid? ApiCredentialId,
        string? UserName, string? BaseUrlOverride, int? MaxTokens, double? Temperature, double? TopP,
        double? PresencePenalty, double? FrequencyPenalty, bool? Stream, bool? LoggingEnabled);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _db.ClientProfiles.AsNoTracking()
            .OrderBy(cp => cp.Name)
            .Select(cp => new {
                cp.Id, cp.Name, cp.ProviderId, cp.ModelId, cp.ApiCredentialId,
                cp.MaxTokens, cp.Temperature, cp.TopP, cp.PresencePenalty, cp.FrequencyPenalty, cp.Stream, cp.LoggingEnabled,
                cp.UserName, cp.BaseUrlOverride
            })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var cp = await _db.ClientProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (cp == null) return NotFound();
        return Ok(cp);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req)
    {
        if (!await _db.Providers.AnyAsync(p => p.Id == req.ProviderId)) return BadRequest("Provider not found");
        if (req.ModelId.HasValue && !await _db.Models.AnyAsync(m => m.Id == req.ModelId.Value)) return BadRequest("Model not found");
        if (req.ApiCredentialId.HasValue && !await _db.ApiCredentials.AnyAsync(a => a.Id == req.ApiCredentialId.Value)) return BadRequest("Credential not found");
        var cp = new ClientProfile
        {
            Name = req.Name,
            ProviderId = req.ProviderId,
            ModelId = req.ModelId,
            ApiCredentialId = req.ApiCredentialId,
            UserName = req.UserName,
            BaseUrlOverride = req.BaseUrlOverride,
            MaxTokens = req.MaxTokens,
            Temperature = req.Temperature,
            TopP = req.TopP,
            PresencePenalty = req.PresencePenalty,
            FrequencyPenalty = req.FrequencyPenalty,
            Stream = req.Stream,
            LoggingEnabled = req.LoggingEnabled,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.ClientProfiles.Add(cp);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = cp.Id }, new { cp.Id });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRequest req)
    {
        var cp = await _db.ClientProfiles.FirstOrDefaultAsync(x => x.Id == id);
        if (cp == null) return NotFound();
        if (req.ModelId.HasValue)
        {
            if (!await _db.Models.AnyAsync(m => m.Id == req.ModelId.Value)) return BadRequest("Model not found");
            cp.ModelId = req.ModelId;
        }
        if (req.ApiCredentialId.HasValue)
        {
            if (!await _db.ApiCredentials.AnyAsync(a => a.Id == req.ApiCredentialId.Value)) return BadRequest("Credential not found");
            cp.ApiCredentialId = req.ApiCredentialId;
        }
        cp.UserName = req.UserName ?? cp.UserName;
        cp.BaseUrlOverride = req.BaseUrlOverride ?? cp.BaseUrlOverride;
        if (req.MaxTokens.HasValue) cp.MaxTokens = req.MaxTokens.Value;
        if (req.Temperature.HasValue) cp.Temperature = req.Temperature.Value;
        if (req.TopP.HasValue) cp.TopP = req.TopP.Value;
        if (req.PresencePenalty.HasValue) cp.PresencePenalty = req.PresencePenalty.Value;
        if (req.FrequencyPenalty.HasValue) cp.FrequencyPenalty = req.FrequencyPenalty.Value;
        if (req.Stream.HasValue) cp.Stream = req.Stream.Value;
        if (req.LoggingEnabled.HasValue) cp.LoggingEnabled = req.LoggingEnabled.Value;
        cp.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
