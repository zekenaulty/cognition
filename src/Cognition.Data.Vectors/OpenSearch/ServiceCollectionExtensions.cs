using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSearch.Client;

using Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Provisioning;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Store;
using Cognition.Data.Vectors.OpenSearch.OpenSearch.Utils;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCognitionOpenSearchVectors(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<OpenSearchVectorsOptions>(o => config.GetSection("OpenSearch:Vectors").Bind(o));
        services.Configure<OpenSearchModelOptions>(o => config.GetSection("OpenSearch:Model").Bind(o));

        services.AddSingleton<IOpenSearchClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenSearchVectorsOptions>>().Value;
            return OpenSearchClientFactory.Create(opts);
        });

        services.AddSingleton<OpenSearchProvisioner>();
        services.AddSingleton<IVectorStore, OpenSearchVectorStore>();
        services.AddHostedService<ProvisionerHostedService>();
        return services;
    }
}

internal sealed class ProvisionerHostedService : IHostedService
{
    private readonly OpenSearchProvisioner _provisioner;
    private readonly ILogger<ProvisionerHostedService> _logger;

    public ProvisionerHostedService(OpenSearchProvisioner provisioner, ILogger<ProvisionerHostedService> logger)
    {
        _provisioner = provisioner;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _provisioner.EnsureProvisionedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenSearch provisioning failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
