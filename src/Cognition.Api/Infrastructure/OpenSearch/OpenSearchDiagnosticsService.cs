using System.Net.Http;
using System.Text.Json;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;

namespace Cognition.Api.Infrastructure.OpenSearch;

public interface IOpenSearchDiagnosticsService
{
    Task<OpenSearchDiagnosticsReport> GetReportAsync(CancellationToken ct);
}

public sealed record OpenSearchDiagnosticsReport(
    DateTime CheckedAtUtc,
    string Endpoint,
    string DefaultIndex,
    string? PipelineId,
    string? ModelId,
    bool ClusterAvailable,
    string? ClusterStatus,
    bool IndexExists,
    bool PipelineExists,
    string? ModelState,
    string? ModelDeployState,
    IReadOnlyList<string> Notes);

public sealed class OpenSearchDiagnosticsService : IOpenSearchDiagnosticsService
{
    private const string HttpClientName = "opensearch-bootstrap";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOpenSearchClient _client;
    private readonly OpenSearchVectorsOptions _vectors;
    private readonly OpenSearchModelOptions _model;
    private readonly ILogger<OpenSearchDiagnosticsService> _logger;

    public OpenSearchDiagnosticsService(
        IHttpClientFactory httpClientFactory,
        IOpenSearchClient client,
        IOptions<OpenSearchVectorsOptions> vectorOptions,
        IOptions<OpenSearchModelOptions> modelOptions,
        ILogger<OpenSearchDiagnosticsService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _vectors = vectorOptions?.Value ?? throw new ArgumentNullException(nameof(vectorOptions));
        _model = modelOptions?.Value ?? throw new ArgumentNullException(nameof(modelOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OpenSearchDiagnosticsReport> GetReportAsync(CancellationToken ct)
    {
        var notes = new List<string>();

        var endpoint = _vectors.Url ?? "unknown";
        var (clusterAvailable, clusterStatus) = await GetClusterStatusAsync(ct, notes).ConfigureAwait(false);
        var indexExists = await CheckIndexAsync(ct, notes).ConfigureAwait(false);
        var pipelineExists = await CheckPipelineAsync(ct, notes).ConfigureAwait(false);
        var (modelState, deployState) = await GetModelStateAsync(ct, notes).ConfigureAwait(false);

        return new OpenSearchDiagnosticsReport(
            CheckedAtUtc: DateTime.UtcNow,
            Endpoint: endpoint,
            DefaultIndex: _vectors.DefaultIndex,
            PipelineId: _vectors.PipelineId,
            ModelId: string.IsNullOrWhiteSpace(_model.ModelId) ? null : _model.ModelId,
            ClusterAvailable: clusterAvailable,
            ClusterStatus: clusterStatus,
            IndexExists: indexExists,
            PipelineExists: pipelineExists,
            ModelState: modelState,
            ModelDeployState: deployState,
            Notes: notes);
    }

    private async Task<(bool available, string? status)> GetClusterStatusAsync(CancellationToken ct, List<string> notes)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var response = await _client.LowLevel.Cluster.HealthAsync<StringResponse>().ConfigureAwait(false);
            if (!response.Success)
            {
                notes.Add($"Cluster health call failed (status {response.HttpStatusCode}).");
                return (false, null);
            }

            var status = TryExtractString(response.Body, "status");
            return (true, status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenSearch cluster health failed.");
            notes.Add($"Cluster health error: {ex.Message}");
            return (false, null);
        }
    }

    private async Task<bool> CheckIndexAsync(CancellationToken ct, List<string> notes)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var response = await _client.LowLevel.Indices.ExistsAsync<StringResponse>(_vectors.DefaultIndex).ConfigureAwait(false);
            if (response.Success && response.HttpStatusCode == 200)
            {
                return true;
            }

            if (response.HttpStatusCode == 404)
            {
                return false;
            }

            notes.Add($"Index '{_vectors.DefaultIndex}' check failed (status {response.HttpStatusCode}).");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenSearch index check failed.");
            notes.Add($"Index '{_vectors.DefaultIndex}' check error: {ex.Message}");
        }

        return false;
    }

    private async Task<bool> CheckPipelineAsync(CancellationToken ct, List<string> notes)
    {
        if (string.IsNullOrWhiteSpace(_vectors.PipelineId))
        {
            notes.Add("PipelineId not configured; skipping pipeline diagnostics.");
            return false;
        }

        try
        {
            ct.ThrowIfCancellationRequested();
            var response = await _client.LowLevel.Ingest.GetPipelineAsync<StringResponse>(_vectors.PipelineId).ConfigureAwait(false);
            return response.Success && response.HttpStatusCode == 200;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenSearch pipeline check failed.");
            notes.Add($"Pipeline '{_vectors.PipelineId}' check error: {ex.Message}");
            return false;
        }
    }

    private async Task<(string? modelState, string? deployState)> GetModelStateAsync(CancellationToken ct, List<string> notes)
    {
        if (string.IsNullOrWhiteSpace(_model.ModelId))
        {
            notes.Add("ModelId not configured; skipping model diagnostics.");
            return (null, null);
        }

        try
        {
            var http = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await http.GetAsync($"_plugins/_ml/models/{_model.ModelId}", ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                notes.Add($"Model '{_model.ModelId}' lookup failed with status {(int)response.StatusCode}.");
                return (null, null);
            }

            using var doc = await ReadJsonAsync(response, ct).ConfigureAwait(false);
            if (doc is null)
            {
                notes.Add($"Model '{_model.ModelId}' lookup returned an empty body.");
                return (null, null);
            }

            var root = doc.RootElement;
            var modelState = root.TryGetProperty("model_state", out var stateElement)
                ? stateElement.GetString()
                : null;
            var deployState = root.TryGetProperty("deploy_state", out var deployElement)
                ? deployElement.GetString()
                : null;

            return (modelState, deployState);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenSearch model diagnostics failed.");
            notes.Add($"Model '{_model.ModelId}' diagnostics error: {ex.Message}");
            return (null, null);
        }
    }

    private static string? TryExtractString(string? json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return null;
    }

    private static async Task<JsonDocument?> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        if (stream.Length == 0)
        {
            return null;
        }

        return await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
    }
}
