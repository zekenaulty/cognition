using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cognition.Contracts.Events;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Jobs;
using Cognition.Clients.Tools;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Rebus.Bus;
using Xunit;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Clients.Tools.Planning;

namespace Cognition.Jobs.Tests
{
    public class PlannerBacklogMetadataTests
    {
        [Fact]
        public async Task PlanReadyHandler_propagates_backlog_metadata_to_tool_request()
        {
            await using var db = CreateDbContext();
            var conversationId = Guid.NewGuid();
            var personaId = Guid.NewGuid();
            var planId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var args = new Dictionary<string, object?>
            {
                ["planId"] = Guid.NewGuid(),
                ["agentId"] = agentId,
                ["conversationId"] = conversationId,
                ["providerId"] = Guid.NewGuid(),
                ["backlogItemId"] = "outline-core-conflicts"
            };

            var conversationPlan = new ConversationPlan
            {
                Id = planId,
                ConversationId = conversationId,
                PersonaId = personaId,
                Title = "Backlog Plan",
                CreatedAt = DateTime.UtcNow,
                Tasks = new List<ConversationTask>()
            };

            conversationPlan.Tasks.Add(new ConversationTask
            {
                Id = Guid.NewGuid(),
                ConversationPlanId = planId,
                StepNumber = 1,
                ToolName = "fiction.weaver.chapterArchitect",
                ArgsJson = System.Text.Json.JsonSerializer.Serialize(args),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });

            db.ConversationPlans.Add(conversationPlan);
            await db.SaveChangesAsync();

            var bus = Substitute.For<IBus>();
            ToolExecutionRequested? published = null;
            bus.Publish(Arg.Any<object>()).Returns(Task.CompletedTask).AndDoes(ci =>
            {
                if (ci.Arg<object>() is ToolExecutionRequested request)
                {
                    published = request;
                }
            });

            var handler = new PlanReadyHandler(db, bus, new WorkflowEventLogger(db, false));

            var planReady = new PlanReady(
                conversationId,
                agentId,
                personaId,
                Guid.NewGuid(),
                null,
                new ToolPlan("{}"),
                planId,
                Guid.NewGuid(),
                "main",
                new Dictionary<string, object?>());

            await handler.Handle(planReady);

            published.Should().NotBeNull();
            published!.Metadata.Should().ContainKey("backlogItemId").WhoseValue.Should().Be("outline-core-conflicts");
        }

        [Fact]
        public async Task ToolExecutionHandler_passes_backlog_metadata_to_weaver_jobs()
        {
            await using var db = CreateDbContext();
            var planId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var conversationId = Guid.NewGuid();
            var task = SeedConversationTask(db, planId, 1, "fiction.weaver.chapterArchitect", conversationId, new Dictionary<string, object?>
            {
                ["planId"] = planId,
                ["agentId"] = agentId,
                ["conversationId"] = conversationId,
                ["providerId"] = Guid.NewGuid(),
                ["chapterBlueprintId"] = Guid.NewGuid(),
            });

            var dispatcher = Substitute.For<IToolDispatcher>();
            var weaverJobs = Substitute.For<IFictionWeaverJobClient>();
            weaverJobs.EnqueueChapterArchitect(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>>())
                .Returns("job-123");
            var bus = Substitute.For<IBus>();
            ToolExecutionHandler handler = new ToolExecutionHandler(db, dispatcher, weaverJobs, new ServiceCollection().BuildServiceProvider(), bus, new WorkflowEventLogger(db, false));

            var metadata = new Dictionary<string, object?>
            {
                ["backlogItemId"] = "outline-core-conflicts"
            };

            var args = new Dictionary<string, object?>
            {
                ["planId"] = planId.ToString(),
                ["agentId"] = agentId.ToString(),
                ["conversationId"] = conversationId.ToString(),
                ["providerId"] = Guid.NewGuid().ToString(),
                ["chapterBlueprintId"] = Guid.NewGuid().ToString()
            };

            var request = new ToolExecutionRequested(
                conversationId,
                agentId,
                Guid.NewGuid(),
                "fiction.weaver.chapterArchitect",
                args,
                task.ConversationPlanId,
                task.StepNumber,
                Guid.NewGuid(),
                "main",
                metadata);

            System.Threading.Tasks.Task handleTask = handler.Handle(request);
            await handleTask;

            weaverJobs.Received(1).EnqueueChapterArchitect(
                planId,
                agentId,
                conversationId,
                Guid.Parse(args["chapterBlueprintId"]?.ToString() ?? throw new InvalidOperationException("chapterBlueprintId missing")),
                Guid.Parse(args["providerId"]?.ToString() ?? throw new InvalidOperationException("providerId missing")),
                Arg.Any<Guid?>(),
                "main",
                Arg.Is<IReadOnlyDictionary<string, string>>(md => md.ContainsKey("backlogItemId") && md["backlogItemId"] == "outline-core-conflicts"));
        }

