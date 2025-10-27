using System.Net.Http;
using System.Text;
using System.Text.Json;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Provisioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;

namespace Cognition.Api.Infrastructure.OpenSearch;

public interface IOpenSearchBootstrapper
{
    Task<OpenSearchBootstrapResult> BootstrapAsync(CancellationToken ct);
}

public sealed record OpenSearchBootstrapResult(
    string ModelId,
    bool ModelCreated,
    bool ModelDeployed,
    bool PipelineCreated,
    bool IndexCreated,
    IReadOnlyList<string> Notes);

internal sealed record ModelBootstrapOutcome(string ModelId, bool Created, IReadOnlyList<string> Notes);

public sealed class OpenSearchBootstrapper : IOpenSearchBootstrapper
{
    private const string HttpClientName = "opensearch-bootstrap";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOpenSearchClient _client;
    private readonly OpenSearchProvisioner _provisioner;
    private readonly OpenSearchVectorsOptions _vectors;
    private readonly OpenSearchModelOptions _model;
    private readonly ILogger<OpenSearchBootstrapper> _logger;

    public OpenSearchBootstrapper(
        IOpenSearchClient client,
        OpenSearchProvisioner provisioner,
        IHttpClientFactory httpClientFactory,
        IOptions<OpenSearchVectorsOptions> vectorOptions,
        IOptions<OpenSearchModelOptions> modelOptions,
        ILogger<OpenSearchBootstrapper> logger)
    {
        _client = client;
        _provisioner = provisioner;
        _httpClientFactory = httpClientFactory;
        _vectors = vectorOptions.Value;
        _model = modelOptions.Value;
        _logger = logger;
    }

    public async Task<OpenSearchBootstrapResult> BootstrapAsync(CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient(HttpClientName);
        var notes = new List<string>();

        var modelOutcome = await EnsureModelAsync(http, _model.ModelId, ct).ConfigureAwait(false);
        notes.AddRange(modelOutcome.Notes);

        var deployed = await EnsureModelDeployedAsync(http, modelOutcome.ModelId, ct).ConfigureAwait(false);
        if (deployed)
        {
            notes.Add($"Model '{modelOutcome.ModelId}' deployed.");
        }

        var pipelineCreated = await EnsurePipelineAsync(modelOutcome.ModelId, ct).ConfigureAwait(false);
        if (pipelineCreated)
        {
            notes.Add($"Pipeline '{_vectors.PipelineId ?? "vectors-embed"}' created.");
        }

        var indexCreated = await EnsureIndexAsync(ct).ConfigureAwait(false);
        if (indexCreated)
        {
            notes.Add($"Index '{_vectors.DefaultIndex}' created.");
        }

        return new OpenSearchBootstrapResult(
            modelOutcome.ModelId,
            modelOutcome.Created,
            deployed,
            pipelineCreated,
            indexCreated,
            notes);
    }

    private async Task<ModelBootstrapOutcome> EnsureModelAsync(HttpClient http, string? configuredModelId, CancellationToken ct)
    {
        var notes = new List<string>();

        if (!string.IsNullOrWhiteSpace(configuredModelId))
        {
            if (await ModelExistsAsync(http, configuredModelId!, ct).ConfigureAwait(false))
            {
                _logger.LogInformation("Using configured OpenSearch model id {ModelId}.", configuredModelId);
                return new ModelBootstrapOutcome(configuredModelId!, false, notes);
            }

            notes.Add($"Configured model '{configuredModelId}' was not found. A new model will be registered.");
        }

        var existing = await FindExistingModelAsync(http, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            notes.Add($"Found existing model '{existing}' matching bootstrap settings.");
            return new ModelBootstrapOutcome(existing, false, notes);
        }

        var taskId = await RegisterModelAsync(http, ct).ConfigureAwait(false);
        notes.Add("Model registration submitted.");

        var modelId = await WaitForModelRegistrationAsync(http, taskId, ct).ConfigureAwait(false);
        notes.Add($"Model registration task '{taskId}' completed with model id '{modelId}'.");

        return new ModelBootstrapOutcome(modelId, true, notes);
    }

    private async Task<bool> EnsureModelDeployedAsync(HttpClient http, string modelId, CancellationToken ct)
    {
        var info = await GetModelInfoAsync(http, modelId, ct).ConfigureAwait(false);
        if (info.State is not null && info.State.Equals("DEPLOYED", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        _logger.LogInformation("Deploying OpenSearch model {ModelId}.", modelId);
        using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"_plugins/_ml/models/{modelId}/_deploy");
        using var response = await http.SendAsync(request, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException($"Failed to deploy model '{modelId}': {error}");
        }

        await WaitForDeploymentAsync(http, modelId, ct).ConfigureAwait(false);
        return true;
    }

    private async Task<bool> EnsurePipelineAsync(string modelId, CancellationToken ct)
    {
        var pipelineId = string.IsNullOrWhiteSpace(_vectors.PipelineId) ? "vectors-embed" : _vectors.PipelineId!;
        if (await PipelineExistsAsync(pipelineId, ct).ConfigureAwait(false))
        {
            return false;
        }

        _logger.LogInformation("Creating OpenSearch ingest pipeline {PipelineId}.", pipelineId);
        var processors = new[]
        {
            new Dictionary<string, object?>
            {
                ["inference"] = new Dictionary<string, object?>
                {
                    ["model_id"] = modelId,
                    ["target_field"] = _model.EmbeddingField,
                    ["field_map"] = new Dictionary<string, string>
                    {
                        [_model.TextField] = _model.TextField
                    }
                }
            }
        };

        var body = new
        {
            description = $"Embedding pipeline for {_model.Bootstrap.Name}",
            processors
        };

        var resp = await _client.LowLevel.Ingest.PutPipelineAsync<StringResponse>(
            pipelineId,
            PostData.Serializable(body)).ConfigureAwait(false);

        if (!resp.Success)
        {
            throw new InvalidOperationException($"Failed to create pipeline '{pipelineId}': {resp.Body ?? resp.DebugInformation}");
        }

        return true;
    }

    private async Task<bool> EnsureIndexAsync(CancellationToken ct)
    {
        var existing = await _client.Indices.ExistsAsync(_vectors.DefaultIndex, c => c, ct).ConfigureAwait(false);
        await _provisioner.EnsureProvisionedAsync(ct).ConfigureAwait(false);
        return !existing.Exists;
    }

    private static StringContent JsonContent(object value)
        => new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static async Task<JsonDocument?> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
    }

