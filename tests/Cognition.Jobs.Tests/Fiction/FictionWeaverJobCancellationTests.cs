using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Jobs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Rebus.Bus;
using Xunit;

namespace Cognition.Jobs.Tests.Fiction;

public class FictionWeaverJobCancellationTests
{
    [Fact]
    public async Task RunVisionPlannerAsync_WhenCancelled_MarksConversationTaskCancelled()
    {
        await using var db = CreateDbContext();
        var planId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        var plan = new FictionPlan
        {
            Id = planId,
            FictionProjectId = Guid.NewGuid(),
            Name = "Cancellation Plan",
            PrimaryBranchSlug = "main"
        };
        db.FictionPlans.Add(plan);

        var conversationPlan = new ConversationPlan
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            PersonaId = Guid.NewGuid(),
            Title = "Fiction Weaver Plan",
            Description = "Verify cancellation propagation"
        };

        var currentTask = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = conversationPlan.Id,
            StepNumber = 1,
            ToolName = "fiction.weaver.visionPlanner",
            Status = "Queued",
            Goal = "Run vision planner"
        };

        var followOnTask = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = conversationPlan.Id,
            StepNumber = 2,
            ToolName = "fiction.weaver.worldBibleManager",
            Status = "Pending",
            Goal = "Run world bible manager"
        };

        conversationPlan.Tasks.Add(currentTask);
        conversationPlan.Tasks.Add(followOnTask);
        db.ConversationPlans.Add(conversationPlan);

        await db.SaveChangesAsync();

        var runner = new BlockingRunner(FictionPhase.VisionPlanner);
        var jobs = new FictionWeaverJobs(
            db,
            new IFictionPhaseRunner[] { runner },
            Substitute.For<IBus>(),
            Substitute.For<IPlanProgressNotifier>(),
            new WorkflowEventLogger(db, enabled: false),
            NullLogger<FictionWeaverJobs>.Instance);

        var metadata = new Dictionary<string, string>
        {
            ["conversationPlanId"] = conversationPlan.Id.ToString(),
            ["taskId"] = currentTask.Id.ToString(),
            ["stepNumber"] = currentTask.StepNumber.ToString()
        };

        using var cts = new CancellationTokenSource();
        var execution = jobs.RunVisionPlannerAsync(planId, agentId, conversationId, providerId, metadata: metadata, cancellationToken: cts.Token);

        await runner.WaitUntilStartedAsync().WaitAsync(TimeSpan.FromSeconds(2));
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);

        await db.Entry(currentTask).ReloadAsync();
        currentTask.Status.Should().Be("Cancelled");
        currentTask.Error.Should().Be("Phase execution cancelled.");

        await db.Entry(followOnTask).ReloadAsync();
        followOnTask.Status.Should().Be("Pending");
    }

    private static CognitionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new CancellationTestDbContext(options);
    }

    private sealed class CancellationTestDbContext : CognitionDbContext
    {
        public CancellationTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var allowed = new HashSet<Type>
            {
                typeof(FictionPlan),
                typeof(FictionPlanCheckpoint),
                typeof(ConversationPlan),
                typeof(ConversationTask)
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

            modelBuilder.Entity<FictionPlanCheckpoint>().Ignore(x => x.FictionPlan);
            modelBuilder.Entity<FictionPlanCheckpoint>().Ignore(x => x.Progress);

            modelBuilder.Entity<ConversationPlan>().Ignore(x => x.Conversation);
            modelBuilder.Entity<ConversationPlan>().Ignore(x => x.Persona);
        }
    }

    private sealed class BlockingRunner : IFictionPhaseRunner
    {
        private readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BlockingRunner(FictionPhase phase)
        {
            Phase = phase;
        }

        public FictionPhase Phase { get; }

        public Task WaitUntilStartedAsync() => _started.Task;

        public async Task<FictionPhaseResult> RunAsync(FictionPhaseExecutionContext context, CancellationToken cancellationToken = default)
        {
            _started.TrySetResult(true);
            await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            return FictionPhaseResult.Success(Phase, "Completed");
        }
    }
}