        [Fact]
        public async Task ToolExecutionHandler_reads_backlog_metadata_from_args_when_missing()
        {
            await using var db = CreateDbContext();
            var planId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var conversationId = Guid.NewGuid();
            var backlogId = "architect-outline";
            var task = SeedConversationTask(db, planId, 2, "fiction.weaver.chapterArchitect", conversationId, new Dictionary<string, object?>
            {
                ["planId"] = planId,
                ["agentId"] = agentId,
                ["conversationId"] = conversationId,
                ["providerId"] = Guid.NewGuid(),
                ["chapterBlueprintId"] = Guid.NewGuid(),
                ["backlogItemId"] = backlogId
            });

            var dispatcher = Substitute.For<IToolDispatcher>();
            var weaverJobs = Substitute.For<IFictionWeaverJobClient>();
            weaverJobs.EnqueueChapterArchitect(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>>())
                .Returns("job-456");
            var bus = Substitute.For<IBus>();
            ToolExecutionHandler handler = new ToolExecutionHandler(db, dispatcher, weaverJobs, new ServiceCollection().BuildServiceProvider(), bus, new WorkflowEventLogger(db, false));

            var args = new Dictionary<string, object?>
            {
                ["planId"] = planId.ToString(),
                ["agentId"] = agentId.ToString(),
                ["conversationId"] = conversationId.ToString(),
                ["providerId"] = Guid.NewGuid().ToString(),
                ["chapterBlueprintId"] = Guid.NewGuid().ToString(),
                ["backlogItemId"] = backlogId
            };

            var request = new ToolExecutionRequested(
                conversationId,
                agentId,
                Guid.NewGuid(),
                "fiction.weaver.chapterArchitect",
                args,
                task.ConversationPlanId,
                task.StepNumber,
                Guid.NewGuid(),
                "main",
                new Dictionary<string, object?>());

            System.Threading.Tasks.Task handleTask = handler.Handle(request);
            await handleTask;

            weaverJobs.Received(1).EnqueueChapterArchitect(
                planId,
                agentId,
                conversationId,
                Guid.Parse(args["chapterBlueprintId"]?.ToString() ?? throw new InvalidOperationException("chapterBlueprintId missing")),
                Guid.Parse(args["providerId"]?.ToString() ?? throw new InvalidOperationException("providerId missing")),
                Arg.Any<Guid?>(),
                "main",
                Arg.Is<IReadOnlyDictionary<string, string>>(md => md.ContainsKey("backlogItemId") && md["backlogItemId"] == backlogId));
        }

