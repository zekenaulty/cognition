using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Clients.Tools.Planning;
using Cognition.Clients.Tools.Planning.Fiction;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Prompts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cognition.Clients.Tests.Tools.Planning;

public class PlannerInfrastructureTests
{
    [Fact]
    public async Task PlannerTranscriptStore_persists_execution_record()
    {
        await using var db = CreateDbContext();
        var store = new PlannerTranscriptStore(db, NullLogger<PlannerTranscriptStore>.Instance);

        var services = new ServiceCollection().BuildServiceProvider();
        var toolContext = new ToolContext(
            AgentId: Guid.NewGuid(),
            ConversationId: Guid.NewGuid(),
            PersonaId: null,
            Services: services,
            Ct: CancellationToken.None);

        var plannerContext = PlannerContext.FromToolContext(
            toolContext,
            toolId: Guid.NewGuid(),
            scopePath: null,
            primaryAgentId: Guid.NewGuid(),
            conversationState: new Dictionary<string, object?>
            {
                ["planId"] = Guid.NewGuid(),
                ["branch"] = "main"
            },
            environment: "test");

        var metadata = PlannerMetadata.Create(
            name: "test-planner",
            description: "test",
            capabilities: new[] { "planning" },
            steps: new[] { new PlannerStepDescriptor("step-1", "Step 1") });

        var result = PlannerResult.Success()
            .AddArtifact("echo", "value")
            .AddStep(new PlannerStepRecord("step-1", PlannerStepStatus.Completed, new Dictionary<string, object?>(), TimeSpan.FromMilliseconds(10)))
            .AddTranscript(new PlannerTranscriptEntry(DateTime.UtcNow, "system", "prompt"))
            .AddTranscript(new PlannerTranscriptEntry(DateTime.UtcNow, "assistant", "response", new Dictionary<string, object?>
            {
                ["score"] = 0.9
            }))
            .AddMetric("durationMs", 10d)
            .AddDiagnostics("summary", "ok");

        await store.StoreAsync(plannerContext, metadata, result, CancellationToken.None);

        var execution = await db.PlannerExecutions.SingleAsync();
        execution.PlannerName.Should().Be(metadata.Name);
        execution.Outcome.Should().Be(result.Outcome.ToString());
        execution.Artifacts.Should().ContainKey("echo");
        execution.Diagnostics.Should().ContainKey("summary");
        execution.Metrics.Should().ContainKey("durationMs");
        execution.Transcript.Should().HaveCount(2);
        execution.ConversationState.Should().ContainKey("planId");
    }

    [Fact]
    public async Task PlannerTemplateRepository_returns_active_template()
    {
        await using var db = CreateDbContext();
        db.PromptTemplates.Add(new PromptTemplate
        {
            Id = Guid.NewGuid(),
            Name = "planner.fiction.vision",
            PromptType = Cognition.Data.Relational.Modules.Common.PromptType.SystemInstruction,
            Template = "Hello {{projectTitle}}",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var repository = new PlannerTemplateRepository(db, NullLogger<PlannerTemplateRepository>.Instance);

        var template = await repository.GetTemplateAsync("planner.fiction.vision", CancellationToken.None);
        template.Should().Be("Hello {{projectTitle}}");
    }

    [Fact]
    public async Task VisionPlannerTool_uses_template_repository_when_available()
    {
        var agentService = new StubAgentService();
        var telemetry = new SpyPlannerTelemetry();
        var transcriptStore = new NullPlannerTranscriptStore();
        var template = @"You are working on {{projectTitle}} on branch {{branch}}.
Description: {{description}}
Logline: {{logline}}

Return minified JSON:
{
  ""authorSummary"": ""string describing the author persona voice, tone, pacing, and stylistic edges"",
  ""bookGoals"": [""goal 1"", ""goal 2"", ""goal 3""],
  ""planningBacklog"": [
    {
      ""id"": ""outline-core-conflicts"",
      ""description"": ""Define the headline conflicts and stakes"",
      ""status"": ""pending""
    }
  ],
  ""openQuestions"": [""unknown or risky assumptions""],
  ""worldSeeds"": [""worldbuilding seeds to expand later""]
}

Ensure backlog entries capture still-needed planner passes rather than a finished story outline.
Respond with JSON only.";
        var templateRepository = new StubTemplateRepository(template);
        var tool = new VisionPlannerTool(
            agentService,
            NullLoggerFactory.Instance,
            telemetry,
            transcriptStore,
            templateRepository);

        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            FictionProject = new FictionProject
            {
                Id = Guid.NewGuid(),
                Title = "Project Starfall",
                Logline = "An epic space opera."
            },
            Name = "Starfall Plan",
            Description = "A sweeping multi-act space saga."
        };

        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var personaId = Guid.NewGuid();

        var conversation = new Conversation
        {
            Id = conversationId,
            AgentId = agentId,
            Agent = new Agent
            {
                Id = agentId,
                PersonaId = personaId,
                Persona = new Persona
                {
                    Id = personaId,
                    Name = "Test Persona",
                    Nickname = "Test",
                    Role = "Assistant"
                }
            }
        };

        var executionContext = FictionPhaseExecutionContext.ForPlan(plan.Id, agentId, conversationId, branchSlug: "draft");

        var parameters = VisionPlannerParameters.Create(
            plan,
            conversation,
            executionContext,
            providerId: Guid.NewGuid(),
            modelId: Guid.NewGuid());

        var services = new ServiceCollection().BuildServiceProvider();
        var toolContext = new ToolContext(agentId, conversationId, personaId, services, CancellationToken.None);
        var plannerContext = PlannerContext.FromToolContext(toolContext, toolId: Guid.NewGuid(), scopePath: null, primaryAgentId: agentId);

        var result = await tool.PlanAsync(plannerContext, parameters, CancellationToken.None);

        agentService.Requests.Should().HaveCount(1);
        var prompt = agentService.Requests.Single().Prompt;
        prompt.Should().Contain("Project Starfall");
        prompt.Should().Contain("draft");
        prompt.Should().NotContain("{{projectTitle}}");
        prompt.Should().Contain("planningBacklog");
        prompt.Should().Contain("Ensure backlog entries capture still-needed planner passes");
        result.Artifacts.Should().ContainKey("prompt");
        ((string)result.Artifacts["prompt"]!).Should().Be(prompt);
        result.Backlog.Should().HaveCount(2);
        result.Backlog.Select(b => b.Id).Should().Contain("outline-arcs");
        result.Backlog.Should().OnlyContain(b => b.Status == PlannerBacklogStatus.Pending);
    }

    private static CognitionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new PlannerTestDbContext(options);
    }

