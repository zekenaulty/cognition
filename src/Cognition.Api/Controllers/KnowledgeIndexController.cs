using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Knowledge;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Store;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cognition.Api.Controllers;

[ApiController]
[Route("api/knowledge")] 
public class KnowledgeIndexController : ControllerBase
{
    private readonly CognitionDbContext _db;
    private readonly IVectorStore _store;
    private readonly IOptions<OpenSearchVectorsOptions> _options;

    public KnowledgeIndexController(CognitionDbContext db, IVectorStore store, IOptions<OpenSearchVectorsOptions> options)
    { _db = db; _store = store; _options = options; }

    [HttpPost("reindex")] 
    public async Task<IActionResult> Reindex(CancellationToken ct)
    {
        if (!_options.Value.UseEmbeddingPipeline)
            return BadRequest(new { message = "OpenSearch embedding pipeline disabled; set OpenSearch:Vectors:UseEmbeddingPipeline=true." });

        int batchSize = 500; int page = 0; int indexed = 0;
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
                    Embedding = null, // computed by OpenSearch ingest pipeline
                    Metadata = meta,
                    SchemaVersion = 1
                });
            }
            await _store.UpsertManyAsync(docs, ct);
            indexed += docs.Count; page++;
        }

        return Ok(new { indexed });
    }
}