        [Fact]
        public async Task ToolExecutionHandler_passes_backlog_metadata_to_scroll_refiner_jobs()
        {
            await using var db = CreateDbContext();
            var planId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var conversationId = Guid.NewGuid();
            var task = SeedConversationTask(db, planId, 3, "fiction.weaver.scrollRefiner", conversationId, new Dictionary<string, object?>
            {
                ["planId"] = planId,
                ["agentId"] = agentId,
                ["conversationId"] = conversationId,
                ["providerId"] = Guid.NewGuid(),
                ["chapterScrollId"] = Guid.NewGuid()
            });

            var dispatcher = Substitute.For<IToolDispatcher>();
            var weaverJobs = Substitute.For<IFictionWeaverJobClient>();
            weaverJobs.EnqueueScrollRefiner(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>>())
                .Returns("job-789");
            var bus = Substitute.For<IBus>();
            var handler = new ToolExecutionHandler(db, dispatcher, weaverJobs, new ServiceCollection().BuildServiceProvider(), bus, new WorkflowEventLogger(db, false));

            var metadata = new Dictionary<string, object?>
            {
                ["backlogItemId"] = "refine-scroll"
            };

            var args = new Dictionary<string, object?>
            {
                ["planId"] = planId.ToString(),
                ["agentId"] = agentId.ToString(),
                ["conversationId"] = conversationId.ToString(),
                ["providerId"] = Guid.NewGuid().ToString(),
                ["chapterScrollId"] = Guid.NewGuid().ToString()
            };

            var request = new ToolExecutionRequested(
                conversationId,
                agentId,
                Guid.NewGuid(),
                "fiction.weaver.scrollRefiner",
                args,
                task.ConversationPlanId,
                task.StepNumber,
                Guid.NewGuid(),
                "main",
                metadata);

            await handler.Handle(request);

            weaverJobs.Received(1).EnqueueScrollRefiner(
                planId,
                agentId,
                conversationId,
                Guid.Parse(args["chapterScrollId"]?.ToString() ?? throw new InvalidOperationException("chapterScrollId missing")),
                Guid.Parse(args["providerId"]?.ToString() ?? throw new InvalidOperationException("providerId missing")),
                Arg.Any<Guid?>(),
                "main",
                Arg.Is<IReadOnlyDictionary<string, string>>(md => md.ContainsKey("backlogItemId") && md["backlogItemId"] == "refine-scroll"));
        }

        [Fact]
        public async Task ToolExecutionHandler_passes_backlog_metadata_to_scene_weaver_jobs()
        {
            await using var db = CreateDbContext();
            var planId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var conversationId = Guid.NewGuid();
            var task = SeedConversationTask(db, planId, 4, "fiction.weaver.sceneWeaver", conversationId, new Dictionary<string, object?>
            {
                ["planId"] = planId,
                ["agentId"] = agentId,
                ["conversationId"] = conversationId,
                ["providerId"] = Guid.NewGuid(),
                ["chapterSceneId"] = Guid.NewGuid()
            });

            var dispatcher = Substitute.For<IToolDispatcher>();
            var weaverJobs = Substitute.For<IFictionWeaverJobClient>();
            weaverJobs.EnqueueSceneWeaver(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>>())
                .Returns("job-987");
            var bus = Substitute.For<IBus>();
            var handler = new ToolExecutionHandler(db, dispatcher, weaverJobs, new ServiceCollection().BuildServiceProvider(), bus, new WorkflowEventLogger(db, false));

            var metadata = new Dictionary<string, object?>
            {
                ["backlogItemId"] = "draft-scene"
            };

            var args = new Dictionary<string, object?>
            {
                ["planId"] = planId.ToString(),
                ["agentId"] = agentId.ToString(),
                ["conversationId"] = conversationId.ToString(),
                ["providerId"] = Guid.NewGuid().ToString(),
                ["chapterSceneId"] = Guid.NewGuid().ToString()
            };

            var request = new ToolExecutionRequested(
                conversationId,
                agentId,
                Guid.NewGuid(),
                "fiction.weaver.sceneWeaver",
                args,
                task.ConversationPlanId,
                task.StepNumber,
                Guid.NewGuid(),
                "main",
                metadata);

            await handler.Handle(request);

            weaverJobs.Received(1).EnqueueSceneWeaver(
                planId,
                agentId,
                conversationId,
                Guid.Parse(args["chapterSceneId"]?.ToString() ?? throw new InvalidOperationException("chapterSceneId missing")),
                Guid.Parse(args["providerId"]?.ToString() ?? throw new InvalidOperationException("providerId missing")),
                Arg.Any<Guid?>(),
                "main",
                Arg.Is<IReadOnlyDictionary<string, string>>(md => md.ContainsKey("backlogItemId") && md["backlogItemId"] == "draft-scene"));
        }

