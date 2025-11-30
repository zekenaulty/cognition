using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Planning;
using Cognition.Clients.Tools.Sandbox;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Tools;
using Cognition.Testing.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cognition.Clients.Tests.Tools;

public class ToolDispatcherSandboxEnforceTests
{
    [Fact]
    public async Task ExecuteAsync_in_enforce_mode_uses_worker_and_skips_tool()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new ToolDispatcherDbContext(options);
        var toolId = Guid.NewGuid();
        var classPath = ToolClassPath<CountingTool>();

        db.Tools.Add(new Tool
        {
            Id = toolId,
            Name = "counting",
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

        var countingTool = new CountingTool();
        var services = new ServiceCollection()
            .AddSingleton(countingTool)
            .AddSingleton<ITool>(countingTool)
            .BuildServiceProvider();

        var registry = new SingleToolRegistry(classPath, typeof(CountingTool));
        var scopePathBuilder = ScopePathBuilderTestHelper.CreateBuilder();
        var quotaService = new AllowAllPlannerQuotaService();
        var telemetry = new SpyPlannerTelemetry();
        var sandboxOptions = new ToolSandboxOptions
        {
            Mode = SandboxMode.Enforce,
            AllowedUnsafeToolIds = new[] { toolId }
        };
        var sandboxPolicy = new SandboxPolicyEvaluator(new OptionsMonitorStub<ToolSandboxOptions>(sandboxOptions), NullLogger<SandboxPolicyEvaluator>.Instance);
        var worker = new RecordingSandboxWorker();

        var dispatcher = new ToolDispatcher(
            db,
            services,
            registry,
            NullLogger<ToolDispatcher>.Instance,
            scopePathBuilder,
            quotaService,
            telemetry,
            sandboxPolicy,
            new LoggerSandboxTelemetry(NullLogger<LoggerSandboxTelemetry>.Instance),
            NullSandboxAlertPublisher.Instance,
            NullSandboxApprovalQueue.Instance,
            worker,
            new OptionsMonitorStub<ToolSandboxOptions>(sandboxOptions));

        var ctx = new ToolContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), services, CancellationToken.None);
        var args = new Dictionary<string, object?>
        {
            ["providerId"] = Guid.NewGuid(),
            ["modelId"] = Guid.NewGuid()
        };

        var (ok, _, error) = await dispatcher.ExecuteAsync(toolId, ctx, args, log: false);

        ok.Should().BeTrue(error);
        countingTool.Invocations.Should().Be(0, "enforce mode should use sandbox worker");
        worker.Invocations.Should().Be(1, "worker should run in enforce mode");
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
            Map = new Dictionary<string, Type>(StringComparer.Ordinal) { { classPath, type } };
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
        public IReadOnlyCollection<Type> GetPlannersByCapability(string capability) => Array.Empty<Type>();
    }

    private sealed class ToolDispatcherDbContext : CognitionDbContext
    {
        public ToolDispatcherDbContext(DbContextOptions<CognitionDbContext> options) : base(options) { }

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
                b.HasMany(t => t.Parameters).WithOne(p => p.Tool).HasForeignKey(p => p.ToolId);
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

    private sealed class CountingTool : ITool
    {
        public string Name => "Count";
        public string ClassPath => ToolClassPath<CountingTool>();
        public int Invocations { get; private set; }

        public Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
        {
            Invocations++;
            return Task.FromResult<object?>(new { ok = true });
        }
    }

    private sealed class RecordingSandboxWorker : IToolSandboxWorker
    {
        public int Invocations { get; private set; }

        public Task<ToolSandboxResult> ExecuteAsync(ToolSandboxWorkRequest request, CancellationToken ct)
        {
            Invocations++;
            return Task.FromResult(new ToolSandboxResult(true, new { sandboxed = true }, null));
        }
    }

    private sealed class OptionsMonitorStub<T> : IOptionsMonitor<T>
    {
        private readonly T _value;
        public OptionsMonitorStub(T value) => _value = value;
        public T CurrentValue => _value;
        public T Get(string? name) => _value;
        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }

    private sealed class AllowAllPlannerQuotaService : IPlannerQuotaService
    {
        public PlannerQuotaDecision Evaluate(string plannerKey, PlannerQuotaContext context, Guid? personaId)
            => PlannerQuotaDecision.Allowed();
    }

    private sealed class NullSandboxAlertPublisher : ISandboxAlertPublisher
    {
        public static readonly NullSandboxAlertPublisher Instance = new();
        public Task PublishAsync(SandboxDecision decision, Tool tool, ToolContext context, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NullSandboxApprovalQueue : IToolSandboxApprovalQueue
    {
        public static readonly NullSandboxApprovalQueue Instance = new();
        public Task EnqueueAsync(ToolSandboxWorkRequest request, CancellationToken ct) => Task.CompletedTask;
        public bool TryDequeue(out ToolSandboxWorkRequest? request) { request = null; return false; }
        public IReadOnlyCollection<ToolSandboxWorkRequest> Snapshot() => Array.Empty<ToolSandboxWorkRequest>();
    }

    private sealed class SpyPlannerTelemetry : IPlannerTelemetry
    {
        public Task PlanStartedAsync(PlannerTelemetryContext context, CancellationToken ct) => Task.CompletedTask;
        public Task PlanCompletedAsync(PlannerTelemetryContext context, PlannerResult result, CancellationToken ct) => Task.CompletedTask;
        public Task PlanFailedAsync(PlannerTelemetryContext context, Exception exception, CancellationToken ct) => Task.CompletedTask;
        public Task PlanThrottledAsync(PlannerTelemetryContext context, PlannerQuotaDecision decision, CancellationToken ct) => Task.CompletedTask;
        public Task PlanRejectedAsync(PlannerTelemetryContext context, PlannerQuotaDecision decision, CancellationToken ct) => Task.CompletedTask;
    }
}
