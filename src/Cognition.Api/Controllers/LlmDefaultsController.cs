using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Security;
using Cognition.Api.Infrastructure.ErrorHandling;
using Cognition.Api.Services;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.LLM;
using Cognition.Api.Infrastructure.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.UserOrHigher)]
[Route("api/settings/llm-defaults")]
public sealed class LlmDefaultsController : ControllerBase
{
    private readonly CognitionDbContext _db;
    private readonly ILlmDefaultService _service;

    public LlmDefaultsController(CognitionDbContext db, ILlmDefaultService service)
    {
        _db = db;
        _service = service;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken ct)
    {
        var row = await _service.GetAsync(ct);
        if (row == null)
        {
          return Ok(new { providerId = (Guid?)null, modelId = (Guid?)null, isActive = false, priority = 0 });
        }
        await _db.Entry(row).Reference(r => r.Model).LoadAsync(ct);
        await _db.Entry(row.Model).Reference(m => m.Provider).LoadAsync(ct);
        return Ok(new
        {
            providerId = row.Model.ProviderId,
            modelId = row.ModelId,
            isActive = row.IsActive,
            priority = row.Priority,
            providerName = row.Model.Provider.Name,
            modelName = row.Model.Name,
            updatedByUserId = row.UpdatedByUserId,
            updatedAtUtc = row.UpdatedAtUtc,
        });
    }

    public sealed class UpdateLlmDefaultsRequest
    {
        [Required]
        public Guid ModelId { get; set; }
        public int Priority { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    [Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
    [HttpPatch]
    public async Task<IActionResult> PatchAsync([FromBody] UpdateLlmDefaultsRequest request, CancellationToken ct)
    {
        var model = await _db.Models.Include(m => m.Provider).FirstOrDefaultAsync(m => m.Id == request.ModelId, ct);
        if (model == null)
        {
            return BadRequest(ApiErrorResponse.Create("model_not_found", "Specified model not found."));
        }
        if (!model.Provider.IsActive)
        {
            return BadRequest(ApiErrorResponse.Create("provider_inactive", "Provider is inactive."));
        }
        if (model.IsDeprecated)
        {
            return BadRequest(ApiErrorResponse.Create("model_deprecated", "Model is deprecated."));
        }

        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? updatedBy = Guid.TryParse(userId, out var guid) ? guid : null;

        var updated = await _service.UpsertAsync(request.ModelId, updatedBy, request.Priority, request.IsActive, ct);
        return Ok(new { updated.Id, updated.ModelId, updated.Priority, updated.IsActive, updated.UpdatedByUserId, updated.UpdatedAtUtc });
    }
}
