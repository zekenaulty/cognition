using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Retrieval;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Planning;
using Cognition.Contracts;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Tools;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cognition.Clients.Tests.Tools;

public class ToolDispatcherScopeTests
{
    [Fact]
    public async Task ExecuteAsync_passes_scope_to_tool_and_logs_agent_id()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new ToolDispatcherDbContext(options);
        var toolId = Guid.NewGuid();
        var classPath = ToolClassPath<TestScopeTool>();

        db.Tools.Add(new Tool
        {
            Id = toolId,
            Name = "test",
            ClassPath = classPath,
            Parameters = new List<ToolParameter>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "providerId",
                    Type = "guid",
                    Direction = ToolParamDirection.Input
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "modelId",
                    Type = "guid",
                    Direction = ToolParamDirection.Input
                }
            }
        });
        await db.SaveChangesAsync();

        var toolSpy = new TestScopeTool();
        var services = new ServiceCollection()
            .AddSingleton(toolSpy)
            .AddSingleton<ITool>(toolSpy)
            .BuildServiceProvider();

        var registry = new SingleToolRegistry(classPath, typeof(TestScopeTool));
        var scopePathBuilder = new ScopePathBuilder();
        var telemetry = new SpyPlannerTelemetry();
        var quotaService = new AllowAllPlannerQuotaService();
        var dispatcher = new ToolDispatcher(db, services, registry, NullLogger<ToolDispatcher>.Instance, scopePathBuilder, quotaService, telemetry);

        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var personaId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();

        var ctx = new ToolContext(agentId, conversationId, personaId, services, CancellationToken.None);
        var args = new Dictionary<string, object?>
        {
            ["providerId"] = providerId,
            ["modelId"] = modelId
        };

        var (ok, _, error) = await dispatcher.ExecuteAsync(toolId, ctx, args);

        error.Should().BeNull("dispatcher error: {0}", error);
        ok.Should().BeTrue();

        toolSpy.Invocations.Should().HaveCount(1);
        var captured = toolSpy.Invocations.Single();
        captured.Context.AgentId.Should().Be(agentId);
        captured.Context.ConversationId.Should().Be(conversationId);
        captured.Context.PersonaId.Should().Be(personaId);
        captured.Args["providerId"].Should().Be(providerId);
        captured.Args["modelId"].Should().Be(modelId);

        var log = await db.ToolExecutionLogs.SingleAsync();
        log.AgentId.Should().Be(agentId);
        log.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_propagates_scope_to_retrieval_dependency()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new ToolDispatcherDbContext(options);
        var toolId = Guid.NewGuid();
        var classPath = ToolClassPath<RememberDispatchTool>();

        db.Tools.Add(new Tool
        {
            Id = toolId,
            Name = "remember",
            ClassPath = classPath,
            Parameters = new List<ToolParameter>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ToolId = toolId,
                    Name = "text",
                    Type = "string",
                    Direction = ToolParamDirection.Input,
                    Required = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ToolId = toolId,
                    Name = "providerId",
                    Type = "guid",
                    Direction = ToolParamDirection.Input,
                    Required = false
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ToolId = toolId,
                    Name = "modelId",
                    Type = "guid",
                    Direction = ToolParamDirection.Input,
                    Required = false
                }
            }
        });
        await db.SaveChangesAsync();

        var retrieval = new RecordingRetrievalService();
        var services = new ServiceCollection()
            .AddSingleton(retrieval)
            .AddSingleton<IRetrievalService>(retrieval)
            .AddSingleton<RememberDispatchTool>()
            .AddSingleton<ITool>(sp => sp.GetRequiredService<RememberDispatchTool>())
            .BuildServiceProvider();

        var registry = new SingleToolRegistry(classPath, typeof(RememberDispatchTool));
        var scopePathBuilder = new ScopePathBuilder();
        var telemetry = new SpyPlannerTelemetry();
        var quotaService = new AllowAllPlannerQuotaService();
        var dispatcher = new ToolDispatcher(db, services, registry, NullLogger<ToolDispatcher>.Instance, scopePathBuilder, quotaService, telemetry);

        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var ctx = new ToolContext(agentId, conversationId, PersonaId: null, services, CancellationToken.None);
        var args = new Dictionary<string, object?>
        {
            ["text"] = "scope aware remember",
            ["providerId"] = Guid.NewGuid(),
            ["modelId"] = Guid.NewGuid()
        };

        var (ok, _, error) = await dispatcher.ExecuteAsync(toolId, ctx, args);

        error.Should().BeNull("dispatcher error: {0}", error);
        ok.Should().BeTrue();

        retrieval.Writes.Should().ContainSingle();
        var recorded = retrieval.Writes.Single();
        recorded.AgentId.Should().Be(agentId);
        recorded.ConversationId.Should().Be(conversationId);
    }

    [Fact]
    public async Task ExecutePlannerAsync_emits_telemetry_events()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new ToolDispatcherDbContext(options);
        var toolId = Guid.NewGuid();
        var classPath = ToolClassPath<TestPlannerTool>();

        db.Tools.Add(new Tool
        {
            Id = toolId,
            Name = "planner",
            ClassPath = classPath,
            Parameters = new List<ToolParameter>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ToolId = toolId,
                    Name = "RequiredText",
                    Type = "string",
                    Direction = ToolParamDirection.Input,
                    Required = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ToolId = toolId,
                    Name = "providerId",
                    Type = "guid",
                    Direction = ToolParamDirection.Input,
                    Required = false
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ToolId = toolId,
                    Name = "modelId",
                    Type = "guid",
                    Direction = ToolParamDirection.Input,
                    Required = false
                }
            }
        });
        await db.SaveChangesAsync();

        var telemetry = new SpyPlannerTelemetry();
        var services = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(new LoggerFactory())
            .AddSingleton<IPlannerTelemetry>(telemetry)
            .AddSingleton<IPlannerTranscriptStore, NullPlannerTranscriptStore>()
            .AddSingleton<IPlannerTemplateRepository, NullPlannerTemplateRepository>()
            .AddSingleton<IOptions<PlannerCritiqueOptions>>(Options.Create(new PlannerCritiqueOptions()))
            .AddSingleton<IScopePathBuilder, ScopePathBuilder>()
            .AddSingleton<TestPlannerTool>()
            .BuildServiceProvider();

        var planner = services.GetRequiredService<TestPlannerTool>();
        var registry = new SingleToolRegistry(classPath, typeof(TestPlannerTool));
        var scopePathBuilder = services.GetRequiredService<IScopePathBuilder>();
        var quotaService = new AllowAllPlannerQuotaService();
        var dispatcher = new ToolDispatcher(db, services, registry, NullLogger<ToolDispatcher>.Instance, scopePathBuilder, quotaService, telemetry);

        var conversationId = Guid.NewGuid();
        var personaId = Guid.NewGuid();
        var toolContext = new ToolContext(null, conversationId, personaId, services, CancellationToken.None);
        var plannerContext = PlannerContext.FromToolContext(toolContext, toolId: toolId);
        var parameters = new PlannerParameters(new Dictionary<string, object?>
        {
            ["RequiredText"] = "hello world",
            ["providerId"] = Guid.NewGuid(),
            ["modelId"] = Guid.NewGuid()
        });

        var (ok, result, error) = await dispatcher.ExecutePlannerAsync(toolId, plannerContext, parameters, log: true);

        error.Should().BeNull("dispatcher error: {0}", error);
        ok.Should().BeTrue();
        result.Should().NotBeNull();

        planner.Contexts.Should().HaveCount(1);
        var recordedContext = planner.Contexts.Single();
        recordedContext.ScopePath.Should().NotBeNull();
        var expectedScope = scopePathBuilder.Build(new ScopeToken(null, null, personaId, null, conversationId, null, null)).Canonical;
        recordedContext.ScopePath!.Canonical.Should().Be(expectedScope);
        telemetry.Started.Should().HaveCount(1);
        telemetry.Completed.Should().HaveCount(1);
        telemetry.Failed.Should().BeEmpty();

        var logEntry = await db.ToolExecutionLogs.SingleAsync();
        logEntry.Success.Should().BeTrue();
        logEntry.ToolId.Should().Be(toolId);
    }

    [Fact]
    public async Task ExecuteAsync_propagates_cancellation_from_tool_context()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new ToolDispatcherDbContext(options);
        var toolId = Guid.NewGuid();
        var classPath = ToolClassPath<TestScopeTool>();

        db.Tools.Add(new Tool
        {
            Id = toolId,
            Name = "cancel-test",
            ClassPath = classPath,
            Parameters = new List<ToolParameter>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ToolId = toolId,
                    Name = "providerId",
                    Type = "guid",
                    Direction = ToolParamDirection.Input,
                    Required = false
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ToolId = toolId,
                    Name = "modelId",
                    Type = "guid",
                    Direction = ToolParamDirection.Input,
                    Required = false
                }
            }
        });
        await db.SaveChangesAsync();

        var services = new ServiceCollection()
            .AddSingleton<TestScopeTool>()
            .AddSingleton<ITool>(sp => sp.GetRequiredService<TestScopeTool>())
            .BuildServiceProvider();

        var registry = new SingleToolRegistry(classPath, typeof(TestScopeTool));
        var dispatcher = new ToolDispatcher(
            db,
            services,
            registry,
            NullLogger<ToolDispatcher>.Instance,
            new ScopePathBuilder(),
            new AllowAllPlannerQuotaService(),
            new SpyPlannerTelemetry());

        var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        var ctx = new ToolContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), services, cancelled.Token);

        Func<Task> act = async () =>
            await dispatcher.ExecuteAsync(toolId, ctx, new Dictionary<string, object?>(), log: false);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecutePlannerAsync_emits_failure_telemetry_on_exception()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new ToolDispatcherDbContext(options);
        var toolId = Guid.NewGuid();
        var classPath = ToolClassPath<TestPlannerTool>();

        db.Tools.Add(new Tool
        {
            Id = toolId,
            Name = "planner",
            ClassPath = classPath,
            Parameters = new List<ToolParameter>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ToolId = toolId,
                    Name = "RequiredText",
                    Type = "string",
                    Direction = ToolParamDirection.Input,
                    Required = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ToolId = toolId,
                    Name = "providerId",
                    Type = "guid",
                    Direction = ToolParamDirection.Input,
                    Required = false
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ToolId = toolId,
                    Name = "modelId",
                    Type = "guid",
                    Direction = ToolParamDirection.Input,
                    Required = false
                }
            }
        });
        await db.SaveChangesAsync();

        var telemetry = new SpyPlannerTelemetry();
        var services = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(new LoggerFactory())
            .AddSingleton<IPlannerTelemetry>(telemetry)
            .AddSingleton<IPlannerTranscriptStore, NullPlannerTranscriptStore>()
            .AddSingleton<IPlannerTemplateRepository, NullPlannerTemplateRepository>()
            .AddSingleton<IOptions<PlannerCritiqueOptions>>(Options.Create(new PlannerCritiqueOptions()))
            .AddSingleton<IScopePathBuilder, ScopePathBuilder>()
            .AddSingleton<TestPlannerTool>()
            .BuildServiceProvider();

        var planner = services.GetRequiredService<TestPlannerTool>();
        planner.ShouldFail = true;

        var registry = new SingleToolRegistry(classPath, typeof(TestPlannerTool));
        var scopePathBuilder = services.GetRequiredService<IScopePathBuilder>();
        var quotaService = new AllowAllPlannerQuotaService();
        var dispatcher = new ToolDispatcher(db, services, registry, NullLogger<ToolDispatcher>.Instance, scopePathBuilder, quotaService, telemetry);

        var conversationId = Guid.NewGuid();
        var personaId = Guid.NewGuid();
        var toolContext = new ToolContext(null, conversationId, personaId, services, CancellationToken.None);
        var plannerContext = PlannerContext.FromToolContext(toolContext, toolId: toolId);
        var parameters = new PlannerParameters(new Dictionary<string, object?>
        {
            ["RequiredText"] = "boom",
            ["providerId"] = Guid.NewGuid(),
            ["modelId"] = Guid.NewGuid()
        });

        var (ok, result, error) = await dispatcher.ExecutePlannerAsync(toolId, plannerContext, parameters, log: true);

        ok.Should().BeFalse();
        result.Should().BeNull();
        error.Should().NotBeNull("dispatcher error: {0}", error);

        planner.Contexts.Should().HaveCount(1);
        var recordedContext = planner.Contexts.Single();
        recordedContext.ScopePath.Should().NotBeNull();
        var expectedScope = scopePathBuilder.Build(new ScopeToken(null, null, personaId, null, conversationId, null, null)).Canonical;
        recordedContext.ScopePath!.Canonical.Should().Be(expectedScope);
        telemetry.Started.Should().HaveCount(1);
        telemetry.Completed.Should().BeEmpty();
        telemetry.Failed.Should().HaveCount(1);

        var logEntry = await db.ToolExecutionLogs.SingleAsync();
        logEntry.Success.Should().BeFalse();
        logEntry.Error.Should().NotBeNull();
    }

    private static string ToolClassPath<T>() where T : ITool
        => $"{typeof(T).FullName}, {typeof(T).Assembly.GetName().Name}";

    private sealed class SingleToolRegistry : IToolRegistry
    {
        private readonly string _classPath;
        private readonly Type _type;

        public SingleToolRegistry(string classPath, Type type)
        {
            _classPath = classPath;
            _type = type;
            Map = new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                { classPath, type }
            };
        }

        public bool TryResolveByClassPath(string classPath, out Type type)
        {
            if (string.Equals(classPath, _classPath, StringComparison.Ordinal))
            {
                type = _type;
                return true;
            }

            type = null!;
            return false;
        }

        public bool IsKnownClassPath(string classPath) => string.Equals(classPath, _classPath, StringComparison.Ordinal);

        public IReadOnlyDictionary<string, Type> Map { get; }

        public IReadOnlyCollection<Type> GetPlannersByCapability(string capability)
        {
            if (typeof(IPlannerTool).IsAssignableFrom(_type) && !string.IsNullOrWhiteSpace(capability))
            {
                return new[] { _type };
            }
            return Array.Empty<Type>();
        }
    }

    private sealed class TestScopeTool : ITool
    {
        public string Name => "TestScope";
        public string ClassPath => ToolClassPath<TestScopeTool>();

        public List<(ToolContext Context, IDictionary<string, object?> Args)> Invocations { get; } = new();

        public Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
        {
            Invocations.Add((ctx, new Dictionary<string, object?>(args)));
            return Task.FromResult<object?>(new Dictionary<string, object?>
            {
                ["ok"] = true,
                ["agent"] = ctx.AgentId
            });
        }
    }

    private sealed class RememberDispatchTool : ITool
    {
        private readonly IRetrievalService _retrieval;

        public RememberDispatchTool(IRetrievalService retrieval) => _retrieval = retrieval;

        public string Name => "RememberDispatch";
        public string ClassPath => ToolClassPath<RememberDispatchTool>();

        public Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
        {
            var text = args.TryGetValue("text", out var raw) ? raw?.ToString() ?? string.Empty : string.Empty;
            var scope = new ScopeToken(null, null, ctx.PersonaId, ctx.AgentId, ctx.ConversationId, null, null);
            return ExecuteInternalAsync(scope, text, ctx.Ct);
        }

        private async Task<object?> ExecuteInternalAsync(ScopeToken scope, string text, CancellationToken ct)
        {
            await _retrieval.WriteAsync(scope, text, metadata: null, ct).ConfigureAwait(false);
            return new { ok = true };
        }
    }

    private sealed class TestPlannerParameters : PlannerParameters
    {
        public TestPlannerParameters(IDictionary<string, object?> values) : base(values)
        {
        }

        public string RequiredText => Get<string>("RequiredText") ?? string.Empty;
    }

    private sealed class TestPlannerTool : PlannerBase<TestPlannerParameters>
    {
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
            name: "Test Planner",
            description: "Planner used for dispatcher telemetry tests.",
            capabilities: new[] { "planning", "test" },
            steps: new[] { new PlannerStepDescriptor("step", "Step") });

        public bool ShouldFail { get; set; }
        public List<PlannerContext> Contexts { get; } = new();

        protected override void ValidateInputs(TestPlannerParameters parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters.RequiredText))
            {
                throw new ArgumentException("RequiredText must be provided.", nameof(parameters));
            }
        }

        protected override Task<PlannerResult> ExecutePlanAsync(PlannerContext context, TestPlannerParameters parameters, CancellationToken ct)
        {
            Contexts.Add(context);
            if (ShouldFail)
            {
                throw new InvalidOperationException("Planner failure requested.");
            }

            var result = PlannerResult.Success()
                .AddArtifact("echo", parameters.RequiredText)
                .AddStep(new PlannerStepRecord("step", PlannerStepStatus.Completed, new Dictionary<string, object?>
                {
                    ["text"] = parameters.RequiredText
                }, TimeSpan.FromMilliseconds(5)));

            return Task.FromResult(result);
        }
    }

    private sealed class AllowAllPlannerQuotaService : IPlannerQuotaService
    {
        public PlannerQuotaDecision Evaluate(string plannerKey, PlannerQuotaContext context, Guid? personaId)
            => PlannerQuotaDecision.Allowed();
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

    private sealed class RecordingRetrievalService : IRetrievalService
    {
        public List<ScopeToken> Writes { get; } = new();

        public Task<IReadOnlyList<(string Id, string Content, double Score)>> SearchAsync(
            ScopeToken scope,
            string query,
            int k = 8,
            IDictionary<string, object?>? filters = null,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<(string, string, double)>>(Array.Empty<(string, string, double)>());

        public Task<bool> WriteAsync(
            ScopeToken scope,
            string content,
            IDictionary<string, object?>? metadata = null,
            CancellationToken ct = default)
        {
            Writes.Add(scope);
            return Task.FromResult(true);
        }
    }

    private sealed class ToolDispatcherDbContext : CognitionDbContext
    {
        public ToolDispatcherDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var allowed = new HashSet<Type>
            {
                typeof(Tool),
                typeof(ToolParameter),
                typeof(ToolExecutionLog),
                typeof(Cognition.Data.Relational.Modules.LLM.ClientProfile)
            };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (entityType.ClrType is not null && !allowed.Contains(entityType.ClrType))
                {
                    modelBuilder.Ignore(entityType.ClrType);
                }
            }

            modelBuilder.Entity<Cognition.Data.Relational.Modules.LLM.Model>(b => b.Ignore(m => m.Metadata));

            modelBuilder.Entity<Cognition.Data.Relational.Modules.LLM.ClientProfile>(b =>
            {
                b.Ignore(p => p.Provider);
                b.Ignore(p => p.Model);
                b.Ignore(p => p.ApiCredential);
            });

            modelBuilder.Entity<Tool>(b =>
            {
                b.HasMany(t => t.Parameters)
                    .WithOne(p => p.Tool)
                    .HasForeignKey(p => p.ToolId);
                b.Ignore(t => t.Tags);
                b.Ignore(t => t.Metadata);
                b.Ignore(t => t.Description);
                b.Ignore(t => t.Example);
            });

            modelBuilder.Entity<ToolParameter>(b =>
            {
                b.Ignore(p => p.DefaultValue);
                b.Ignore(p => p.Options);
            });

            modelBuilder.Entity<ToolExecutionLog>(b =>
            {
                b.Ignore(p => p.Tool);
                b.Ignore(p => p.Agent);
                b.Ignore(p => p.Request);
                b.Ignore(p => p.Response);
                b.Ignore(p => p.Error);
            });
        }
    }
}

