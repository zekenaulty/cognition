using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Cognition.Api.Infrastructure.Security;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.LLM;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
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
        double? PresencePenalty, double? FrequencyPenalty, bool? Stream, bool? LoggingEnabled, bool? IsActive);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _db.ClientProfiles.AsNoTracking()
            .OrderBy(cp => cp.Name)
            .Select(cp => new {
                cp.Id, cp.Name, cp.ProviderId, cp.ModelId, cp.ApiCredentialId,
                cp.MaxTokens, cp.Temperature, cp.TopP, cp.PresencePenalty, cp.FrequencyPenalty, cp.Stream, cp.LoggingEnabled,
                cp.UserName, cp.BaseUrlOverride
            })
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var cp = await _db.ClientProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (cp == null) return NotFound();
        return Ok(cp);
    }

    [HttpPost]
    [Swashbuckle.AspNetCore.Annotations.SwaggerOperation(
        Summary = "Create a ClientProfile",
        Description = "Creates a new client profile. Example body: {\n  'name':'OpenAI 4o default', 'providerId':'<GUID>', 'modelId':'<GUID>',\n  'maxTokens':8192, 'temperature':0.7, 'topP':0.95, 'stream':true\n}")]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken cancellationToken)
    {
        if (!await _db.Providers.AnyAsync(p => p.Id == req.ProviderId, cancellationToken)) return BadRequest("Provider not found");
        if (req.ModelId.HasValue && !await _db.Models.AnyAsync(m => m.Id == req.ModelId.Value, cancellationToken)) return BadRequest("Model not found");
        if (req.ApiCredentialId.HasValue && !await _db.ApiCredentials.AnyAsync(a => a.Id == req.ApiCredentialId.Value, cancellationToken)) return BadRequest("Credential not found");
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
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = cp.Id }, new { cp.Id });
    }

    [HttpPatch("{id:guid}")]
    [Swashbuckle.AspNetCore.Annotations.SwaggerOperation(
        Summary = "Update ClientProfile",
        Description = "Update model/parameters. To disable a profile set isActive=false. Use force=true to reassign in-use tools/agents to default profile before disabling.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRequest req, [FromQuery] bool force = false, CancellationToken cancellationToken = default)
    {
        var cp = await _db.ClientProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (cp == null) return NotFound();
        if (req.ModelId.HasValue)
        {
            if (!await _db.Models.AnyAsync(m => m.Id == req.ModelId.Value, cancellationToken)) return BadRequest("Model not found");
            cp.ModelId = req.ModelId;
        }
        if (req.ApiCredentialId.HasValue)
        {
            if (!await _db.ApiCredentials.AnyAsync(a => a.Id == req.ApiCredentialId.Value, cancellationToken)) return BadRequest("Credential not found");
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
        if (req.IsActive.HasValue && req.IsActive.Value == false)
        {
            var inUse = await _db.Tools.AnyAsync(t => t.ClientProfileId == id, cancellationToken) || await _db.Agents.AnyAsync(a => a.ClientProfileId == id, cancellationToken);
            if (inUse && !force) return BadRequest(new { message = "ClientProfile is in use; set force=true to reassign to default before disabling." });
            if (inUse && force)
            {
                var def = await ResolveDefaultProfileAsync(cancellationToken);
                if (def == null) return BadRequest(new { message = "Default profile (OpenAI gpt-4o) not available for reassignment." });
                await _db.Tools.Where(t => t.ClientProfileId == id).ExecuteUpdateAsync(setters => setters.SetProperty(t => t.ClientProfileId, def.Value), cancellationToken);
                await _db.Agents.Where(a => a.ClientProfileId == id).ExecuteUpdateAsync(setters => setters.SetProperty(a => a.ClientProfileId, def.Value), cancellationToken);
            }
            cp.IsActive = false;
        }
        cp.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Swashbuckle.AspNetCore.Annotations.SwaggerOperation(
        Summary = "Delete ClientProfile",
        Description = "Deletes the profile. Use force=true to reassign in-use tools/agents to default profile before delete.")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] bool force = false, CancellationToken cancellationToken = default)
    {
        var cp = await _db.ClientProfiles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (cp == null) return NotFound();
        bool inUse = await _db.Tools.AnyAsync(t => t.ClientProfileId == id, cancellationToken)
                      || await _db.Agents.AnyAsync(a => a.ClientProfileId == id, cancellationToken);
        if (inUse && !force) return BadRequest(new { message = "ClientProfile is in use; set force=true to reassign to default profile before delete." });
        if (inUse && force)
        {
            var def = await ResolveDefaultProfileAsync(cancellationToken);
            if (def == null) return BadRequest(new { message = "Default profile (OpenAI gpt-4o) not available for reassignment." });
            await _db.Tools.Where(t => t.ClientProfileId == id).ExecuteUpdateAsync(setters => setters.SetProperty(t => t.ClientProfileId, def.Value), cancellationToken);
            await _db.Agents.Where(a => a.ClientProfileId == id).ExecuteUpdateAsync(setters => setters.SetProperty(a => a.ClientProfileId, def.Value), cancellationToken);
        }
        _db.ClientProfiles.Remove(cp);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<Guid?> ResolveDefaultProfileAsync(CancellationToken cancellationToken)
    {
        var openai = await _db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Name.ToLower() == "openai", cancellationToken);
        if (openai == null) return null;
        var gpt4o = await _db.Models.AsNoTracking().FirstOrDefaultAsync(m => m.ProviderId == openai.Id && m.Name.ToLower() == "gpt-4o", cancellationToken);
        if (gpt4o == null) return null;
        var def = await _db.ClientProfiles.AsNoTracking().FirstOrDefaultAsync(cp => cp.ProviderId == openai.Id && cp.ModelId == gpt4o.Id, cancellationToken);
        return def?.Id;
    }
}

