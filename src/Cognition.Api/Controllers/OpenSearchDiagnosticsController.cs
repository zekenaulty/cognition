using Cognition.Api.Infrastructure.OpenSearch;
using Microsoft.AspNetCore.Mvc;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/diagnostics/opensearch")]
public sealed class OpenSearchDiagnosticsController : ControllerBase
{
    private readonly IOpenSearchDiagnosticsService _diagnostics;
    private readonly IOpenSearchBootstrapper _bootstrapper;

    public OpenSearchDiagnosticsController(
        IOpenSearchDiagnosticsService diagnostics,
        IOpenSearchBootstrapper bootstrapper)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _bootstrapper = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
    }

    [HttpGet]
    public async Task<ActionResult<OpenSearchDiagnosticsReport>> GetAsync(CancellationToken ct)
    {
        var report = await _diagnostics.GetReportAsync(ct).ConfigureAwait(false);
        return Ok(report);
    }

    [HttpPost("bootstrap")]
    public async Task<ActionResult<OpenSearchBootstrapResult>> BootstrapAsync(CancellationToken ct)
    {
        var result = await _bootstrapper.BootstrapAsync(ct).ConfigureAwait(false);
        return Ok(result);
    }
}
