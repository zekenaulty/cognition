using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Tools;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
            .AddSingleton<TestScopeTool>(toolSpy)
            .AddSingleton<ITool>(toolSpy)
            .BuildServiceProvider();

        var registry = new SingleToolRegistry(classPath, typeof(TestScopeTool));
        var dispatcher = new ToolDispatcher(db, services, registry, NullLogger<ToolDispatcher>.Instance);

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

        ok.Should().BeTrue();
        error.Should().BeNull();

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

            modelBuilder.Entity<Cognition.Data.Relational.Modules.LLM.ClientProfile>(b =>
            {
                b.Ignore(p => p.Provider);
                b.Ignore(p => p.Model);
                b.Ignore(p => p.ApiCredential);
            });
        }
    }
}