        [Fact]
        public async Task FictionWeaverJobs_sets_backlog_status_on_success()
        {
            await using var db = CreateDbContext();
            var planId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var conversationId = Guid.NewGuid();

            db.FictionPlans.Add(new FictionPlan
            {
                Id = planId,
                FictionProjectId = Guid.NewGuid(),
                Name = "Backlog Plan",
                PrimaryBranchSlug = "main",
                Status = FictionPlanStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow
            });

            db.FictionPlanBacklogItems.Add(new FictionPlanBacklogItem
            {
                Id = Guid.NewGuid(),
                FictionPlanId = planId,
                BacklogId = "outline-core-conflicts",
                Description = "Outline conflicts",
                Status = FictionPlanBacklogStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            var runner = new StubPhaseRunner(FictionPhase.IterativePlanner, (_, _) =>
                Task.FromResult(FictionPhaseResult.Success(FictionPhase.IterativePlanner, "done")));

            var jobs = new FictionWeaverJobs(
                db,
                new[] { runner },
                Substitute.For<IBus>(),
                Substitute.For<IPlanProgressNotifier>(),
                new WorkflowEventLogger(db, enabled: false),
                NullLogger<FictionWeaverJobs>.Instance);

            var metadata = new Dictionary<string, string>
            {
                ["backlogItemId"] = "outline-core-conflicts"
            };

            await jobs.RunIterativePlannerAsync(planId, agentId, conversationId, 1, Guid.NewGuid(), metadata: metadata);

            var backlog = await db.FictionPlanBacklogItems.SingleAsync(x => x.BacklogId == "outline-core-conflicts");
            backlog.Status.Should().Be(FictionPlanBacklogStatus.Complete);
            backlog.InProgressAtUtc.Should().NotBeNull();
            backlog.CompletedAtUtc.Should().NotBeNull();
        }

        [Fact]
        public async Task FictionWeaverJobs_resets_backlog_status_on_failure()
        {
            await using var db = CreateDbContext();
            var planId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var conversationId = Guid.NewGuid();

            db.FictionPlans.Add(new FictionPlan
            {
                Id = planId,
                FictionProjectId = Guid.NewGuid(),
                Name = "Backlog Plan",
                PrimaryBranchSlug = "main",
                Status = FictionPlanStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow
            });

            db.FictionPlanBacklogItems.Add(new FictionPlanBacklogItem
            {
                Id = Guid.NewGuid(),
                FictionPlanId = planId,
                BacklogId = "outline-core-conflicts",
                Description = "Outline conflicts",
                Status = FictionPlanBacklogStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            var runner = new StubPhaseRunner(FictionPhase.IterativePlanner, (_, _) =>
                Task.FromException<FictionPhaseResult>(new InvalidOperationException("boom")));

            var jobs = new FictionWeaverJobs(
                db,
                new[] { runner },
                Substitute.For<IBus>(),
                Substitute.For<IPlanProgressNotifier>(),
                new WorkflowEventLogger(db, enabled: false),
                NullLogger<FictionWeaverJobs>.Instance);

            var metadata = new Dictionary<string, string>
            {
                ["backlogItemId"] = "outline-core-conflicts"
            };

            await FluentActions.Invoking(() => jobs.RunIterativePlannerAsync(planId, agentId, conversationId, 1, Guid.NewGuid(), metadata: metadata))
                .Should().ThrowAsync<InvalidOperationException>();

            var backlog = await db.FictionPlanBacklogItems.SingleAsync(x => x.BacklogId == "outline-core-conflicts");
            backlog.Status.Should().Be(FictionPlanBacklogStatus.Pending);
            backlog.InProgressAtUtc.Should().BeNull();
            backlog.CompletedAtUtc.Should().BeNull();
        }

        private static ConversationTask SeedConversationTask(PlannerBacklogTestDbContext db, Guid planId, int stepNumber, string toolName, Guid conversationId, Dictionary<string, object?> args)
        {
            var personaId = Guid.NewGuid();
            var plan = new ConversationPlan
            {
                Id = planId,
                ConversationId = conversationId,
                PersonaId = personaId,
                Title = "Backlog Plan",
                CreatedAt = DateTime.UtcNow,
                Tasks = new List<ConversationTask>()
            };

            var task = new ConversationTask
            {
                Id = Guid.NewGuid(),
                ConversationPlanId = planId,
                StepNumber = stepNumber,
                ToolName = toolName,
                ArgsJson = System.Text.Json.JsonSerializer.Serialize(args),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            plan.Tasks.Add(task);
            db.ConversationPlans.Add(plan);
            db.SaveChanges();
            return task;
        }

        private static PlannerBacklogTestDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<CognitionDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;
            return new PlannerBacklogTestDbContext(options);
        }

        private sealed class PlannerBacklogTestDbContext : CognitionDbContext
        {
            public PlannerBacklogTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                var allowed = new HashSet<Type>
                {
                    typeof(ConversationPlan),
                    typeof(ConversationTask),
                    typeof(FictionPlan),
                    typeof(FictionPlanBacklogItem),
                    typeof(FictionPlanCheckpoint),
                    typeof(FictionPlanTranscript)
                };

                foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
                {
                    if (entityType.ClrType is not null && !allowed.Contains(entityType.ClrType))
                    {
                        modelBuilder.Ignore(entityType.ClrType);
                    }
                }

                modelBuilder.Entity<ConversationPlan>().Ignore(x => x.Conversation);
                modelBuilder.Entity<ConversationPlan>().Ignore(x => x.Persona);
                modelBuilder.Entity<FictionPlan>().Ignore(x => x.FictionProject);
                modelBuilder.Entity<FictionPlan>().Ignore(x => x.Passes);
                modelBuilder.Entity<FictionPlan>().Ignore(x => x.ChapterBlueprints);
                modelBuilder.Entity<FictionPlan>().Ignore(x => x.Checkpoints);
                modelBuilder.Entity<FictionPlan>().Ignore(x => x.Backlog);
                modelBuilder.Entity<FictionPlan>().Ignore(x => x.Transcripts);
                modelBuilder.Entity<FictionPlan>().Ignore(x => x.StoryMetrics);
                modelBuilder.Entity<FictionPlan>().Ignore(x => x.WorldBibles);
                modelBuilder.Entity<FictionPlanBacklogItem>().Ignore(x => x.FictionPlan);
                modelBuilder.Entity<FictionPlanCheckpoint>().Ignore(x => x.FictionPlan);
                modelBuilder.Entity<FictionPlanCheckpoint>().Ignore(x => x.Progress);
                modelBuilder.Entity<FictionPlanTranscript>().Ignore(x => x.FictionPlan);
                modelBuilder.Entity<FictionPlanTranscript>().Ignore(x => x.FictionChapterBlueprint);
                modelBuilder.Entity<FictionPlanTranscript>().Ignore(x => x.FictionChapterScene);
                modelBuilder.Entity<FictionPlanTranscript>().Ignore(x => x.Metadata);
            }
        }

        private sealed class StubPhaseRunner : IFictionPhaseRunner
        {
            private readonly FictionPhase _phase;
            private readonly Func<FictionPhaseExecutionContext, CancellationToken, Task<FictionPhaseResult>> _handler;

            public StubPhaseRunner(FictionPhase phase, Func<FictionPhaseExecutionContext, CancellationToken, Task<FictionPhaseResult>> handler)
            {
                _phase = phase;
                _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }

            public FictionPhase Phase => _phase;

            public Task<FictionPhaseResult> RunAsync(FictionPhaseExecutionContext context, CancellationToken cancellationToken = default)
                => _handler(context, cancellationToken);
        }
    }
}