    private async Task<bool> ModelExistsAsync(HttpClient http, string modelId, CancellationToken ct)
    {
        using var response = await http.GetAsync($"_plugins/_ml/models/{modelId}", ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    private async Task<string?> FindExistingModelAsync(HttpClient http, CancellationToken ct)
    {
        var body = new
        {
            name = _model.Bootstrap.Name,
            version = _model.Bootstrap.Version
        };

        using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "_plugins/_ml/models/_search")
        {
            Content = JsonContent(body)
        };
        using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
        using var doc = await ReadJsonAsync(response, ct).ConfigureAwait(false);
        if (doc is null)
        {
            return null;
        }

        if (!doc.RootElement.TryGetProperty("model_list", out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var element in list.EnumerateArray())
        {
            if (element.TryGetProperty("model_id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
            {
                return idElement.GetString();
            }
        }

        return null;
    }

    private async Task<string> RegisterModelAsync(HttpClient http, CancellationToken ct)
    {
        var body = new
        {
            name = _model.Bootstrap.Name,
            version = _model.Bootstrap.Version,
            description = _model.Bootstrap.Description,
            model_format = _model.Bootstrap.ModelFormat,
            function_name = _model.Bootstrap.FunctionName,
            model_content_hash_value = _model.Bootstrap.ContentHash,
            model_config = new
            {
                model_type = _model.Bootstrap.ModelType,
                embedding_dimension = _model.Bootstrap.EmbeddingDimension,
                framework_type = _model.Bootstrap.FrameworkType
            },
            url = _model.Bootstrap.Url
        };

        using var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "_plugins/_ml/models/_register")
        {
            Content = JsonContent(body)
        };
        using var response = await http.SendAsync(request, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException($"Failed to register OpenSearch model: {error}");
        }

        using var doc = await ReadJsonAsync(response, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Model registration response was empty.");
        if (!doc.RootElement.TryGetProperty("task_id", out var taskElement) || taskElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("Model registration did not return a task_id.");
        }

        return taskElement.GetString()!;
    }

    private async Task<string> WaitForModelRegistrationAsync(HttpClient http, string taskId, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            using var response = await http.GetAsync($"_plugins/_ml/tasks/{taskId}", ct).ConfigureAwait(false);
            using var doc = await ReadJsonAsync(response, ct).ConfigureAwait(false);
            if (doc is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
                continue;
            }

            if (!doc.RootElement.TryGetProperty("state", out var stateElement))
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
                continue;
            }

            var state = stateElement.GetString();
            if (string.Equals(state, "FAILED", StringComparison.OrdinalIgnoreCase))
            {
                var error = doc.RootElement.TryGetProperty("error", out var errorElement)
                    ? errorElement.ToString()
                    : "unknown";
                throw new InvalidOperationException($"Model registration task '{taskId}' failed: {error}");
            }

            if (string.Equals(state, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                if (doc.RootElement.TryGetProperty("model_id", out var modelElement) &&
                    modelElement.ValueKind == JsonValueKind.String)
                {
                    return modelElement.GetString()!;
                }

                throw new InvalidOperationException($"Model registration task '{taskId}' completed without returning a model id.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
        }

        throw new TimeoutException($"Timed out waiting for model registration task '{taskId}'.");
    }

    private async Task WaitForDeploymentAsync(HttpClient http, string modelId, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var info = await GetModelInfoAsync(http, modelId, ct).ConfigureAwait(false);
            if (info.State is not null && info.State.Equals("DEPLOYED", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (info.State is not null && info.State.Equals("FAILED", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Model '{modelId}' deployment failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
        }

        throw new TimeoutException($"Timed out waiting for model '{modelId}' to deploy.");
    }

    private async Task<(string? State, string? DeployState)> GetModelInfoAsync(HttpClient http, string modelId, CancellationToken ct)
    {
        using var response = await http.GetAsync($"_plugins/_ml/models/{modelId}", ct).ConfigureAwait(false);
        using var doc = await ReadJsonAsync(response, ct).ConfigureAwait(false);
        if (doc is null)
        {
            return (null, null);
        }

        string? state = null;
        string? deploy = null;

        if (doc.RootElement.TryGetProperty("model_state", out var stateElement) && stateElement.ValueKind == JsonValueKind.String)
        {
            state = stateElement.GetString();
        }

        if (doc.RootElement.TryGetProperty("deploy_state", out var deployElement) && deployElement.ValueKind == JsonValueKind.String)
        {
            deploy = deployElement.GetString();
        }

        return (state, deploy);
    }

    private async Task<bool> PipelineExistsAsync(string pipelineId, CancellationToken ct)
    {
        var resp = await _client.LowLevel.Ingest.GetPipelineAsync<StringResponse>(pipelineId).ConfigureAwait(false);
        return resp.Success && resp.HttpStatusCode is 200;
    }
}
