using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Planning;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Planning;
using Cognition.Clients.Scope;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Planning;
using Cognition.Data.Relational.Modules.Prompts;
using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Config;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Images;
using Cognition.Data.Relational.Modules.Knowledge;
using Cognition.Data.Relational.Modules.LLM;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Tools;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cognition.Api.Tests.Infrastructure;

public class PlannerHealthAlertFanoutTests
{
    [Fact]
    public async Task GetReportAsync_publishes_backlog_alerts_to_publisher()
    {
        await using var db = CreateDbContext();

        // Seed an active template so planner metadata passes.
        db.PromptTemplates.Add(new PromptTemplate
        {
            Id = Guid.NewGuid(),
            Name = TestPlannerTool.TemplateId,
            PromptType = PromptType.SystemInstruction,
            Template = "You are a planner.",
            IsActive = true
        });

        var planId = Guid.NewGuid();
        db.FictionPlans.Add(new FictionPlan
        {
            Id = planId,
            FictionProjectId = Guid.NewGuid(),
            Name = "Fanout Plan",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-6)
        });

        db.FictionPlanBacklogItems.Add(new FictionPlanBacklogItem
        {
            Id = Guid.NewGuid(),
            FictionPlanId = planId,
            BacklogId = "stale-item",
            Description = "Stale backlog",
            Status = FictionPlanBacklogStatus.InProgress,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-5),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-3),
            InProgressAtUtc = DateTime.UtcNow.AddHours(-3)
        });

        await db.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<TestPlannerTool>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var registry = new StubToolRegistry(typeof(TestPlannerTool));
        var alertPublisher = new TestAlertPublisher();
        var health = new PlannerHealthService(db, registry, scope.ServiceProvider, alertPublisher, NullLogger<PlannerHealthService>.Instance);

        var report = await health.GetReportAsync(CancellationToken.None);

        report.Alerts.Should().Contain(a => a.Id.StartsWith("backlog:stale", StringComparison.OrdinalIgnoreCase));
        alertPublisher.Published.Should().HaveCount(1);
        alertPublisher.Published[0].Should().Contain(a => a.Id.StartsWith("backlog:stale", StringComparison.OrdinalIgnoreCase));
    }

    private static CognitionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new PlannerHealthDbContext(options);
    }

    private sealed class PlannerHealthDbContext : CognitionDbContext
    {
        public PlannerHealthDbContext(DbContextOptions<CognitionDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Agent>(b => b.Ignore(a => a.State));
            modelBuilder.Entity<AgentToolBinding>(b => b.Ignore(a => a.Config));
            modelBuilder.Entity<DataSource>(b => b.Ignore(d => d.Config));
            modelBuilder.Entity<SystemVariable>(b => b.Ignore(s => s.Value));
            modelBuilder.Entity<Conversation>(b => b.Ignore(c => c.Metadata));
            modelBuilder.Entity<FictionChapterBlueprint>(b => b.Ignore(f => f.Structure));
            modelBuilder.Entity<FictionChapterScene>(b => b.Ignore(f => f.Metadata));
            modelBuilder.Entity<FictionChapterScroll>(b => b.Ignore(f => f.Metadata));
            modelBuilder.Entity<FictionChapterSection>(b => b.Ignore(f => f.Metadata));
            modelBuilder.Entity<FictionPlanCheckpoint>(b => b.Ignore(c => c.Progress));
            modelBuilder.Entity<FictionPlanPass>(b => b.Ignore(p => p.Metadata));
            modelBuilder.Entity<FictionPlanTranscript>(b => b.Ignore(t => t.Metadata));
            modelBuilder.Entity<FictionStoryMetric>(b => b.Ignore(m => m.Data));
            modelBuilder.Entity<ImageAsset>(b => b.Ignore(i => i.Metadata));
            modelBuilder.Entity<ImageStyle>(b => b.Ignore(i => i.Defaults));
            modelBuilder.Entity<KnowledgeEmbedding>(b =>
            {
                b.Ignore(k => k.Metadata);
                b.Ignore(k => k.ScopeSegments);
            });
            modelBuilder.Entity<KnowledgeItem>(b => b.Ignore(k => k.Properties));
            modelBuilder.Entity<Model>(b => b.Ignore(m => m.Metadata));
            modelBuilder.Entity<PersonaDream>(b => b.Ignore(p => p.Properties));
            modelBuilder.Entity<PersonaEvent>(b => b.Ignore(p => p.Properties));
            modelBuilder.Entity<PersonaEventType>(b => b.Ignore(p => p.Properties));
            modelBuilder.Entity<PersonaMemory>(b => b.Ignore(p => p.Properties));
            modelBuilder.Entity<PersonaMemoryType>(b => b.Ignore(p => p.Properties));
            modelBuilder.Entity<PromptTemplate>(b => b.Ignore(p => p.Tokens));
            modelBuilder.Entity<Tool>(b => b.Ignore(t => t.Metadata));
            modelBuilder.Entity<ToolParameter>(b =>
            {
                b.Ignore(t => t.DefaultValue);
                b.Ignore(t => t.Options);
            });
            modelBuilder.Entity<ToolExecutionLog>(b =>
            {
                b.Ignore(l => l.Request);
                b.Ignore(l => l.Response);
            });
            modelBuilder.Entity<PlannerExecution>(b =>
            {
                b.Ignore(p => p.Artifacts);
                b.Ignore(p => p.ConversationState);
                b.Ignore(p => p.Diagnostics);
                b.Ignore(p => p.Transcript);
                b.Ignore(p => p.Metrics);
            });
        }
    }

    private sealed class StubToolRegistry : IToolRegistry
    {
        private readonly Type _type;
        public StubToolRegistry(Type type) => _type = type;

        public bool TryResolveByClassPath(string classPath, out Type type)
        {
            type = _type;
            return true;
        }

        public bool IsKnownClassPath(string classPath) => true;
        public IReadOnlyDictionary<string, Type> Map => new Dictionary<string, Type>(StringComparer.Ordinal);
        public IReadOnlyCollection<Type> GetPlannersByCapability(string capability) => new[] { _type };
    }

    private sealed class TestPlannerTool : PlannerBase<PlannerParameters>
    {
        public const string TemplateId = "planner.test.template";
        public static string PlannerName => "Test Planner";

        public TestPlannerTool(
            ILoggerFactory loggerFactory,
            IPlannerTelemetry telemetry,
            IPlannerTranscriptStore transcriptStore,
            IPlannerTemplateRepository templateRepository,
            IOptions<PlannerCritiqueOptions> critiqueOptions,
            IScopePathBuilder scopePathBuilder)
            : base(loggerFactory, telemetry, transcriptStore, templateRepository, critiqueOptions, scopePathBuilder)
        {
        }

        public override PlannerMetadata Metadata { get; } = PlannerMetadata.Create(
            name: PlannerName,
            description: "Test planner",
            capabilities: new[] { "planning", "test" },
            steps: new[] { new PlannerStepDescriptor("step", "Step", TemplateId) });

        protected override void ValidateInputs(PlannerParameters parameters) { }

        protected override Task<PlannerResult> ExecutePlanAsync(PlannerContext context, PlannerParameters parameters, CancellationToken ct)
        {
            var result = PlannerResult.Success().AddStep(new PlannerStepRecord("step", PlannerStepStatus.Completed, new Dictionary<string, object?>(), TimeSpan.Zero));
            return Task.FromResult(result);
        }
    }

    private sealed class TestAlertPublisher : IPlannerAlertPublisher
    {
        public List<IReadOnlyList<PlannerHealthAlert>> Published { get; } = new();
        public Task PublishAsync(IReadOnlyList<PlannerHealthAlert> alerts, CancellationToken ct)
        {
            Published.Add(alerts);
            return Task.CompletedTask;
        }
    }
}
