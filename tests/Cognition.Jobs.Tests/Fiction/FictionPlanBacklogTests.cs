using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Clients.Tools.Planning;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Jobs;
using Cognition.Contracts;
using Cognition.Contracts.Events;
using Cognition.Testing.Utilities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Rebus.Bus;
using Xunit;

namespace Cognition.Jobs.Tests.Fiction;

public class FictionPlanBacklogTests
{
    [Fact]
    public async Task VisionPlanner_UpsertsBacklogItems()
    {
        await using var db = CreateDbContext();
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            Name = "Starfall Plan",
            Description = "An epic saga."
        };
        db.FictionPlans.Add(plan);
        await db.SaveChangesAsync();

        var backlogPayload = new Dictionary<string, object?>
        {
            ["backlog"] = new[]
            {
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["id"] = "outline-core-conflicts",
                    ["description"] = "Define the headline conflicts and stakes",
                    ["status"] = "in_progress",
                    ["inputs"] = new[] { "vision-plan" },
                    ["outputs"] = new[] { "conflict-outline" }
                },
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["id"] = "map-world-seeds",
                    ["description"] = "Enumerate worldbuilding seeds",
                    ["status"] = "pending"
                }
            }
        };

        var runner = new StubPhaseRunner(
            FictionPhase.VisionPlanner,
            (context, ct) => Task.FromResult(new FictionPhaseResult(
                FictionPhase.VisionPlanner,
                FictionPhaseStatus.Completed,
                "Vision backlog generated.",
                backlogPayload)));

        var jobs = CreateJobs(db, runner);

        var metadata = BuildMetadata();
        await jobs.RunVisionPlannerAsync(plan.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cancellationToken: CancellationToken.None, metadata: metadata);

        var items = await db.FictionPlanBacklogItems
            .Where(x => x.FictionPlanId == plan.Id)
            .OrderBy(x => x.BacklogId)
            .ToListAsync();

        items.Should().HaveCount(2);
        var conflicts = items.Single(i => string.Equals(i.BacklogId, "outline-core-conflicts", StringComparison.OrdinalIgnoreCase));
        conflicts.Status.Should().Be(FictionPlanBacklogStatus.InProgress);
        var seeds = items.Single(i => string.Equals(i.BacklogId, "map-world-seeds", StringComparison.OrdinalIgnoreCase));
        seeds.Status.Should().Be(FictionPlanBacklogStatus.Pending);
    }

    [Fact]
    public async Task ChapterArchitect_CompletesBacklogItem()
    {
        await using var db = CreateDbContext();
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            Name = "Starfall Plan"
        };
        db.FictionPlans.Add(plan);
        db.FictionPlanBacklogItems.Add(new FictionPlanBacklogItem
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            BacklogId = "outline-core-conflicts",
            Description = "Outline chapter conflicts",
            Status = FictionPlanBacklogStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var runner = new StubPhaseRunner(
            FictionPhase.ChapterArchitect,
            (context, ct) => Task.FromResult(FictionPhaseResult.Success(
                FictionPhase.ChapterArchitect,
                "Blueprint generated.")));

        var bus = Substitute.For<IBus>();
        var notifier = Substitute.For<IPlanProgressNotifier>();
        var scopePaths = ScopePathBuilderTestHelper.CreateBuilder();
        var jobs = new FictionWeaverJobs(
            db,
            new[] { runner },
            bus,
            notifier,
            new WorkflowEventLogger(db, enabled: false),
            Substitute.For<IFictionBacklogScheduler>(),
            scopePaths,
            NullLogger<FictionWeaverJobs>.Instance);
        var metadata = BuildMetadata("outline-core-conflicts");

        await jobs.RunChapterArchitectAsync(plan.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cancellationToken: CancellationToken.None, metadata: metadata);

        var backlogItem = await db.FictionPlanBacklogItems.SingleAsync(x => x.FictionPlanId == plan.Id);
        backlogItem.Status.Should().Be(FictionPlanBacklogStatus.Complete);
        backlogItem.InProgressAtUtc.Should().NotBeNull();
        backlogItem.CompletedAtUtc.Should().NotBeNull();

        await bus.Received().Publish(Arg.Is<FictionPhaseProgressed>(evt =>
            string.Equals(evt.BacklogItemId, "outline-core-conflicts", StringComparison.OrdinalIgnoreCase)));

        await notifier.Received().NotifyPlanProgressAsync(
            Arg.Any<Guid>(),
            Arg.Is<object>(payload => string.Equals(ReadBacklogId(payload), "outline-core-conflicts", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task ChapterArchitect_Failure_RevertsBacklogToPending()
    {
        await using var db = CreateDbContext();
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            Name = "Starfall Plan"
        };
        db.FictionPlans.Add(plan);
        db.FictionPlanBacklogItems.Add(new FictionPlanBacklogItem
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            BacklogId = "outline-core-conflicts",
            Description = "Outline chapter conflicts",
            Status = FictionPlanBacklogStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var runner = new StubPhaseRunner(
            FictionPhase.ChapterArchitect,
            (context, ct) => Task.FromResult(FictionPhaseResult.Failed(
                FictionPhase.ChapterArchitect,
                "Planner failed.",
                new InvalidOperationException("LLM refused."),
                data: null)));

        var jobs = CreateJobs(db, runner);
        var metadata = new Dictionary<string, string>
        {
            ["backlogItemId"] = "outline-core-conflicts"
        };

        await jobs.RunChapterArchitectAsync(plan.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cancellationToken: CancellationToken.None, metadata: BuildMetadata("outline-core-conflicts"));

        var backlogItem = await db.FictionPlanBacklogItems.SingleAsync(x => x.FictionPlanId == plan.Id);
        backlogItem.Status.Should().Be(FictionPlanBacklogStatus.Pending);
        backlogItem.InProgressAtUtc.Should().BeNull();
        backlogItem.CompletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task LoreFulfillmentJob_creates_world_bible_entry_and_marks_requirement_ready()
    {
        await using var db = CreateDbContext();
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            Name = "Lore Automation Plan",
            PrimaryBranchSlug = "main"
        };
        db.FictionPlans.Add(plan);

        var requirement = new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            RequirementSlug = "stellar-key",
            Title = "Stellar Key",
            Status = FictionLoreRequirementStatus.Blocked,
            Description = "Ancient artifact binding the two branches.",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
        };
        db.FictionLoreRequirements.Add(requirement);
        await db.SaveChangesAsync();

        var jobs = CreateJobs(db, enableWorkflowLogging: true);

        await jobs.RunLoreFulfillmentAsync(
            plan.Id,
            requirement.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "main",
            metadata: null,
            cancellationToken: CancellationToken.None);

        var updated = await db.FictionLoreRequirements.SingleAsync(r => r.Id == requirement.Id);
        updated.Status.Should().Be(FictionLoreRequirementStatus.Ready);
        updated.WorldBibleEntryId.Should().NotBeNull();
        updated.MetadataJson.Should().Contain("autoFulfillmentCompletedUtc");

        var entry = await db.FictionWorldBibleEntries.SingleAsync(e => e.Id == updated.WorldBibleEntryId);
        entry.EntryName.Should().Be("Stellar Key");
        entry.Content.Summary.Should().Contain("Ancient artifact");

        db.WorkflowEvents.Should().HaveCount(1);
        db.WorkflowEvents.Single().Kind.Should().Be("fiction.lore.fulfillment");
    }

    [Fact]
    public async Task VisionPlanner_CreatesConversationTasksForBacklog()
    {
        await using var db = CreateDbContext();
        var persona = new Persona { Id = Guid.NewGuid(), Name = "Planner Persona" };
        var agent = new Agent { Id = Guid.NewGuid(), PersonaId = persona.Id, Persona = persona };
        var conversation = new Conversation { Id = Guid.NewGuid(), AgentId = agent.Id, Agent = agent, Title = "Lore Sync" };
        var conversationPlan = new ConversationPlan
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            PersonaId = persona.Id,
            Title = "Fiction Plan",
            CreatedAt = DateTime.UtcNow,
            Tasks = new List<ConversationTask>()
        };

        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            Name = "Starfall Plan",
            CurrentConversationPlanId = conversationPlan.Id
        };

        db.Personas.Add(persona);
        db.Agents.Add(agent);
        db.Conversations.Add(conversation);
        db.ConversationPlans.Add(conversationPlan);
        db.FictionPlans.Add(plan);
        await db.SaveChangesAsync();

        var backlogPayload = new Dictionary<string, object?>
        {
            ["backlog"] = new[]
            {
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["id"] = "outline-core-conflicts",
                    ["description"] = "Define the headline conflicts and stakes",
                    ["status"] = "in_progress",
                    ["inputs"] = new[] { "vision-plan" },
                    ["outputs"] = new[] { "conflict-outline" }
                },
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["id"] = "map-world-seeds",
                    ["description"] = "Enumerate worldbuilding seeds",
                    ["status"] = "pending"
                }
            }
        };

        var runner = new StubPhaseRunner(
            FictionPhase.VisionPlanner,
            (context, ct) => Task.FromResult(new FictionPhaseResult(
                FictionPhase.VisionPlanner,
                FictionPhaseStatus.Completed,
                "Vision backlog generated.",
                backlogPayload)));

        var jobs = CreateJobs(db, runner);
        var providerId = Guid.NewGuid();

        await jobs.RunVisionPlannerAsync(plan.Id, agent.Id, conversation.Id, providerId, Guid.NewGuid(), metadata: BuildMetadata(), cancellationToken: CancellationToken.None);

        var tasks = await db.ConversationTasks
            .Where(t => t.ConversationPlanId == conversationPlan.Id)
            .OrderBy(t => t.StepNumber)
            .ToListAsync();

        tasks.Should().Contain(t => string.Equals(t.BacklogItemId, "outline-core-conflicts", StringComparison.OrdinalIgnoreCase));
        tasks.Should().Contain(t => string.Equals(t.BacklogItemId, "map-world-seeds", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ChapterArchitect_ContextIncludesBacklogMetadata()
    {
        await using var db = CreateDbContext();
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            Name = "Backlog Test Plan"
        };
        db.FictionPlans.Add(plan);
        await db.SaveChangesAsync();

        var backlogId = "outline-core-conflicts";
        string? capturedBacklogId = null;

        var runner = new StubPhaseRunner(
            FictionPhase.ChapterArchitect,
            (context, ct) =>
            {
                context.Metadata.Should().NotBeNull();
                context.Metadata!.TryGetValue("backlogItemId", out capturedBacklogId).Should().BeTrue();
                return Task.FromResult(FictionPhaseResult.Success(FictionPhase.ChapterArchitect, "ok"));
            });

        var jobs = CreateJobs(db, runner);
        var metadata = new Dictionary<string, string>
        {
            ["backlogItemId"] = backlogId
        };

        var result = await jobs.RunChapterArchitectAsync(plan.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cancellationToken: CancellationToken.None, metadata: BuildMetadata("outline-core-conflicts"));

        capturedBacklogId.Should().Be(backlogId);
        result.Data.Should().NotBeNull().And.ContainKey("backlogItemId").WhoseValue.Should().Be(backlogId);
    }

    [Fact]
    public async Task VisionPlanner_AttachesScopePathMetadata()
    {
        await using var db = CreateDbContext();
        var projectId = Guid.NewGuid();
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = projectId,
            Name = "Scope metadata plan",
            Description = "Verifies scope propagation."
        };
        db.FictionPlans.Add(plan);
        await db.SaveChangesAsync();

        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        FictionPhaseExecutionContext? capturedContext = null;
        var runner = new StubPhaseRunner(
            FictionPhase.VisionPlanner,
            (context, _) =>
            {
                capturedContext = context;
                return Task.FromResult(FictionPhaseResult.Success(FictionPhase.VisionPlanner));
            });

        var bus = Substitute.For<IBus>();
        FictionPhaseProgressed? published = null;
        bus.Publish(Arg.Do<FictionPhaseProgressed>(evt => published = evt)).Returns(Task.CompletedTask);

        var notifier = Substitute.For<IPlanProgressNotifier>();
        var scheduler = Substitute.For<IFictionBacklogScheduler>();
        var scopePaths = ScopePathBuilderTestHelper.CreateBuilder();
        var jobs = new FictionWeaverJobs(
            db,
            new[] { runner },
            bus,
            notifier,
            new WorkflowEventLogger(db, enabled: false),
            scheduler,
            scopePaths,
            NullLogger<FictionWeaverJobs>.Instance);

        await jobs.RunVisionPlannerAsync(plan.Id, agentId, conversationId, Guid.NewGuid(), Guid.NewGuid(), metadata: BuildMetadata(), cancellationToken: CancellationToken.None);

        capturedContext.Should().NotBeNull();
        var context = capturedContext!;
        context.ScopePath.Should().NotBeNull();
        context.ScopeToken.Should().NotBeNull();

        var expectedScope = scopePaths.Build(new ScopeToken(null, null, null, agentId, conversationId, plan.Id, projectId, null)).Canonical;
        context.ScopePath!.Canonical.Should().Be(expectedScope);
        context.Metadata.Should().NotBeNull();
        var metadata = context.Metadata!;
        metadata.Should().ContainKey("scopePath").WhoseValue.Should().Be(expectedScope);
        metadata.Should().ContainKey("scopePrincipalType").WhoseValue.Should().Be("agent");
        metadata.Should().ContainKey("scopePrincipalId").WhoseValue.Should().Be(agentId.ToString("D"));

        published.Should().NotBeNull();
        var payload = published!.Payload!;
        payload.Should().ContainKey("scopePath");
        payload["scopePath"]?.ToString().Should().Be(expectedScope);
        payload["scopePrincipalType"]?.ToString().Should().Be("agent");
        payload["scopePrincipalId"]?.ToString().Should().Be(agentId.ToString("D"));
    }

    [Fact]
    public async Task ScrollRefiner_ContextIncludesBacklogMetadata()
    {
        await using var db = CreateDbContext();
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            Name = "Backlog Scroll Plan"
        };
        db.FictionPlans.Add(plan);
        await db.SaveChangesAsync();

        var backlogId = "refine-scroll";
        string? capturedBacklogId = null;

        var runner = new StubPhaseRunner(
            FictionPhase.ScrollRefiner,
            (context, ct) =>
            {
                context.Metadata.Should().NotBeNull();
                context.Metadata!.TryGetValue("backlogItemId", out capturedBacklogId).Should().BeTrue();
                return Task.FromResult(FictionPhaseResult.Success(FictionPhase.ScrollRefiner, "ok"));
            });

        var jobs = CreateJobs(db, runner);
        var metadata = new Dictionary<string, string>
        {
            ["backlogItemId"] = backlogId
        };

        var result = await jobs.RunScrollRefinerAsync(plan.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cancellationToken: CancellationToken.None, metadata: BuildMetadata("refine-scroll"));

        capturedBacklogId.Should().Be(backlogId);
        result.Data.Should().NotBeNull().And.ContainKey("backlogItemId").WhoseValue.Should().Be(backlogId);
    }

    [Fact]
    public async Task SceneWeaver_ContextIncludesBacklogMetadata()
    {
        await using var db = CreateDbContext();
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            Name = "Backlog Scene Plan"
        };
        db.FictionPlans.Add(plan);
        await db.SaveChangesAsync();

        var backlogId = "draft-first-scene";
        string? capturedBacklogId = null;

        var runner = new StubPhaseRunner(
            FictionPhase.SceneWeaver,
            (context, ct) =>
            {
                context.Metadata.Should().NotBeNull();
                context.Metadata!.TryGetValue("backlogItemId", out capturedBacklogId).Should().BeTrue();
                return Task.FromResult(FictionPhaseResult.Success(FictionPhase.SceneWeaver, "ok"));
            });

        var jobs = CreateJobs(db, runner);
        var metadata = new Dictionary<string, string>
        {
            ["backlogItemId"] = backlogId
        };

        var result = await jobs.RunSceneWeaverAsync(plan.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cancellationToken: CancellationToken.None, metadata: BuildMetadata(backlogId));

        capturedBacklogId.Should().Be(backlogId);
        result.Data.Should().NotBeNull().And.ContainKey("backlogItemId").WhoseValue.Should().Be(backlogId);
    }

    private static Dictionary<string, string> BuildMetadata(string? backlogId = null)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["conversationPlanId"] = Guid.NewGuid().ToString(),
            ["taskId"] = Guid.NewGuid().ToString()
        };

        if (!string.IsNullOrWhiteSpace(backlogId))
        {
            metadata["backlogItemId"] = backlogId;
        }

        return metadata;
    }

    private static FictionWeaverJobs CreateJobs(CognitionDbContext db, params IFictionPhaseRunner[] runners)
        => CreateJobsInternal(db, false, runners);

    private static FictionWeaverJobs CreateJobs(CognitionDbContext db, bool enableWorkflowLogging, params IFictionPhaseRunner[] runners)
        => CreateJobsInternal(db, enableWorkflowLogging, runners);

    private static FictionWeaverJobs CreateJobsInternal(CognitionDbContext db, bool enableWorkflowLogging, params IFictionPhaseRunner[] runners)
    {
        var bus = Substitute.For<IBus>();
        var notifier = Substitute.For<IPlanProgressNotifier>();
        var workflowLogger = new WorkflowEventLogger(db, enableWorkflowLogging);
        var scheduler = Substitute.For<IFictionBacklogScheduler>();
        var logger = NullLogger<FictionWeaverJobs>.Instance;
        var scopePaths = ScopePathBuilderTestHelper.CreateBuilder();
        return new FictionWeaverJobs(db, runners, bus, notifier, workflowLogger, scheduler, scopePaths, logger);
    }

    private static string? ReadBacklogId(object payload)
    {
        var property = payload.GetType().GetProperty("backlogItemId");
        return property?.GetValue(payload)?.ToString();
    }

    private static CognitionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new BacklogTestDbContext(options);
    }


    private sealed class BacklogTestDbContext : CognitionDbContext
    {
        public BacklogTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var allowed = new HashSet<Type>
            {
                typeof(FictionPlan),
                typeof(FictionPlanBacklogItem),
                typeof(FictionPlanCheckpoint),
                typeof(ConversationPlan),
                typeof(ConversationTask),
                typeof(Conversation),
                typeof(Persona),
                typeof(Agent),
                typeof(FictionLoreRequirement),
                typeof(FictionWorldBible),
                typeof(FictionWorldBibleEntry),
                typeof(WorkflowEvent)
            };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (entityType.ClrType is not null && !allowed.Contains(entityType.ClrType))
                {
                    modelBuilder.Ignore(entityType.ClrType);
                }
            }

            modelBuilder.Entity<Agent>().Ignore(a => a.State);
            modelBuilder.Entity<Conversation>().Ignore(c => c.Metadata);
            modelBuilder.Entity<FictionPlanBacklogItem>().Ignore(x => x.Inputs);
            modelBuilder.Entity<FictionPlanBacklogItem>().Ignore(x => x.Outputs);
            modelBuilder.Entity<FictionPlanCheckpoint>().Ignore(x => x.Progress);
        }
    }


    private sealed class StubPhaseRunner : IFictionPhaseRunner
    {
        private readonly Func<FictionPhaseExecutionContext, CancellationToken, Task<FictionPhaseResult>> _handler;

        public StubPhaseRunner(FictionPhase phase, Func<FictionPhaseExecutionContext, CancellationToken, Task<FictionPhaseResult>> handler)
        {
            Phase = phase;
            _handler = handler;
        }

        public FictionPhase Phase { get; }

        public Task<FictionPhaseResult> RunAsync(FictionPhaseExecutionContext context, CancellationToken cancellationToken = default)
            => _handler(context, cancellationToken);
    }
}
