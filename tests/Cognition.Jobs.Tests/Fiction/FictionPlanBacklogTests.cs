using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Clients.Tools.Planning;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Jobs;
using Cognition.Contracts.Events;
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

        await jobs.RunVisionPlannerAsync(plan.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cancellationToken: CancellationToken.None);

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
        var jobs = new FictionWeaverJobs(
            db,
            new[] { runner },
            bus,
            notifier,
            new WorkflowEventLogger(db, enabled: false),
            Substitute.For<IFictionBacklogScheduler>(),
            NullLogger<FictionWeaverJobs>.Instance);
        var metadata = new Dictionary<string, string>
        {
            ["backlogItemId"] = "outline-core-conflicts"
        };

        await jobs.RunChapterArchitectAsync(plan.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cancellationToken: CancellationToken.None, metadata: metadata);

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

        await jobs.RunChapterArchitectAsync(plan.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cancellationToken: CancellationToken.None, metadata: metadata);

        var backlogItem = await db.FictionPlanBacklogItems.SingleAsync(x => x.FictionPlanId == plan.Id);
        backlogItem.Status.Should().Be(FictionPlanBacklogStatus.Pending);
        backlogItem.InProgressAtUtc.Should().BeNull();
        backlogItem.CompletedAtUtc.Should().BeNull();
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

        var result = await jobs.RunChapterArchitectAsync(plan.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cancellationToken: CancellationToken.None, metadata: metadata);

        capturedBacklogId.Should().Be(backlogId);
        result.Data.Should().NotBeNull().And.ContainKey("backlogItemId").WhoseValue.Should().Be(backlogId);
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

        var result = await jobs.RunScrollRefinerAsync(plan.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cancellationToken: CancellationToken.None, metadata: metadata);

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

        var result = await jobs.RunSceneWeaverAsync(plan.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cancellationToken: CancellationToken.None, metadata: metadata);

        capturedBacklogId.Should().Be(backlogId);
        result.Data.Should().NotBeNull().And.ContainKey("backlogItemId").WhoseValue.Should().Be(backlogId);
    }

    private static FictionWeaverJobs CreateJobs(CognitionDbContext db, params IFictionPhaseRunner[] runners)
    {
        var bus = Substitute.For<IBus>();
        var notifier = Substitute.For<IPlanProgressNotifier>();
        var workflowLogger = new WorkflowEventLogger(db, enabled: false);
        var scheduler = Substitute.For<IFictionBacklogScheduler>();
        var logger = NullLogger<FictionWeaverJobs>.Instance;
        return new FictionWeaverJobs(db, runners, bus, notifier, workflowLogger, scheduler, logger);
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
                typeof(FictionPlanCheckpoint)
            };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (entityType.ClrType is not null && !allowed.Contains(entityType.ClrType))
                {
                    modelBuilder.Ignore(entityType.ClrType);
                }
            }

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
