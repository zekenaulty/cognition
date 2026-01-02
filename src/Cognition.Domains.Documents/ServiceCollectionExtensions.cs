using Cognition.Domains.Documents.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;

namespace Cognition.Domains.Documents;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCognitionDomainsDocuments(this IServiceCollection services, IConfiguration configuration)
    {
        var urlSection = configuration.GetSection("RavenDb:Urls");
        var urls = urlSection.GetChildren()
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        if (urls.Length == 0)
        {
            urls = new[] { "http://localhost:8080" };
        }

        var database = configuration["RavenDb:Database"] ?? "cognition_dod_docs";

        var store = new DocumentStore
        {
            Urls = urls,
            Database = database
        }.Initialize();

        services.AddSingleton<IDocumentStore>(store);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().OpenAsyncSession());
        services.AddScoped<IDomainManifestDocumentRepository, DomainManifestDocumentRepository>();
        services.AddScoped<IKnowledgeAssetDocumentRepository, KnowledgeAssetDocumentRepository>();

        return services;
    }
}
