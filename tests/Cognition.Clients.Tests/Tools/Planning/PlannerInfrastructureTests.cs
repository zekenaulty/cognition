using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Scope;
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
using Microsoft.Extensions.Options;
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
        var scopePathBuilder = new ScopePathBuilder();
        var tool = new VisionPlannerTool(
            agentService,
            NullLoggerFactory.Instance,
            telemetry,
            transcriptStore,
            templateRepository,
            Options.Create(new PlannerCritiqueOptions()),
            scopePathBuilder);

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

    [Fact]
    public async Task IterativePlannerTool_uses_template_repository_when_available()
    {
        var agentService = new StubAgentService();
        var telemetry = new SpyPlannerTelemetry();
        var transcriptStore = new NullPlannerTranscriptStore();
        var template = @"You are iterating on {{planName}} at branch {{branch}} (iteration {{iterationIndex}}).

Existing passes:
{{existingPasses}}

Project details:
{{description}}";
        var templateRepository = new StubTemplateRepository(template);
        var scopePathBuilder = new ScopePathBuilder();
        var tool = new IterativePlannerTool(
            agentService,
            NullLoggerFactory.Instance,
            telemetry,
            transcriptStore,
            templateRepository,
            Options.Create(new PlannerCritiqueOptions()),
            scopePathBuilder);

        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            Name = "Starfall Plan",
            Description = "A sweeping multi-act space saga."
        };

        var executionContext = FictionPhaseExecutionContext.ForPlan(plan.Id, Guid.NewGuid(), Guid.NewGuid(), branchSlug: "feature/backlog")
            with { IterationIndex = 3 };

        var existingPasses = new List<IterativePlanPassSummary>
        {
            new(1, "Kickoff", "Defined high level stakes"),
            new(2, "Character polish", "Refined ensemble arcs")
        };

        var parameters = IterativePlannerParameters.Create(
            plan,
            executionContext,
            providerId: Guid.NewGuid(),
            modelId: Guid.NewGuid(),
            existingPasses);

        var services = new ServiceCollection().BuildServiceProvider();
        var toolContext = new ToolContext(executionContext.AgentId, executionContext.ConversationId, null, services, CancellationToken.None);
        var plannerContext = PlannerContext.FromToolContext(toolContext, toolId: Guid.NewGuid());

        var result = await tool.PlanAsync(plannerContext, parameters, CancellationToken.None);

        agentService.Requests.Should().HaveCount(1);
        var prompt = agentService.Requests.Single().Prompt;
        prompt.Should().Contain("Starfall Plan");
        prompt.Should().Contain("feature/backlog");
        prompt.Should().Contain("iteration 3");
        prompt.Should().Contain("Pass 1: Kickoff");
        prompt.Should().NotContain("{{planName}}");
        result.Artifacts.Should().ContainKey("existingPasses");
        result.Artifacts.Should().ContainKey("prompt");
        ((string)result.Artifacts["prompt"]!).Should().Be(prompt);
        telemetry.Started.Should().HaveCount(1);
        telemetry.Completed.Should().HaveCount(1);
    }

    [Fact]
    public async Task PlannerBase_requests_each_template_once()
    {
        var telemetry = new SpyPlannerTelemetry();
        var transcriptStore = new NullPlannerTranscriptStore();
        var templateRepository = new CountingTemplateRepository("planner.test.shared", "Shared template content.");
        var options = Options.Create(new PlannerCritiqueOptions());
        var scopePathBuilder = new ScopePathBuilder();
        var planner = new TemplateCountingPlanner(
            NullLoggerFactory.Instance,
            telemetry,
            transcriptStore,
            templateRepository,
            options,
            scopePathBuilder);

        var services = new ServiceCollection().BuildServiceProvider();
        var toolContext = new ToolContext(
            AgentId: Guid.NewGuid(),
            ConversationId: Guid.NewGuid(),
            PersonaId: null,
            Services: services,
            Ct: CancellationToken.None);
        var plannerContext = PlannerContext.FromToolContext(toolContext, toolId: Guid.NewGuid());

        var result = await planner.PlanAsync(plannerContext, new PlannerParameters(), CancellationToken.None);

        result.Outcome.Should().Be(PlannerOutcome.Success);
        templateRepository.GetCount("planner.test.shared").Should().Be(1);
    }

    private static CognitionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new PlannerTestDbContext(options);
    }

    private sealed class TemplateCountingPlanner : PlannerBase<PlannerParameters>
    {
        private static readonly PlannerMetadata MetadataDefinition = PlannerMetadata.Create(
            name: "Template Counting Planner",
            description: "Ensures templates are only requested once per plan.",
            steps: new[]
            {
                new PlannerStepDescriptor("step-1", "First Step", TemplateId: "planner.test.shared"),
                new PlannerStepDescriptor("step-2", "Second Step", TemplateId: "planner.test.shared")
            });

        public TemplateCountingPlanner(
            ILoggerFactory loggerFactory,
            IPlannerTelemetry telemetry,
            IPlannerTranscriptStore transcriptStore,
            IPlannerTemplateRepository templateRepository,
            IOptions<PlannerCritiqueOptions> critiqueOptions,
            IScopePathBuilder scopePathBuilder)
            : base(loggerFactory, telemetry, transcriptStore, templateRepository, critiqueOptions, scopePathBuilder)
        {
        }

        public override PlannerMetadata Metadata => MetadataDefinition;

        protected override Task<PlannerResult> ExecutePlanAsync(PlannerContext context, PlannerParameters parameters, CancellationToken ct)
        {
            var result = PlannerResult.Success()
                .AddStep(new PlannerStepRecord("step-1", PlannerStepStatus.Completed, new Dictionary<string, object?>(), TimeSpan.Zero))
                .AddStep(new PlannerStepRecord("step-2", PlannerStepStatus.Completed, new Dictionary<string, object?>(), TimeSpan.Zero));
            return Task.FromResult(result);
        }
    }

    private sealed class CountingTemplateRepository : IPlannerTemplateRepository
    {
        private readonly string _templateId;
        private readonly string _template;
        private readonly Dictionary<string, int> _counts = new(StringComparer.OrdinalIgnoreCase);

        public CountingTemplateRepository(string templateId, string template)
        {
            _templateId = templateId;
            _template = template;
        }

        public Task<string?> GetTemplateAsync(string templateId, CancellationToken ct)
        {
            _counts.TryGetValue(templateId, out var value);
            _counts[templateId] = value + 1;
            if (string.Equals(templateId, _templateId, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<string?>(_template);
            }

            return Task.FromResult<string?>(null);
        }

        public int GetCount(string templateId)
        {
            return _counts.TryGetValue(templateId, out var value) ? value : 0;
        }
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

            if (input.Contains("storyAdjustments", StringComparison.OrdinalIgnoreCase) ||
                input.Contains("iteration", StringComparison.OrdinalIgnoreCase))
            {
                const string iterative = @"{
  ""storyAdjustments"": [
    ""Deepen the political stakes on the orbital council"",
    ""Seed foreshadowing for the rival faction's betrayal""
  ],
  ""characterPriorities"": [
    ""Clarify the captain's flaw around delegation"",
    ""Raise the navigator's emotional cost for the plan""
  ],
  ""locationNotes"": [
    ""Expand the comet sanctuary sensory detail"",
    ""Document the refugee flotilla morale conditions""
  ],
  ""systemsConsiderations"": [
    ""Ensure the quantum sail behaves consistently with prior chapters""
  ],
  ""risks"": [
    ""Iteration may feel repetitive without new stakes"",
    ""Antagonist reveal needs clearer timing""
  ]
}";
                return Task.FromResult((iterative, Guid.NewGuid()));
            }

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
        public List<(PlannerTelemetryContext Context, PlannerQuotaDecision Decision)> Throttled { get; } = new();
        public List<(PlannerTelemetryContext Context, PlannerQuotaDecision Decision)> Rejected { get; } = new();

        public Task PlanStartedAsync(PlannerTelemetryContext context, CancellationToken ct)
        {
            Started.Add(context);
            return Task.CompletedTask;
        }

        public Task PlanCompletedAsync(PlannerTelemetryContext context, PlannerResult result, CancellationToken ct)
        {
            Completed.Add((context, result));
            return Task.CompletedTask;
        }

        public Task PlanFailedAsync(PlannerTelemetryContext context, Exception exception, CancellationToken ct)
        {
            Failed.Add((context, exception));
            return Task.CompletedTask;
        }

        public Task PlanThrottledAsync(PlannerTelemetryContext context, PlannerQuotaDecision decision, CancellationToken ct)
        {
            Throttled.Add((context, decision));
            return Task.CompletedTask;
        }

        public Task PlanRejectedAsync(PlannerTelemetryContext context, PlannerQuotaDecision decision, CancellationToken ct)
        {
            Rejected.Add((context, decision));
            return Task.CompletedTask;
        }
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
                        v => Deserialize<Dictionary<string, JsonElement>>(v));
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
