using Cognition.Clients.Images;
using Cognition.Clients.LLM;
using Microsoft.Extensions.DependencyInjection;

namespace Cognition.Clients;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCognitionClients(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddScoped<ILLMClientFactory, LLMClientFactory>();
        services.AddScoped<IImageClient, OpenAIImageClient>();
        return services;
    }
}

