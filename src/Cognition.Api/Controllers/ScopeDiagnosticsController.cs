using Cognition.Api.Infrastructure.ScopePath;
using Cognition.Clients.Configuration;
using Cognition.Clients.Scope;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/diagnostics/scope")]
public sealed class ScopeDiagnosticsController : ControllerBase
{
    private readonly IOptionsMonitor<ScopePathOptions> _options;
    private readonly IScopePathDiagnostics _diagnostics;
    private readonly ScopePathBackfillService _backfill;

    public ScopeDiagnosticsController(
        IOptionsMonitor<ScopePathOptions> options,
        IScopePathDiagnostics diagnostics,
        ScopePathBackfillService backfill)
    {
        _options = options;
        _diagnostics = diagnostics;
        _backfill = backfill;
    }

    [HttpGet]
    public ActionResult<object> Get()
    {
        var opts = _options.CurrentValue;
        var snapshot = _diagnostics.Snapshot();
        var payload = new
        {
            flags = new
            {
                opts.PathAwareHashingEnabled,
                opts.DualWriteEnabled
            },
            metrics = new
            {
                snapshot.LegacyWrites,
                snapshot.PathWrites,
                snapshot.LastUpdatedUtc,
                snapshot.PrincipalCounts,
                collisions = new
                {
                    count = snapshot.CollisionCount,
                    lastUpdatedUtc = snapshot.LastCollisionUtc
                },
                backfill = new
                {
                    updated = snapshot.BackfillUpdated,
                    skipped = snapshot.BackfillSkipped,
                    lastUpdatedUtc = snapshot.LastBackfillUtc
                }
            }
        };
        return Ok(payload);
    }

    [HttpPost("backfill")]
    public async Task<ActionResult<ScopePathBackfillResult>> RunBackfillAsync([FromQuery] int batchSize = 200, CancellationToken ct = default)
    {
        batchSize = Math.Clamp(batchSize, 10, 1000);
        var result = await _backfill.RunAsync(batchSize, ct).ConfigureAwait(false);
        return Ok(result);
    }
}
