using Cognition.Clients.Images;
using Cognition.Clients.LLM;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Planning;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Clients.Tools.Fiction.Lifecycle;
using Cognition.Clients.Tools.Fiction.Authoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Cognition.Clients.Agents;
using Cognition.Clients.Prompts;
using Polly;
using System.Net;
using System.Net.Http;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Sandbox;

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
        services.AddScoped<ILLMProviderResolver, LLMProviderResolver>();
        // Embeddings client (OpenAI)
        services.AddHttpClient<IEmbeddingsClient, OpenAIEmbeddingsClient>();
        services.AddHttpClient<IImageClient, OpenAIImageClient>(c =>
        {
            // Allow long-running image generation (up to 10 minutes)
            c.Timeout = TimeSpan.FromMinutes(10);
        });
        services.AddScoped<IPromptBuilder, PromptBuilder>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IFictionPhaseRunner, VisionPlannerRunner>();
        services.AddScoped<IFictionPhaseRunner, WorldBibleManagerRunner>();
        services.AddScoped<IFictionPhaseRunner, IterativePlannerRunner>();
        services.AddScoped<IFictionPhaseRunner, ChapterArchitectRunner>();
        services.AddScoped<IFictionPhaseRunner, ScrollRefinerRunner>();
        services.AddScoped<IFictionPhaseRunner, SceneWeaverRunner>();
        services.AddScoped<ICharacterLifecycleService, CharacterLifecycleService>();
        services.AddOptions<AuthorPersonaOptions>();
        services.AddScoped<IAuthorPersonaRegistry, AuthorPersonaRegistry>();
        services.AddSingleton<IScopePathDiagnostics, ScopePathDiagnostics>();
        services.AddSingleton<IScopePathBuilder, ScopePathBuilder>();
        return services;
    }

    public static IServiceCollection AddCognitionTools(this IServiceCollection services)
    {
        services.AddSingleton<IPlannerTelemetry, LoggerPlannerTelemetry>();
        services.AddSingleton<IPlannerQuotaService, PlannerQuotaService>();
        services.AddOptions<ToolSandboxOptions>();
        services.AddSingleton<IConfigureOptions<ToolSandboxOptions>, ToolSandboxOptionsSetup>();
        services.AddSingleton<ISandboxPolicyEvaluator, SandboxPolicyEvaluator>();
        services.AddSingleton<ISandboxTelemetry, LoggerSandboxTelemetry>();
        services.AddOptions<ToolSandboxAlertOptions>();
        services.AddSingleton<ISandboxAlertPublisher, LoggerSandboxAlertPublisher>();
        services.AddSingleton<IToolSandboxApprovalQueue, InMemorySandboxApprovalQueue>();
        services.AddSingleton<IToolSandboxWorker, ProcessSandboxWorker>();
        services.AddScoped<IPlannerTranscriptStore, PlannerTranscriptStore>();
        services.AddScoped<IPlannerTemplateRepository, PlannerTemplateRepository>();
        services.AddSingleton<IPlannerCatalog, PlannerCatalog>();
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




