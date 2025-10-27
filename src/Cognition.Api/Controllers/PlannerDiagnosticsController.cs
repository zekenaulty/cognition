using Cognition.Api.Infrastructure.Planning;
using Microsoft.AspNetCore.Mvc;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/diagnostics/planner")]
public sealed class PlannerDiagnosticsController : ControllerBase
{
    private readonly IPlannerHealthService _healthService;

    public PlannerDiagnosticsController(IPlannerHealthService healthService)
    {
        _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
    }

    [HttpGet]
    public async Task<ActionResult<PlannerHealthReport>> GetAsync(CancellationToken ct = default)
    {
        var report = await _healthService.GetReportAsync(ct).ConfigureAwait(false);
        return Ok(report);
    }
}

