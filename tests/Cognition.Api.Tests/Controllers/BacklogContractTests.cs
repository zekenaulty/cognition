using System;
using System.Collections.Generic;
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
using System.Text.Json;
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

    [Fact]
    public async Task ResumeBacklog_logs_contract_event_when_task_backlog_mismatch()
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
            BacklogItemId = "different-backlog",
            StepNumber = 1,
            Status = "Pending",
            ArgsJson = "{}",
            ProviderId = Guid.NewGuid(),
            AgentId = Guid.NewGuid(),
            ModelId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        planConversation.Tasks.Add(task);
        plan.CurrentConversationPlanId = planConversation.Id;

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
            AgentId: task.AgentId ?? Guid.NewGuid(),
            ProviderId: task.ProviderId ?? Guid.NewGuid(),
            ModelId: task.ModelId ?? Guid.NewGuid(),
            TaskId: task.Id,
            BranchSlug: "main");

        var result = await controller.ResumeBacklog(plan.Id, backlog.BacklogId, resumeRequest, CancellationToken.None);
        result.Result.Should().BeOfType<BadRequestObjectResult>();

        var contractEvents = await db.WorkflowEvents.Where(e => e.Kind == "fiction.backlog.contract").ToListAsync();
        contractEvents.Should().ContainSingle();
        contractEvents[0].Payload.Value<string>("code").Should().Be("task-backlog-mismatch");
    }

    [Fact]
    public async Task ResumeBacklog_logs_contract_event_when_provider_missing()
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
            CreatedAt = DateTime.UtcNow
        };
        planConversation.Tasks.Add(task);
        plan.CurrentConversationPlanId = planConversation.Id;

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
            AgentId: task.AgentId ?? Guid.NewGuid(),
            ProviderId: Guid.Empty,
            ModelId: Guid.NewGuid(),
            TaskId: task.Id,
            BranchSlug: "draft");

        var result = await controller.ResumeBacklog(plan.Id, backlog.BacklogId, resumeRequest, CancellationToken.None);
        result.Result.Should().BeOfType<BadRequestObjectResult>();

        var contractEvents = await db.WorkflowEvents.Where(e => e.Kind == "fiction.backlog.contract").ToListAsync();
        contractEvents.Should().ContainSingle();
        contractEvents[0].Payload.Value<string>("code").Should().Be("missing-agent-or-provider");
        contractEvents[0].Payload.Value<string>("branch").Should().Be("draft");
    }

    [Fact]
    public async Task ResumeBacklog_stamps_conversation_task_metadata_when_valid()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Contract Plan", PrimaryBranchSlug = "main" };
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
        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var task = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = planConversation.Id,
            ConversationPlan = planConversation,
            BacklogItemId = backlog.BacklogId,
            StepNumber = 1,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            ArgsJson = "{}"
        };
        planConversation.Tasks.Add(task);
        plan.CurrentConversationPlanId = planConversation.Id;

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
            AgentId: agentId,
            ProviderId: providerId,
            ModelId: modelId,
            TaskId: task.Id,
            BranchSlug: "feature");

        var result = await controller.ResumeBacklog(plan.Id, backlog.BacklogId, resumeRequest, CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();

        var updatedTask = await db.ConversationTasks.SingleAsync(t => t.Id == task.Id);
        updatedTask.ProviderId.Should().Be(providerId);
        updatedTask.ModelId.Should().Be(modelId);
        updatedTask.AgentId.Should().Be(agentId);
        updatedTask.Status.Should().Be("Pending");

        var args = JsonSerializer.Deserialize<Dictionary<string, object?>>(updatedTask.ArgsJson ?? "{}", new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        args["planId"]!.ToString().Should().Be(plan.Id.ToString());
        args["backlogItemId"]!.ToString().Should().Be(backlog.BacklogId);
        args["conversationPlanId"]!.ToString().Should().Be(planConversation.Id.ToString());
        args["conversationId"]!.ToString().Should().Be(conversation.Id.ToString());
        args["providerId"]!.ToString().Should().Be(providerId.ToString());
        args["agentId"]!.ToString().Should().Be(agentId.ToString());
        args["modelId"]!.ToString().Should().Be(modelId.ToString());
        args["branchSlug"]!.ToString().Should().Be("feature");

        backlog.Status.Should().Be(FictionPlanBacklogStatus.Pending);
        backlog.InProgressAtUtc.Should().BeNull();
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
