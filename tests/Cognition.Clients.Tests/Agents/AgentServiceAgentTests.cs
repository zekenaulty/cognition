using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Agents;
using Cognition.Clients.LLM;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Planning;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Personas;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cognition.Clients.Tests.Agents;

public class AgentServiceAgentTests
{
    private static CognitionDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AgentTestDbContext(options);
    }

    [Fact]
    public async Task AskAsync_UsesAgentIdAndReturnsReply()
    {
        await using var db = CreateDb();
        var persona = new Persona { Id = Guid.NewGuid(), Name = "Agent Persona", OwnedBy = OwnedBy.System, Type = PersonaType.Agent };
        var agent = new Agent { Id = Guid.NewGuid(), PersonaId = persona.Id };
        db.Personas.Add(persona);
        db.Agents.Add(agent);
        await db.SaveChangesAsync();

        var llm = new StubLlmClient { Reply = "hello" };
        var service = new AgentService(
            db,
            new StubLlmFactory(llm),
            new NullToolDispatcher(),
            new StubServiceProvider(),
            NullLogger<AgentService>.Instance);

        var result = await service.AskAsync(agent.Id, Guid.NewGuid(), null, "ping", CancellationToken.None);

        result.Should().Be("hello");
        llm.LastChatInput.Should().Contain("ping");
    }

    [Fact]
    public async Task AskAsync_WhenAgentMissing_Throws()
    {
        await using var db = CreateDb();
        var llm = new StubLlmClient { Reply = "hello" };
        var service = new AgentService(
            db,
            new StubLlmFactory(llm),
            new NullToolDispatcher(),
            new StubServiceProvider(),
            NullLogger<AgentService>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => service.AskAsync(Guid.NewGuid(), Guid.NewGuid(), null, "ping", CancellationToken.None));
    }

    private sealed class StubLlmFactory : ILLMClientFactory
    {
        private readonly ILLMClient _client;
        public StubLlmFactory(ILLMClient client) => _client = client;
        public Task<ILLMClient> CreateAsync(Guid providerId, Guid? modelId = null, CancellationToken ct = default) => Task.FromResult(_client);
        public Task<ILLMClient> CreateAsync(Guid providerId, Guid? modelId, Guid? personaId, CancellationToken ct = default) => Task.FromResult(_client);
    }

    private sealed class StubLlmClient : ILLMClient
    {
        public string Reply { get; set; } = string.Empty;
        public string? LastChatInput { get; private set; }

        public Task<string> GenerateAsync(string prompt, bool track = false)
        {
            LastChatInput = prompt;
            return Task.FromResult(Reply);
        }

        public IAsyncEnumerable<string> GenerateStreamAsync(string prompt, bool track = false)
        {
            async IAsyncEnumerable<string> Stream()
            {
                LastChatInput = prompt;
                yield return Reply;
            }
            return Stream();
        }

        public Task<string> ChatAsync(IEnumerable<ChatMessage> messages, bool track = false)
        {
            LastChatInput = string.Join("\n", messages);
            return Task.FromResult(Reply);
        }

        public IAsyncEnumerable<string> ChatStreamAsync(IEnumerable<ChatMessage> messages, bool track = false)
        {
            async IAsyncEnumerable<string> Stream()
            {
                LastChatInput = string.Join("\n", messages);
                yield return Reply;
            }
            return Stream();
        }
    }

    private sealed class NullToolDispatcher : IToolDispatcher
    {
        public Task<(bool ok, object? result, string? error)> ExecuteAsync(Guid toolId, ToolContext ctx, IDictionary<string, object?> args, bool log = true)
            => Task.FromResult<(bool, object?, string?)>((true, "noop", null));
        public Task<(bool ok, PlannerResult? result, string? error)> ExecutePlannerAsync(Guid toolId, PlannerContext ctx, PlannerParameters parameters, bool log = true)
            => Task.FromResult<(bool, PlannerResult?, string?)>((true, PlannerResult.Success(), null));
    }

    private sealed class StubServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class AgentTestDbContext : CognitionDbContext
    {
        public AgentTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Agent>(b => b.Ignore(a => a.State));
            modelBuilder.Entity<AgentToolBinding>(b => b.Ignore(t => t.Config));
            modelBuilder.Entity<Data.Relational.Modules.Config.DataSource>(b => b.Ignore(d => d.Config));
            modelBuilder.Entity<Data.Relational.Modules.Config.SystemVariable>(b => b.Ignore(s => s.Value));
            modelBuilder.Entity<Data.Relational.Modules.Conversations.Conversation>(b => b.Ignore(c => c.Metadata));
            modelBuilder.Entity<Data.Relational.Modules.Fiction.FictionChapterBlueprint>(b => b.Ignore(f => f.Structure));
            modelBuilder.Entity<Data.Relational.Modules.Fiction.FictionChapterScene>(b => b.Ignore(f => f.Metadata));
            modelBuilder.Entity<Data.Relational.Modules.Fiction.FictionChapterScroll>(b => b.Ignore(f => f.Metadata));
            modelBuilder.Entity<Data.Relational.Modules.Fiction.FictionChapterSection>(b => b.Ignore(f => f.Metadata));
            modelBuilder.Entity<Data.Relational.Modules.Fiction.FictionPlanCheckpoint>(b => b.Ignore(c => c.Progress));
            modelBuilder.Entity<Data.Relational.Modules.Fiction.FictionPlanPass>(b => b.Ignore(p => p.Metadata));
            modelBuilder.Entity<Data.Relational.Modules.Fiction.FictionPlanTranscript>(b => b.Ignore(t => t.Metadata));
            modelBuilder.Entity<Data.Relational.Modules.Fiction.FictionStoryMetric>(b => b.Ignore(m => m.Data));
            modelBuilder.Entity<Data.Relational.Modules.Images.ImageAsset>(b => b.Ignore(i => i.Metadata));
            modelBuilder.Entity<Data.Relational.Modules.Images.ImageStyle>(b => b.Ignore(i => i.Defaults));
            modelBuilder.Entity<Data.Relational.Modules.Knowledge.KnowledgeEmbedding>(b =>
            {
                b.Ignore(k => k.Metadata);
                b.Ignore(k => k.ScopeSegments);
            });
            modelBuilder.Entity<Data.Relational.Modules.Knowledge.KnowledgeItem>(b => b.Ignore(k => k.Properties));
            modelBuilder.Entity<Data.Relational.Modules.LLM.Model>(b => b.Ignore(m => m.Metadata));
            modelBuilder.Entity<Data.Relational.Modules.Personas.PersonaDream>(b => b.Ignore(p => p.Properties));
            modelBuilder.Entity<Data.Relational.Modules.Personas.PersonaEvent>(b => b.Ignore(p => p.Properties));
            modelBuilder.Entity<Data.Relational.Modules.Personas.PersonaEventType>(b => b.Ignore(p => p.Properties));
            modelBuilder.Entity<Data.Relational.Modules.Personas.PersonaMemory>(b => b.Ignore(p => p.Properties));
            modelBuilder.Entity<Data.Relational.Modules.Personas.PersonaMemoryType>(b => b.Ignore(p => p.Properties));
            modelBuilder.Entity<Data.Relational.Modules.Prompts.PromptTemplate>(b => b.Ignore(p => p.Tokens));
            modelBuilder.Entity<Data.Relational.Modules.Tools.Tool>(b => b.Ignore(t => t.Metadata));
            modelBuilder.Entity<Data.Relational.Modules.Tools.ToolParameter>(b =>
            {
                b.Ignore(t => t.DefaultValue);
                b.Ignore(t => t.Options);
            });
            modelBuilder.Entity<Data.Relational.Modules.Tools.ToolExecutionLog>(b =>
            {
                b.Ignore(t => t.Request);
                b.Ignore(t => t.Response);
            });
            modelBuilder.Entity<Data.Relational.Modules.Planning.PlannerExecution>(b =>
            {
                b.Ignore(p => p.Artifacts);
                b.Ignore(p => p.ConversationState);
                b.Ignore(p => p.Diagnostics);
                b.Ignore(p => p.Transcript);
                b.Ignore(p => p.Metrics);
            });
        }
    }
}