    private sealed class StubTemplateRepository : IPlannerTemplateRepository
    {
        private readonly string _template;

        public StubTemplateRepository(string template)
        {
            _template = template;
        }

        public Task<string?> GetTemplateAsync(string templateId, CancellationToken ct)
            => Task.FromResult<string?>(_template);
    }

    private sealed record AgentRequest(
        Guid ConversationId,
        Guid AgentId,
        Guid ProviderId,
        Guid? ModelId,
        string Prompt);

    private sealed class StubAgentService : Cognition.Clients.Agents.IAgentService
    {
        public List<AgentRequest> Requests { get; } = new();
        public Task<string> AskAsync(Guid agentId, Guid providerId, Guid? modelId, string input, CancellationToken ct = default)
            => Task.FromResult(string.Empty);
        public Task<string> AskWithPlanAsync(Guid conversationId, Guid agentId, Guid providerId, Guid? modelId, string input, int minSteps, int maxSteps, CancellationToken ct = default)
            => Task.FromResult(string.Empty);
        public Task<string> AskWithToolsAsync(Guid agentId, Guid providerId, Guid? modelId, string input, CancellationToken ct = default)
            => Task.FromResult(string.Empty);

        public Task<(string Reply, Guid MessageId)> ChatAsync(Guid conversationId, Guid agentId, Guid providerId, Guid? modelId, string input, CancellationToken ct = default)
        {
            Requests.Add(new AgentRequest(conversationId, agentId, providerId, modelId, input));

            const string response = @"{
  ""authorSummary"": ""An accomplished storyteller weaving grand adventures."",
  ""bookGoals"": [
    ""Deliver a galaxy-spanning saga."",
    ""Explore the cost of ambition.""
  ],
  ""planningBacklog"": [
    {
      ""id"": ""outline-arcs"",
      ""description"": ""Draft the protagonist and antagonist arcs"",
      ""status"": ""pending"",
      ""outputs"": [""character-outline-v1""]
    },
    {
      ""id"": ""map-conflicts"",
      ""description"": ""Enumerate the primary conflicts and stakes"",
      ""status"": ""pending""
    }
  ],
  ""openQuestions"": [
    ""What catalyzes the inciting incident?""
  ]
}";
            return Task.FromResult((response, Guid.NewGuid()));
        }
    }

    private sealed class SpyPlannerTelemetry : IPlannerTelemetry
    {
        public List<PlannerTelemetryContext> Started { get; } = new();
        public List<(PlannerTelemetryContext Context, PlannerResult Result)> Completed { get; } = new();
        public List<(PlannerTelemetryContext Context, Exception Exception)> Failed { get; } = new();

        public void PlanStarted(PlannerTelemetryContext context) => Started.Add(context);
        public void PlanCompleted(PlannerTelemetryContext context, PlannerResult result) => Completed.Add((context, result));
        public void PlanFailed(PlannerTelemetryContext context, Exception exception) => Failed.Add((context, exception));
    }

    private sealed class PlannerTestDbContext : CognitionDbContext
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = false
        };

        public PlannerTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var allowed = new HashSet<Type>
            {
                typeof(Cognition.Data.Relational.Modules.Planning.PlannerExecution),
                typeof(PromptTemplate)
            };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (entityType.ClrType is not null && !allowed.Contains(entityType.ClrType))
                {
                    modelBuilder.Ignore(entityType.ClrType);
                }
            }

            modelBuilder.Entity<Cognition.Data.Relational.Modules.Planning.PlannerExecution>(b =>
            {
                b.Property(x => x.ConversationState)
                    .HasConversion(
                        v => Serialize(v),
                        v => Deserialize<Dictionary<string, object?>>(v));
                b.Property(x => x.Artifacts)
                    .HasConversion(
                        v => Serialize(v),
                        v => Deserialize<Dictionary<string, object?>>(v));
                b.Property(x => x.Diagnostics)
                    .HasConversion(
                        v => Serialize(v),
                        v => Deserialize<Dictionary<string, string>>(v));
                b.Property(x => x.Metrics)
                    .HasConversion(
                        v => Serialize(v),
                        v => Deserialize<Dictionary<string, double>>(v));
                b.Property(x => x.Transcript)
                    .HasConversion(
                        v => Serialize(v),
                        v => Deserialize<List<Cognition.Data.Relational.Modules.Planning.PlannerExecutionTranscriptEntry>>(v));
            });

            modelBuilder.Entity<PromptTemplate>(b =>
            {
                b.Property(x => x.Tokens)
                    .HasConversion(
                        v => Serialize(v),
                        v => Deserialize<Dictionary<string, object?>>(v));
            });
        }

        private static string? Serialize<T>(T value)
        {
            if (value is null)
            {
                return null;
            }

            return JsonSerializer.Serialize(value, SerializerOptions);
        }

        private static T? Deserialize<T>(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value, SerializerOptions);
        }
    }
}
