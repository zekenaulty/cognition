using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Clients.Tools.Planning;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Jobs;
using Cognition.Testing.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Rebus.Bus;
using Xunit;

namespace Cognition.Jobs.Tests.Fiction;

public class FictionResumeRegressionTests
{
    [Fact]
    public async Task Resume_Triggers_Scheduler_Which_Triggers_LoreJob_Which_Updates_WorldBible()
    {
        // 1. Setup DB
        await using var db = CreateDbContext();
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = Guid.NewGuid(),
            Name = "Regression Plan",
            PrimaryBranchSlug = "main"
        };
        db.FictionPlans.Add(plan);

        var requirement = new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            RequirementSlug = "ancient-seal",
            Title = "The Ancient Seal",
            Status = FictionLoreRequirementStatus.Blocked,
            Description = "A seal that blocks the path.",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        db.FictionLoreRequirements.Add(requirement);
        await db.SaveChangesAsync();

        // 2. Setup Scheduler with Mock Job Client
        var jobClient = Substitute.For<IFictionWeaverJobClient>();
        var scheduler = new FictionBacklogScheduler(db, jobClient, NullLogger<FictionBacklogScheduler>.Instance);

        var context = new FictionPhaseExecutionContext(
            plan.Id,
            Guid.NewGuid(), // Agent
            Guid.NewGuid(), // Conversation
            "main",
            Metadata: new Dictionary<string, string>
            {
                ["providerId"] = Guid.NewGuid().ToString(),
                ["modelId"] = Guid.NewGuid().ToString()
            });

        // 3. Trigger Scheduler (Simulate Resume/Schedule loop)
        // We pass a dummy completed phase/result as we are testing the "AutoQueueLoreFulfillment" side effect
        await scheduler.ScheduleAsync(
            plan,
            FictionPhase.VisionPlanner,
            FictionPhaseResult.Success(FictionPhase.VisionPlanner),
            context,
            CancellationToken.None);

        // 4. Verify Job Enqueued & Capture Args
        jobClient.Received(1).EnqueueLoreFulfillment(
            plan.Id,
            requirement.Id,
            context.AgentId,
            context.ConversationId,
            Arg.Any<Guid>(), // Provider
            Arg.Any<Guid?>(), // Model
            "main",
            Arg.Any<IReadOnlyDictionary<string, string>>());

        // Capture arguments to pass to the actual job
        var call = jobClient.ReceivedCalls().First(c => c.GetMethodInfo().Name == nameof(IFictionWeaverJobClient.EnqueueLoreFulfillment));
        var args = call.GetArguments();
        // Signature: planId, requirementId, agentId, conversationId, providerId, modelId, branchSlug, metadata

        // 5. Setup Real Job Runner
        var jobs = CreateJobs(db, enableWorkflowLogging: true);

        // 6. Run the Job (Simulate Hangfire execution)
        await jobs.RunLoreFulfillmentAsync(
            (Guid)args[0]!, // planId
            (Guid)args[1]!, // requirementId
            (Guid)args[2]!, // agentId
            (Guid)args[3]!, // conversationId
            (Guid)args[4]!, // providerId
            (Guid?)args[5], // modelId
            (string)args[6]!, // branchSlug
            (IReadOnlyDictionary<string, string>?)args[7], // metadata
            CancellationToken.None);

        // 7. Verify End State
        var updatedReq = await db.FictionLoreRequirements.SingleAsync(r => r.Id == requirement.Id);
        updatedReq.Status.Should().Be(FictionLoreRequirementStatus.Ready);
        updatedReq.WorldBibleEntryId.Should().NotBeNull();

        var entry = await db.FictionWorldBibleEntries.SingleAsync(e => e.Id == updatedReq.WorldBibleEntryId);
        entry.EntryName.Should().Be("The Ancient Seal");
        entry.Content.Summary.Should().Be("A seal that blocks the path."); // Default behavior copies description to summary

        // Verify Workflow Event
        var events = await db.WorkflowEvents.ToListAsync();
        events.Should().ContainSingle(e => e.Kind == "fiction.lore.fulfillment");
    }

    // --- Helpers ---

    private static FictionWeaverJobs CreateJobs(CognitionDbContext db, bool enableWorkflowLogging, params IFictionPhaseRunner[] runners)
    {
        var bus = Substitute.For<IBus>();
        var notifier = Substitute.For<IPlanProgressNotifier>();
        var workflowLogger = new WorkflowEventLogger(db, enableWorkflowLogging);
        var scheduler = Substitute.For<IFictionBacklogScheduler>();
        var logger = NullLogger<FictionWeaverJobs>.Instance;
        var scopePaths = ScopePathBuilderTestHelper.CreateBuilder();
        return new FictionWeaverJobs(db, runners, bus, notifier, workflowLogger, scheduler, scopePaths, logger);
    }

    private static CognitionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new RegressionTestDbContext(options);
    }

    private sealed class RegressionTestDbContext : CognitionDbContext
    {
        public RegressionTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ignore entities not needed for this test to avoid EF Core issues with partial context
            var allowed = new HashSet<Type>
            {
                typeof(FictionPlan),
                typeof(FictionPlanBacklogItem),
                typeof(FictionLoreRequirement),
                typeof(FictionWorldBible),
                typeof(FictionWorldBibleEntry),
                typeof(WorkflowEvent),
                typeof(Agent),
                typeof(Persona),
                typeof(Conversation)
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
        }
    }
}
