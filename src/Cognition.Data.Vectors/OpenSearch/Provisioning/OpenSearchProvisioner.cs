using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using OpenSearch.Client;
using OpenSearch.Net;

using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Provisioning.Mappings;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Provisioning.Pipelines;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Provisioning;

public sealed class OpenSearchProvisioner
{
    private readonly IOpenSearchClient _client;
    private readonly OpenSearchVectorsOptions _vectors;
    private readonly OpenSearchModelOptions _model;
    private readonly ILogger<OpenSearchProvisioner> _logger;

    public OpenSearchProvisioner(
        IOpenSearchClient client,
        IOptions<OpenSearchVectorsOptions> vectors,
        IOptions<OpenSearchModelOptions> model,
        ILogger<OpenSearchProvisioner> logger)
    {
        _client = client;
        _vectors = vectors.Value;
        _model = model.Value;
        _logger = logger;
    }

    public async Task EnsureProvisionedAsync(CancellationToken ct = default)
    {
        var index = _vectors.DefaultIndex;
        // Create index if missing
        var exists = await _client.Indices.ExistsAsync(index, x => x, ct).ConfigureAwait(false);
        if (!exists.Exists)
        {
            var body = VectorIndexMappingProvider.BuildIndexRequestBody(_vectors);
            var createResp = await _client.LowLevel.Indices.CreateAsync<StringResponse>(index, PostData.Serializable(body)).ConfigureAwait(false);
            if (createResp.HttpStatusCode is < 200 or >= 300)
                throw new InvalidOperationException($"Failed to create index '{index}': {createResp.Body}");
            _logger.LogInformation("Created OpenSearch index {Index}", index);
        }
        else
        {
            // update mapping idempotently (put mapping)
            var putMapping = new
            {
                properties = ((dynamic)VectorIndexMappingProvider.BuildIndexRequestBody(_vectors)).mappings.properties
            };
            var mapResp = await _client.LowLevel.Indices.PutMappingAsync<StringResponse>(index, PostData.Serializable(putMapping)).ConfigureAwait(false);
            if (mapResp.HttpStatusCode is >= 400)
                _logger.LogWarning("PutMapping returned {Code}: {Body}", mapResp.HttpStatusCode, mapResp.Body);
        }

        // Optional: embedding ingest pipeline
        if (_vectors.UseEmbeddingPipeline)
        {
            var pipelineId = string.IsNullOrWhiteSpace(_vectors.PipelineId) ? "vectors-embed" : _vectors.PipelineId;
            var pipelineBody = EmbeddingPipelineProvider.Build(_vectors, _model);
            var resp = await _client.LowLevel.Ingest.PutPipelineAsync<StringResponse>(pipelineId, PostData.Serializable(pipelineBody)).ConfigureAwait(false);
            if (resp.HttpStatusCode is < 200 or >= 300)
                throw new InvalidOperationException($"Failed to create/put pipeline '{pipelineId}': {resp.Body}");
            _logger.LogInformation("Provisioned embedding pipeline {Pipeline}", pipelineId);
        }
    }
}
