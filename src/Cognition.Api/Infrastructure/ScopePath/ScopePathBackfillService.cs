using System;
using System.Collections.Generic;
using System.Linq;
using Cognition.Clients.Configuration;
using Cognition.Clients.Scope;
using Cognition.Contracts.Scopes;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Knowledge;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cognition.Api.Infrastructure.ScopePath;

public sealed record ScopePathBackfillResult(
    int Updated,
    int Skipped,
    IReadOnlyList<string> Notes);

public sealed class ScopePathBackfillService
{
    private readonly CognitionDbContext _db;
    private readonly ScopePathOptions _options;
    private readonly ILogger<ScopePathBackfillService> _logger;
    private readonly IScopePathDiagnostics _diagnostics;

    public ScopePathBackfillService(
        CognitionDbContext db,
        IOptions<ScopePathOptions> options,
        ILogger<ScopePathBackfillService> logger,
        IScopePathDiagnostics diagnostics)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
        _diagnostics = diagnostics;
    }

    public async Task<ScopePathBackfillResult> RunAsync(int batchSize, CancellationToken ct)
    {
        if (!_options.DualWriteEnabled)
        {
            return new ScopePathBackfillResult(0, 0, new[] { "DualWriteEnabled is false; skipping backfill." });
        }

        var embeddings = await _db.KnowledgeEmbeddings
            .Where(e => e.ScopePath == null)
            .OrderBy(e => e.Id)
            .Take(batchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (embeddings.Count == 0)
        {
            return new ScopePathBackfillResult(0, 0, new[] { "No embeddings require backfill." });
        }

        var updated = 0;
        var skipped = 0;

        foreach (var embedding in embeddings)
        {
            if (embedding.Metadata is null || embedding.Metadata.Count == 0)
            {
                skipped++;
                continue;
            }

            if (!ScopeTokenFactory.TryCreateScopeToken(embedding.Metadata, out var scope) ||
                !ScopePathProjection.TryCreate(scope, out var projection))
            {
                skipped++;
                continue;
            }

            if (!string.IsNullOrEmpty(projection.PrincipalId) && Guid.TryParse(projection.PrincipalId, out var principalGuid))
            {
                embedding.ScopePrincipalId = principalGuid;
            }
            else
            {
                embedding.ScopePrincipalId = null;
            }

            embedding.ScopePrincipalType = projection.PrincipalType;
            embedding.ScopePath = projection.Canonical;
            embedding.ScopeSegments = projection.ToSegmentDictionary();
            updated++;
        }

        if (updated > 0)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("ScopePath backfill updated {Count} embeddings (skipped {Skipped})", updated, skipped);
        }

        _diagnostics.RecordBackfill(updated, skipped);

        return new ScopePathBackfillResult(updated, skipped, Array.Empty<string>());
    }
}