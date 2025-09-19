using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Knowledge;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cognition.Jobs;

public sealed class KnowledgeIndexingService : BackgroundService
{
    private readonly CognitionDbContext _db;
    private readonly IVectorStore _store;
    private readonly IOptions<OpenSearchVectorsOptions> _options;
    private readonly ILogger<KnowledgeIndexingService> _logger;

    public KnowledgeIndexingService(CognitionDbContext db, IVectorStore store, IOptions<OpenSearchVectorsOptions> options, ILogger<KnowledgeIndexingService> logger)
    { _db = db; _store = store; _options = options; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Simple periodic indexer; upserts all KnowledgeItems each run (idempotent).
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_options.Value.UseEmbeddingPipeline)
                {
                    _logger.LogInformation("Knowledge indexing skipped: UseEmbeddingPipeline=false");
                }
                else
                {
                    await IndexAllAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Knowledge indexing failed");
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task IndexAllAsync(CancellationToken ct)
    {
        const int batchSize = 500;
        int page = 0; int indexed = 0;
        while (true)
        {
            var items = await _db.KnowledgeItems
                .AsNoTracking()
                .OrderByDescending(k => k.UpdatedAtUtc ?? k.CreatedAtUtc)
                .Skip(page * batchSize)
                .Take(batchSize)
                .ToListAsync(ct);
            if (items.Count == 0) break;

            var docs = new List<VectorItem>(items.Count);
            foreach (var ki in items)
            {
                var meta = new Dictionary<string, object?>
                {
                    ["contentType"] = ki.ContentType.ToString(),
                    ["categories"] = ki.Categories,
                    ["keywords"] = ki.Keywords,
                    ["source"] = ki.Source,
                    ["timestamp"] = ki.Timestamp,
                    ["properties"] = ki.Properties
                };
                docs.Add(new VectorItem
                {
                    Id = ki.Id.ToString(),
                    TenantKey = "default",
                    Kind = "knowledge",
                    Text = ki.Content ?? string.Empty,
                    Embedding = null, // computed by pipeline
                    Metadata = meta,
                    SchemaVersion = 1
                });
            }

            await _store.UpsertManyAsync(docs, ct);
            indexed += docs.Count; page++;
        }
        _logger.LogInformation("Knowledge indexing upserted {Count} items", indexed);
    }
}

