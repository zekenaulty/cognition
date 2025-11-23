using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Controllers;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Clients.Tools.Planning;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Jobs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Rebus.Bus;
using Xunit;
using Cognition.Testing.Utilities;

namespace Cognition.Api.Tests.Controllers;

public class BacklogHangfireLoopTests
{
    [Fact]
    public async Task ResumeBacklog_triggers_scheduler_and_lore_job_with_workflow_events()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);

        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Loop Plan", PrimaryBranchSlug = "main" };
        var backlog = new FictionPlanBacklogItem
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            BacklogId = "vision-1",
            Description = "Vision backlog",
            Status = FictionPlanBacklogStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
        };
        plan.Backlog.Add(backlog);

        var requirement = new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            RequirementSlug = "ancient-seal",
            Title = "Ancient Seal",
            Status = FictionLoreRequirementStatus.Blocked,
            Description = "A seal that blocks the path.",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
        };
        db.FictionLoreRequirements.Add(requirement);

        var conversation = new Conversation { Id = Guid.NewGuid(), AgentId = Guid.NewGuid(), Title = "Conv" };
        var conversationPlan = new ConversationPlan
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            PersonaId = Guid.NewGuid(),
            Title = "Conv Plan"
        };
        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var agentId = conversation.AgentId;
        var task = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = conversationPlan.Id,
            ConversationPlan = conversationPlan,
            BacklogItemId = backlog.BacklogId,
            StepNumber = 1,
            Status = "Pending",
            ArgsJson = "{}",
            ProviderId = providerId,
            ModelId = modelId,
            AgentId = agentId,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        conversationPlan.Tasks.Add(task);

        plan.CurrentConversationPlanId = conversationPlan.Id;
        db.FictionPlans.Add(plan);
        db.FictionPlanBacklogItems.Add(backlog);
        db.Conversations.Add(conversation);
        db.ConversationPlans.Add(conversationPlan);
        db.ConversationTasks.Add(task);
        await db.SaveChangesAsync();

        var jobClient = Substitute.For<IFictionWeaverJobClient>();
        var scheduler = new FictionBacklogScheduler(db, jobClient, NullLogger<FictionBacklogScheduler>.Instance);
        var controller = new FictionPlansController(
            db,
            Substitute.For<Cognition.Clients.Tools.Fiction.Lifecycle.ICharacterLifecycleService>(),
            Substitute.For<Cognition.Clients.Tools.Fiction.Authoring.IAuthorPersonaRegistry>(),
            scheduler,
            Substitute.For<Cognition.Api.Infrastructure.Planning.IFictionPlanCreator>(),
            Microsoft.Extensions.Options.Options.Create(new FictionAutomationOptions()));

        var resumeRequest = new FictionPlansController.ResumeBacklogRequest(
            conversation.Id,
            conversationPlan.Id,
            agentId,
            providerId,
            modelId,
            task.Id,
            "main");

        var response = await controller.ResumeBacklog(plan.Id, backlog.BacklogId, resumeRequest, CancellationToken.None);
        response.Result.Should().BeOfType<OkObjectResult>();

        // Scheduler should have queued lore fulfillment
        jobClient.Received(1).EnqueueLoreFulfillment(
            plan.Id,
            requirement.Id,
            agentId,
            resumeRequest.ConversationId,
            providerId,
            modelId,
            "main",
            Arg.Any<IReadOnlyDictionary<string, string>>());

        // Now execute the queued job manually using FictionWeaverJobs
        var bus = Substitute.For<IBus>();
        var notifier = Substitute.For<IPlanProgressNotifier>();
        var workflowLogger = new WorkflowEventLogger(db, true);
        var scopePaths = ScopePathBuilderTestHelper.CreateBuilder();
        var jobs = new FictionWeaverJobs(db, Array.Empty<IFictionPhaseRunner>(), bus, notifier, workflowLogger, scheduler, scopePaths, NullLogger<FictionWeaverJobs>.Instance);

        await jobs.RunLoreFulfillmentAsync(
            plan.Id,
            requirement.Id,
            agentId,
            resumeRequest.ConversationId,
            providerId,
            modelId,
            branchSlug: "main",
            metadata: new Dictionary<string, string> { ["autoFulfillment"] = "true" },
            cancellationToken: CancellationToken.None);

        var updatedRequirement = await db.FictionLoreRequirements.SingleAsync(r => r.Id == requirement.Id);
        updatedRequirement.Status.Should().Be(FictionLoreRequirementStatus.Ready);
        updatedRequirement.WorldBibleEntryId.Should().NotBeNull();

        var events = await db.WorkflowEvents.Where(e => e.Kind == "fiction.lore.fulfillment").ToListAsync();
        events.Should().NotBeEmpty();
    }
}
