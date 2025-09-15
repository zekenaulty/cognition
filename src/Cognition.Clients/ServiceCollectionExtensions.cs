using Cognition.Clients.Images;
using Cognition.Clients.LLM;
using Cognition.Clients.Tools;
using Microsoft.Extensions.DependencyInjection;
using Cognition.Clients.Agents;
using Polly;
using System.Net;
using System.Net.Http;

namespace Cognition.Clients;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCognitionClients(this IServiceCollection services)
    {
        // Default HttpClient factory; per-request resiliency is implemented inside clients
        services.AddHttpClient();
        // Named client for LLM calls to allow separate tuning if needed
        services.AddHttpClient("llm");
        services.AddScoped<ILLMClientFactory, LLMClientFactory>();
        services.AddScoped<IImageClient, OpenAIImageClient>();
        services.AddScoped<IAgentService, AgentService>();
        return services;
    }

    public static IServiceCollection AddCognitionTools(this IServiceCollection services)
    {
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        services.AddScoped<IToolDispatcher, ToolDispatcher>();

        // Auto-register all ITool implementations in this assembly so
        // they can be resolved by type name (ClassPath) via DI.
        var toolInterface = typeof(ITool);
        var assembly = typeof(ITool).Assembly;
        foreach (var t in assembly.GetTypes())
        {
            if (t.IsAbstract || t.IsInterface) continue;
            if (toolInterface.IsAssignableFrom(t))
            {
                services.AddScoped(t);
            }
        }

        return services;
    }

    // Per-request resiliency is handled in the client implementations for portability.
}
