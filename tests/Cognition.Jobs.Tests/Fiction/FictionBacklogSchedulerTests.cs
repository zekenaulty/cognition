using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Jobs;
using Newtonsoft.Json.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Cognition.Jobs.Tests.Fiction;

public class FictionBacklogSchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_QueuesBacklogItemsInDependencyOrder()
    {
        await using var db = CreateDbContext();
        var planId = Guid.NewGuid();
        var plan = new FictionPlan
        {
            Id = planId,
            FictionProjectId = Guid.NewGuid(),
            Name = "Scheduler Plan",
            PrimaryBranchSlug = "main"
        };
        db.FictionPlans.Add(plan);

        var backlogItems = new[]
        {
            CreateBacklogItem(planId, "outline-core-conflicts", new[] { "vision-plan" }, new[] { "chapter-blueprint" }, createdOffsetMinutes: 0),
            CreateBacklogItem(planId, "refine-scroll", new[] { "chapter-blueprint" }, new[] { "chapter-scroll" }, createdOffsetMinutes: 1),
            CreateBacklogItem(planId, "draft-scene", new[] { "chapter-scroll" }, new[] { "scene-draft" }, createdOffsetMinutes: 2)
        };

        db.FictionPlanBacklogItems.AddRange(backlogItems);
        await db.SaveChangesAsync();

        var jobClient = Substitute.For<IFictionWeaverJobClient>();
        var scheduler = new FictionBacklogScheduler(db, jobClient, NullLogger<FictionBacklogScheduler>.Instance);

        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        var metadata = new Dictionary<string, string>
        {
            ["providerId"] = providerId.ToString(),
            ["modelId"] = modelId.ToString()
        };

        var context = new FictionPhaseExecutionContext(
            planId,
            agentId,
            conversationId,
            "main",
            Metadata: metadata);

        // First scheduling should enqueue ChapterArchitect
        await scheduler.ScheduleAsync(plan, FictionPhase.VisionPlanner, FictionPhaseResult.Success(FictionPhase.VisionPlanner), context, CancellationToken.None);

        backlogItems[0].Status.Should().Be(FictionPlanBacklogStatus.InProgress);
        backlogItems[1].Status.Should().Be(FictionPlanBacklogStatus.Pending);
        backlogItems[2].Status.Should().Be(FictionPlanBacklogStatus.Pending);

        jobClient.Received(1).EnqueueChapterArchitect(
            planId,
            agentId,
            conversationId,
            Arg.Any<Guid>(),
            providerId,
            modelId,
            "main",
            Arg.Is<IReadOnlyDictionary<string, string>>(md => string.Equals(md["backlogItemId"], "outline-core-conflicts", StringComparison.OrdinalIgnoreCase)));

        var blueprintId = ExtractGuid(backlogItems[0], "chapterBlueprintId");
        blueprintId.Should().NotBe(Guid.Empty);
        db.ChangeTracker.Entries<FictionChapterBlueprint>()
            .Select(e => e.Entity.Id)
            .Should().Contain(blueprintId);

        // Mark first backlog as complete and schedule again to queue ScrollRefiner
        backlogItems[0].Status = FictionPlanBacklogStatus.Complete;
        backlogItems[0].CompletedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await scheduler.ScheduleAsync(plan, FictionPhase.ChapterArchitect, FictionPhaseResult.Success(FictionPhase.ChapterArchitect), context, CancellationToken.None);

        backlogItems[1].Status.Should().Be(FictionPlanBacklogStatus.InProgress);
        backlogItems[2].Status.Should().Be(FictionPlanBacklogStatus.Pending);

        jobClient.Received(1).EnqueueScrollRefiner(
            planId,
            agentId,
            conversationId,
            Arg.Any<Guid>(),
            providerId,
            modelId,
            "main",
            Arg.Is<IReadOnlyDictionary<string, string>>(md => string.Equals(md["backlogItemId"], "refine-scroll", StringComparison.OrdinalIgnoreCase)));

        var scrollId = ExtractGuid(backlogItems[1], "chapterScrollId");
        scrollId.Should().NotBe(Guid.Empty);
        db.ChangeTracker.Entries<FictionChapterScroll>()
            .Select(e => e.Entity.Id)
            .Should().Contain(scrollId);

        // Mark second backlog complete and schedule again to queue SceneWeaver
        backlogItems[1].Status = FictionPlanBacklogStatus.Complete;
        backlogItems[1].CompletedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await scheduler.ScheduleAsync(plan, FictionPhase.ScrollRefiner, FictionPhaseResult.Success(FictionPhase.ScrollRefiner), context, CancellationToken.None);

        backlogItems[2].Status.Should().Be(FictionPlanBacklogStatus.InProgress);

        jobClient.Received(1).EnqueueSceneWeaver(
            planId,
            agentId,
            conversationId,
            Arg.Any<Guid>(),
            providerId,
            modelId,
            "main",
            Arg.Is<IReadOnlyDictionary<string, string>>(md => string.Equals(md["backlogItemId"], "draft-scene", StringComparison.OrdinalIgnoreCase)));

        var sceneId = ExtractGuid(backlogItems[2], "chapterSceneId");
        sceneId.Should().NotBe(Guid.Empty);
        db.ChangeTracker.Entries<FictionChapterScene>()
            .Select(e => e.Entity.Id)
            .Should().Contain(sceneId);

        jobClient.Received(1).EnqueueChapterArchitect(
            planId,
            agentId,
            conversationId,
            Arg.Any<Guid>(),
            providerId,
            modelId,
            "main",
            Arg.Is<IReadOnlyDictionary<string, string>>(md => string.Equals(md["backlogItemId"], "outline-core-conflicts", StringComparison.OrdinalIgnoreCase)));

        jobClient.Received(1).EnqueueScrollRefiner(
            planId,
            agentId,
            conversationId,
            Arg.Any<Guid>(),
            providerId,
            modelId,
            "main",
            Arg.Is<IReadOnlyDictionary<string, string>>(md => string.Equals(md["backlogItemId"], "refine-scroll", StringComparison.OrdinalIgnoreCase)));

        jobClient.Received(1).EnqueueSceneWeaver(
            planId,
            agentId,
            conversationId,
            Arg.Any<Guid>(),
            providerId,
            modelId,
            "main",
            Arg.Is<IReadOnlyDictionary<string, string>>(md => string.Equals(md["backlogItemId"], "draft-scene", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task ScheduleAsync_queues_lore_fulfillment_when_blocked_beyond_sla()
    {
        await using var db = CreateDbContext();
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            Name = "Lore Plan",
            PrimaryBranchSlug = "main"
        };
        db.FictionPlans.Add(plan);
        db.FictionLoreRequirements.Add(new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            RequirementSlug = "stellar-key",
            Title = "Stellar Key",
            Status = FictionLoreRequirementStatus.Blocked,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
        });
        await db.SaveChangesAsync();

        var jobClient = Substitute.For<IFictionWeaverJobClient>();
        var scheduler = new FictionBacklogScheduler(db, jobClient, NullLogger<FictionBacklogScheduler>.Instance);

        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var metadata = new Dictionary<string, string>
        {
            ["providerId"] = providerId.ToString(),
            ["modelId"] = modelId.ToString()
        };

        var context = new FictionPhaseExecutionContext(
            plan.Id,
            agentId,
            conversationId,
            "main",
            Metadata: metadata);

        await scheduler.ScheduleAsync(
            plan,
            FictionPhase.VisionPlanner,
            FictionPhaseResult.Success(FictionPhase.VisionPlanner),
            context,
            CancellationToken.None);

        jobClient.Received(1).EnqueueLoreFulfillment(
            plan.Id,
            Arg.Any<Guid>(),
            agentId,
            conversationId,
            providerId,
            modelId,
            "main",
            Arg.Any<IReadOnlyDictionary<string, string>>());

        var requirement = await db.FictionLoreRequirements.SingleAsync();
        requirement.MetadataJson.Should().Contain("autoFulfillmentRequestedUtc");
    }

    [Fact]
    public async Task ScheduleAsync_auto_resumes_stale_inprogress_items_and_logs_action()
    {
        await using var db = CreateDbContext();
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            Name = "Auto Resume Plan",
            PrimaryBranchSlug = "main"
        };

        var persona = new Persona
        {
            Id = Guid.NewGuid(),
            Name = "Author",
            Role = "writer",
            Voice = "steady"
        };

        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            PersonaId = persona.Id,
            Persona = persona,
            RolePlay = false
        };

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            Agent = agent,
            Title = "Backlog Conversation"
        };

        var conversationPlan = new ConversationPlan
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Conversation = conversation,
            PersonaId = persona.Id,
            Persona = persona,
            Title = "Resume Tasks"
        };

        plan.CurrentConversationPlanId = conversationPlan.Id;

        var backlogItem = new FictionPlanBacklogItem
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            BacklogId = "draft-scene-42",
            Description = "Draft the final scene",
            Status = FictionPlanBacklogStatus.InProgress,
            Outputs = new[] { "scene-draft" },
            CreatedAtUtc = DateTime.UtcNow.AddHours(-4),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-3),
            InProgressAtUtc = DateTime.UtcNow.AddHours(-3)
        };

        var conversationTask = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = conversationPlan.Id,
            ConversationPlan = conversationPlan,
            StepNumber = 1,
            Thought = "Run scene weaver",
            ToolName = "fiction.scene.weaver",
            ArgsJson = "{}",
            Status = "Running",
            BacklogItemId = backlogItem.BacklogId,
            Error = "timeout",
            Observation = "stalled"
        };

        db.FictionPlans.Add(plan);
        db.Personas.Add(persona);
        db.Agents.Add(agent);
        db.Conversations.Add(conversation);
        db.ConversationPlans.Add(conversationPlan);
        db.ConversationTasks.Add(conversationTask);
        db.FictionPlanBacklogItems.Add(backlogItem);
        await db.SaveChangesAsync();

        var jobClient = Substitute.For<IFictionWeaverJobClient>();
        var scheduler = new FictionBacklogScheduler(db, jobClient, NullLogger<FictionBacklogScheduler>.Instance);

        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var metadata = new Dictionary<string, string>
        {
            ["providerId"] = providerId.ToString(),
            ["modelId"] = modelId.ToString()
        };

        var context = new FictionPhaseExecutionContext(
            plan.Id,
            agent.Id,
            conversation.Id,
            "main",
            Metadata: metadata);

        await scheduler.ScheduleAsync(
            plan,
            FictionPhase.SceneWeaver,
            FictionPhaseResult.Success(FictionPhase.SceneWeaver),
            context,
            CancellationToken.None);

        var updatedTask = await db.ConversationTasks.SingleAsync(t => t.Id == conversationTask.Id);
        updatedTask.Status.Should().Be("Pending");
        updatedTask.Error.Should().BeNull();
        updatedTask.Observation.Should().BeNull();

        var actionEvent = await db.WorkflowEvents.SingleAsync();
        actionEvent.Kind.Should().Be("fiction.backlog.action");
        actionEvent.Payload.Value<string>("action").Should().Be("auto-resume");
        actionEvent.Payload.Value<string>("backlogId").Should().Be(backlogItem.BacklogId);
        actionEvent.Payload.Value<string>("source").Should().Be("automation");
        actionEvent.Payload.Value<Guid?>("providerId").Should().Be(providerId);
        actionEvent.Payload.Value<Guid?>("modelId").Should().Be(modelId);
        actionEvent.Payload.Value<Guid?>("conversationPlanId").Should().Be(conversationPlan.Id);
        actionEvent.Payload.Value<Guid?>("taskId").Should().Be(conversationTask.Id);
    }

    [Fact]
    public async Task ScheduleAsync_stamps_conversation_task_metadata_and_queue_payload()
    {
        await using var db = CreateDbContext();
        var planId = Guid.NewGuid();
        var plan = new FictionPlan
        {
            Id = planId,
            FictionProjectId = Guid.NewGuid(),
            Name = "Metadata Plan",
            PrimaryBranchSlug = "draft"
        };

        var persona = new Persona
        {
            Id = Guid.NewGuid(),
            Name = "Author",
            Role = "writer",
            Voice = "steady"
        };

        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            PersonaId = persona.Id,
            Persona = persona,
            RolePlay = false
        };

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            Agent = agent,
            Title = "Backlog Conversation"
        };

        var conversationPlan = new ConversationPlan
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Conversation = conversation,
            PersonaId = persona.Id,
            Persona = persona,
            Title = "Resume Tasks"
        };

        var backlogItem = CreateBacklogItem(planId, "outline-core", null, new[] { "chapter-blueprint" }, createdOffsetMinutes: 0);

        var conversationTask = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = conversationPlan.Id,
            ConversationPlan = conversationPlan,
            StepNumber = 1,
            Thought = "Run architect",
            ToolName = "fiction.chapter.architect",
            ArgsJson = "{}",
            Status = "Pending",
            BacklogItemId = backlogItem.BacklogId
        };

        plan.CurrentConversationPlanId = conversationPlan.Id;

        db.FictionPlans.Add(plan);
        db.Personas.Add(persona);
        db.Agents.Add(agent);
        db.Conversations.Add(conversation);
        db.ConversationPlans.Add(conversationPlan);
        db.ConversationTasks.Add(conversationTask);
        db.FictionPlanBacklogItems.Add(backlogItem);
        await db.SaveChangesAsync();

        var jobClient = Substitute.For<IFictionWeaverJobClient>();
        var scheduler = new FictionBacklogScheduler(db, jobClient, NullLogger<FictionBacklogScheduler>.Instance);

        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var contextMetadata = new Dictionary<string, string>
        {
            ["providerId"] = providerId.ToString(),
            ["modelId"] = modelId.ToString(),
            ["conversationPlanId"] = conversationPlan.Id.ToString()
        };

        var context = new FictionPhaseExecutionContext(
            planId,
            agent.Id,
            conversation.Id,
            "draft",
            Metadata: contextMetadata);

        await scheduler.ScheduleAsync(
            plan,
            FictionPhase.VisionPlanner,
            FictionPhaseResult.Success(FictionPhase.VisionPlanner),
            context,
            CancellationToken.None);

        var updatedTask = await db.ConversationTasks.SingleAsync(t => t.Id == conversationTask.Id);
        updatedTask.ProviderId.Should().Be(providerId);
        updatedTask.ModelId.Should().Be(modelId);
        updatedTask.AgentId.Should().Be(agent.Id);
        updatedTask.BacklogItemId.Should().Be(backlogItem.BacklogId);
        updatedTask.ConversationPlanId.Should().Be(conversationPlan.Id);

        var args = JsonSerializer.Deserialize<Dictionary<string, object?>>(updatedTask.ArgsJson ?? "{}", new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        args["planId"]!.ToString().Should().Be(planId.ToString());
        args["backlogItemId"]!.ToString().Should().Be(backlogItem.BacklogId);
        args["conversationPlanId"]!.ToString().Should().Be(conversationPlan.Id.ToString());
        args["conversationId"]!.ToString().Should().Be(conversation.Id.ToString());
        args["providerId"]!.ToString().Should().Be(providerId.ToString());
        args["agentId"]!.ToString().Should().Be(agent.Id.ToString());
        args["modelId"]!.ToString().Should().Be(modelId.ToString());
        args["branchSlug"]!.ToString().Should().Be("draft");

        jobClient.Received(1).EnqueueChapterArchitect(
            planId,
            agent.Id,
            conversation.Id,
            Arg.Any<Guid>(),
            providerId,
            modelId,
            "draft",
            Arg.Is<IReadOnlyDictionary<string, string>>(md =>
                md.ContainsKey("conversationPlanId") &&
                md["conversationPlanId"] == conversationPlan.Id.ToString() &&
                md.ContainsKey("conversationId") &&
                md["conversationId"] == conversation.Id.ToString() &&
                md.ContainsKey("providerId") &&
                md["providerId"] == providerId.ToString() &&
                md.ContainsKey("agentId") &&
                md["agentId"] == agent.Id.ToString() &&
                md.ContainsKey("branchSlug") &&
                md["branchSlug"] == "draft" &&
                md.ContainsKey("backlogItemId") &&
                md["backlogItemId"] == backlogItem.BacklogId));
    }

    [Fact]
    public async Task ScheduleAsync_EnsuresWorldBibleAndQueuesJob()
    {
        await using var db = CreateDbContext();
        var planId = Guid.NewGuid();
        var plan = new FictionPlan
        {
            Id = planId,
            FictionProjectId = Guid.NewGuid(),
            Name = "World Bible Plan",
            PrimaryBranchSlug = "lore"
        };
        db.FictionPlans.Add(plan);

        var backlogItem = CreateBacklogItem(planId, "map-world-seeds", null, new[] { "world-bible" }, createdOffsetMinutes: 0);
        db.FictionPlanBacklogItems.Add(backlogItem);
        await db.SaveChangesAsync();

        var jobClient = Substitute.For<IFictionWeaverJobClient>();
        var scheduler = new FictionBacklogScheduler(db, jobClient, NullLogger<FictionBacklogScheduler>.Instance);

        var providerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        var contextMetadata = new Dictionary<string, string>
        {
            ["providerId"] = providerId.ToString()
        };

        var context = new FictionPhaseExecutionContext(
            planId,
            agentId,
            conversationId,
            BranchSlug: string.Empty,
            Metadata: contextMetadata);

        await scheduler.ScheduleAsync(plan, FictionPhase.VisionPlanner, FictionPhaseResult.Success(FictionPhase.VisionPlanner), context, CancellationToken.None);

        backlogItem.Status.Should().Be(FictionPlanBacklogStatus.InProgress);

        var worldBibles = await db.FictionWorldBibles.Where(x => x.FictionPlanId == planId).ToListAsync();
        worldBibles.Should().HaveCount(1);
        var worldBible = worldBibles.Single();
        worldBible.Domain.Should().Be("core");
        worldBible.BranchSlug.Should().Be("lore");

        backlogItem.Outputs.Should().NotBeNull();
        backlogItem.Outputs!.Should().Contain(o => o.StartsWith("worldBibleId=", StringComparison.OrdinalIgnoreCase));

        jobClient.Received(1).EnqueueWorldBibleManager(
            planId,
            agentId,
            conversationId,
            providerId,
            Arg.Is<Guid?>(m => m == null),
            "lore",
            Arg.Is<IReadOnlyDictionary<string, string>>(md =>
                string.Equals(md["backlogItemId"], "map-world-seeds", StringComparison.OrdinalIgnoreCase) &&
                md.ContainsKey("worldBibleId")));
    }

    [Fact]
    public async Task ScheduleAsync_QueuesIterativePlannerWithNextIterationIndex()
    {
        await using var db = CreateDbContext();
        var planId = Guid.NewGuid();
        var plan = new FictionPlan
        {
            Id = planId,
            FictionProjectId = Guid.NewGuid(),
            Name = "Iteration Plan",
            PrimaryBranchSlug = "main"
        };
        db.FictionPlans.Add(plan);

        db.FictionPlanPasses.AddRange(
            new FictionPlanPass { Id = Guid.NewGuid(), FictionPlanId = planId, PassIndex = 1, Title = "Pass 1" },
            new FictionPlanPass { Id = Guid.NewGuid(), FictionPlanId = planId, PassIndex = 2, Title = "Pass 2" });

        var backlogItem = CreateBacklogItem(planId, "refine-iteration", new[] { "vision-plan" }, new[] { "iteration-plan" }, createdOffsetMinutes: 0);
        db.FictionPlanBacklogItems.Add(backlogItem);
        await db.SaveChangesAsync();

        var jobClient = Substitute.For<IFictionWeaverJobClient>();
        var scheduler = new FictionBacklogScheduler(db, jobClient, NullLogger<FictionBacklogScheduler>.Instance);

        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();

        var contextMetadata = new Dictionary<string, string>
        {
            ["providerId"] = providerId.ToString(),
            ["modelId"] = modelId.ToString()
        };

        var context = new FictionPhaseExecutionContext(
            planId,
            agentId,
            conversationId,
            BranchSlug: "draft",
            Metadata: contextMetadata);

        await scheduler.ScheduleAsync(plan, FictionPhase.VisionPlanner, FictionPhaseResult.Success(FictionPhase.VisionPlanner), context, CancellationToken.None);

        backlogItem.Status.Should().Be(FictionPlanBacklogStatus.InProgress);
        backlogItem.Outputs.Should().NotBeNull();
        backlogItem.Outputs!.Should().Contain(o => o.Equals("iteration-plan", StringComparison.OrdinalIgnoreCase));
        backlogItem.Outputs!.Should().Contain(o => o.StartsWith("iterationIndex=", StringComparison.OrdinalIgnoreCase));

        jobClient.Received(1).EnqueueIterativePlanner(
            planId,
            agentId,
            conversationId,
            3,
            providerId,
            modelId,
            "draft",
            Arg.Is<IReadOnlyDictionary<string, string>>(md =>
                string.Equals(md["backlogItemId"], "refine-iteration", StringComparison.OrdinalIgnoreCase) &&
                md.ContainsKey("iterationIndex") &&
                md["iterationIndex"] == "3"));
    }

    private static FictionPlanBacklogItem CreateBacklogItem(Guid planId, string id, IReadOnlyCollection<string>? inputs, IReadOnlyCollection<string> outputs, int createdOffsetMinutes)
    {
        return new FictionPlanBacklogItem
        {
            Id = Guid.NewGuid(),
            FictionPlanId = planId,
            BacklogId = id,
            Description = id,
            Status = FictionPlanBacklogStatus.Pending,
            Inputs = inputs?.ToArray(),
            Outputs = outputs.ToArray(),
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(createdOffsetMinutes)
        };
    }

    private static Guid ExtractGuid(FictionPlanBacklogItem item, string key)
    {
        var prefix = $"{key}=";
        var entry = item.Outputs?.FirstOrDefault(o => o.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        entry.Should().NotBeNull();
        Guid.TryParse(entry!.Substring(prefix.Length), out var value).Should().BeTrue();
        return value;
    }

    private static FictionBacklogSchedulerTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FictionBacklogSchedulerTestDbContext(options);
    }

    private sealed class FictionBacklogSchedulerTestDbContext : CognitionDbContext
    {
        public FictionBacklogSchedulerTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var allowed = new HashSet<Type>
            {
                typeof(FictionPlan),
                typeof(FictionPlanBacklogItem),
                typeof(FictionChapterBlueprint),
                typeof(FictionChapterScroll),
                typeof(FictionChapterSection),
                typeof(FictionChapterScene),
                typeof(FictionWorldBible),
                typeof(FictionPlanPass),
                typeof(FictionLoreRequirement),
                typeof(Persona),
                typeof(Agent),
                typeof(Conversation),
                typeof(ConversationPlan),
                typeof(ConversationTask),
                typeof(WorkflowEvent)
            };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (entityType.ClrType is not null && !allowed.Contains(entityType.ClrType))
                {
                    modelBuilder.Ignore(entityType.ClrType);
                }
            }

            modelBuilder.Entity<FictionPlan>().Ignore(x => x.FictionProject);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.Passes);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.ChapterBlueprints);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.Checkpoints);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.Backlog);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.Transcripts);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.StoryMetrics);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.WorldBibles);
            modelBuilder.Entity<FictionWorldBible>().Ignore(x => x.Entries);
            modelBuilder.Entity<FictionPlanBacklogItem>().Ignore(x => x.FictionPlan);
            modelBuilder.Entity<FictionChapterBlueprint>().Ignore(x => x.Structure);
            modelBuilder.Entity<FictionChapterScroll>().Ignore(x => x.Metadata);
            modelBuilder.Entity<FictionChapterSection>().Ignore(x => x.Metadata);
            modelBuilder.Entity<FictionChapterScene>().Ignore(x => x.Metadata);
            modelBuilder.Entity<FictionPlanPass>().Ignore(x => x.Metadata);
            modelBuilder.Entity<Persona>().Ignore(x => x.OutboundLinks);
            modelBuilder.Entity<Persona>().Ignore(x => x.InboundLinks);
            modelBuilder.Entity<Persona>().Ignore(x => x.KnownPersonas);
            modelBuilder.Entity<Persona>().Ignore(x => x.SignatureTraits);
            modelBuilder.Entity<Persona>().Ignore(x => x.NarrativeThemes);
            modelBuilder.Entity<Persona>().Ignore(x => x.DomainExpertise);
            modelBuilder.Entity<Agent>().Ignore(x => x.ClientProfile);
            modelBuilder.Entity<Agent>().Ignore(x => x.ToolBindings);
            modelBuilder.Entity<Agent>().Ignore(x => x.State);
            modelBuilder.Entity<Conversation>().Ignore(x => x.Participants);
            modelBuilder.Entity<Conversation>().Ignore(x => x.Messages);
            modelBuilder.Entity<Conversation>().Ignore(x => x.Summaries);
            modelBuilder.Entity<Conversation>().Ignore(x => x.Metadata);
            modelBuilder.Entity<ConversationPlan>().Ignore(x => x.Tasks);
            modelBuilder.Entity<ConversationPlan>().Ignore(x => x.Conversation);
            modelBuilder.Entity<ConversationPlan>().Ignore(x => x.Persona);
            modelBuilder.Entity<ConversationTask>().Ignore(x => x.ConversationPlan);

            var stringArrayConverter = new ValueConverter<string[]?, string?>(
                v => v == null ? null : JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => string.IsNullOrWhiteSpace(v) ? Array.Empty<string>() : JsonSerializer.Deserialize<string[]>(v, JsonSerializerOptions.Default)!);

            modelBuilder.Entity<FictionPlanBacklogItem>()
                .Property(x => x.Inputs)
                .HasConversion(stringArrayConverter);

            modelBuilder.Entity<FictionPlanBacklogItem>()
                .Property(x => x.Outputs)
                .HasConversion(stringArrayConverter);
        }

    }
}








