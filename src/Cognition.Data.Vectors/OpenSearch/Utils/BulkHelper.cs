using OpenSearch.Client;
using Microsoft.Extensions.Logging;

using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Utils;

public sealed class BulkHelper
{
    private readonly IOpenSearchClient _client;
    private readonly OpenSearchVectorsOptions _options;
    private readonly Microsoft.Extensions.Logging.ILogger<BulkHelper>? _logger;

    public BulkHelper(IOpenSearchClient client, OpenSearchVectorsOptions options, Microsoft.Extensions.Logging.ILogger<BulkHelper>? logger = null)
    {
        _client = client;
        _options = options;
        _logger = logger;
    }

    public async Task IndexChunksAsync(IEnumerable<object> docs, string index, string? pipeline, int chunk = 1000, CancellationToken ct = default)
    {
        var list = docs.ToList();
        for (int i = 0; i < list.Count; i += chunk)
        {
            var page = list.Skip(i).Take(Math.Min(chunk, list.Count - i)).ToList();

            var response = await _client.BulkAsync(b =>
            {
                b.Index(index);
                if (!string.IsNullOrWhiteSpace(pipeline))
                    b.Pipeline(pipeline);
                foreach (var d in page)
                    b.Index<object>(x => x.Document(d));
                return b;
            }, ct).ConfigureAwait(false);

            if (response.Errors && _logger is not null)
            {
                foreach (var item in response.ItemsWithErrors)
                {
                    _logger!.LogWarning("Bulk index failure: {Error} status={Status} index={Index} id={Id}", item.Error?.Reason, item.Status, item.Index, item.Id);
                }
            }
        }
    }
}
