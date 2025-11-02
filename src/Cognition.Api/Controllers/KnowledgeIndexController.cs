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
using Microsoft.AspNetCore.Authorization;
using Cognition.Api.Infrastructure.Security;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdministratorOnly)]
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

        const int batchSize = 500;
        var page = 0;
        var indexed = 0;

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
                var meta = BuildMetadata(ki);
                var doc = new VectorItem
                {
                    Id = ki.Id.ToString(),
                    TenantKey = "default",
                    Kind = "knowledge",
                    Text = ki.Content ?? string.Empty,
                    Embedding = null,
                    Metadata = meta,
                    SchemaVersion = 1
                };
                doc.ApplyScopeFromMetadata(ki.Properties, meta);
                docs.Add(doc);
            }

            await _store.UpsertManyAsync(docs, ct);
            indexed += docs.Count;
            page++;
        }

        return Ok(new { indexed });
    }

    private static Dictionary<string, object> BuildMetadata(KnowledgeItem ki)
    {
        var meta = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["contentType"] = ki.ContentType.ToString()
        };
        if (ki.Categories is { Length: > 0 }) meta["categories"] = ki.Categories;
        if (ki.Keywords is { Length: > 0 }) meta["keywords"] = ki.Keywords;
        if (!string.IsNullOrWhiteSpace(ki.Source)) meta["source"] = ki.Source!;
        meta["timestamp"] = ki.Timestamp;
        if (ki.Properties is not null && ki.Properties.Count > 0) meta["properties"] = ki.Properties;
        return meta;
    }
}
