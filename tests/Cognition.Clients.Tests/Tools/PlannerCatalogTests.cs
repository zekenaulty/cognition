using System;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Agents;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Planning;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Planning.Fiction;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Cognition.Clients.Tests.Tools;

public class PlannerCatalogTests
{
    [Fact]
    public void GetByCapability_returns_registered_planner_metadata()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IPlannerTelemetry, LoggerPlannerTelemetry>()
            .AddSingleton<IPlannerTranscriptStore, NullPlannerTranscriptStore>()
            .AddSingleton<IPlannerTemplateRepository, NullPlannerTemplateRepository>()
            .AddSingleton<IAgentService, FakeAgentService>()
            .AddSingleton<IScopePathBuilder, ScopePathBuilder>()
            .AddSingleton<VisionPlannerTool>()
            .BuildServiceProvider();

        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var registry = new ToolRegistry();
        var logger = services.GetRequiredService<ILogger<PlannerCatalog>>();
        var catalog = new PlannerCatalog(registry, scopeFactory, logger);

        var planners = catalog.GetByCapability("vision");

        planners.Should().NotBeEmpty();
        planners.Should().ContainSingle(p => p.Metadata.Name == "Vision Planner");
    }

    [Fact]
    public void TryResolveByName_returns_null_for_unknown_planners()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IPlannerTelemetry, LoggerPlannerTelemetry>()
            .AddSingleton<IPlannerTranscriptStore, NullPlannerTranscriptStore>()
            .AddSingleton<IPlannerTemplateRepository, NullPlannerTemplateRepository>()
            .AddSingleton<IAgentService, FakeAgentService>()
            .AddSingleton<IScopePathBuilder, ScopePathBuilder>()
            .AddSingleton<VisionPlannerTool>()
            .BuildServiceProvider();

        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var registry = new ToolRegistry();
        var logger = services.GetRequiredService<ILogger<PlannerCatalog>>();
        var catalog = new PlannerCatalog(registry, scopeFactory, logger);

        catalog.TryResolveByName("missing").Should().BeNull();
    }

    private sealed class FakeAgentService : IAgentService
    {
        public Task<string> AskAsync(Guid agentId, Guid providerId, Guid? modelId, string input, CancellationToken ct = default)
            => Task.FromResult(string.Empty);

        public Task<string> AskWithPlanAsync(Guid conversationId, Guid agentId, Guid providerId, Guid? modelId, string input, int minSteps, int maxSteps, CancellationToken ct = default)
            => Task.FromResult(string.Empty);

        public Task<string> AskWithToolsAsync(Guid agentId, Guid providerId, Guid? modelId, string input, CancellationToken ct = default)
            => Task.FromResult(string.Empty);

        public Task<(string Reply, Guid MessageId)> ChatAsync(Guid conversationId, Guid agentId, Guid providerId, Guid? modelId, string input, CancellationToken ct = default)
            => Task.FromResult((string.Empty, Guid.Empty));
    }
}
