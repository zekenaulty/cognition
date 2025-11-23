using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Controllers;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Cognition.Api.Tests.Controllers;

public class BacklogContractTests
{
    [Fact]
    public async Task ResumeBacklog_logs_contract_event_on_metadata_mismatch()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Contract Plan" };
        var backlog = new FictionPlanBacklogItem
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            BacklogId = "b1",
            Description = "Test backlog",
            Status = FictionPlanBacklogStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        plan.Backlog.Add(backlog);

        var conversation = new Conversation { Id = Guid.NewGuid(), AgentId = Guid.NewGuid(), Title = "Conv" };
        var planConversation = new ConversationPlan
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            PersonaId = Guid.NewGuid(),
            Title = "PlanConv"
        };
        var task = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = planConversation.Id,
            ConversationPlan = planConversation,
            BacklogItemId = backlog.BacklogId,
            StepNumber = 1,
            Status = "Pending",
            ProviderId = Guid.NewGuid(),
            AgentId = Guid.NewGuid(),
            ModelId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        planConversation.Tasks.Add(task);

        db.FictionPlans.Add(plan);
        db.FictionPlanBacklogItems.Add(backlog);
        db.Conversations.Add(conversation);
        db.ConversationPlans.Add(planConversation);
        db.ConversationTasks.Add(task);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var resumeRequest = new FictionPlansController.ResumeBacklogRequest(
            ConversationId: conversation.Id,
            ConversationPlanId: planConversation.Id,
            AgentId: Guid.NewGuid(), // mismatch on purpose
            ProviderId: Guid.NewGuid(),
            ModelId: Guid.NewGuid(),
            TaskId: task.Id,
            BranchSlug: "main");

        var result = await controller.ResumeBacklog(plan.Id, backlog.BacklogId, resumeRequest, CancellationToken.None);
        result.Result.Should().BeOfType<BadRequestObjectResult>();

        var contractEvents = await db.WorkflowEvents.Where(e => e.Kind == "fiction.backlog.contract").ToListAsync();
        contractEvents.Should().ContainSingle();
        var payload = contractEvents[0].Payload;
        payload.Value<string>("code").Should().Contain("mismatch");
    }

    private static FictionPlansController CreateController(CognitionDbContext db)
        => new FictionPlansController(
            db,
            Substitute.For<Cognition.Clients.Tools.Fiction.Lifecycle.ICharacterLifecycleService>(),
            Substitute.For<Cognition.Clients.Tools.Fiction.Authoring.IAuthorPersonaRegistry>(),
            Substitute.For<Cognition.Jobs.IFictionBacklogScheduler>(),
            Substitute.For<Cognition.Api.Infrastructure.Planning.IFictionPlanCreator>(),
            Microsoft.Extensions.Options.Options.Create(new Cognition.Jobs.FictionAutomationOptions()));
}
