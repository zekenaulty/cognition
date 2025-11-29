using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Controllers;
using Cognition.Api.Infrastructure.Planning;
using Cognition.Clients.Tools.Fiction.Authoring;
using Cognition.Clients.Tools.Fiction.Lifecycle;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Jobs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace Cognition.Api.Tests.Controllers;

public class FictionPlansControllerTests
{
    [Fact]
    public async Task GetRoster_ReturnsNotFound_WhenPlanMissing()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var controller = CreateController(db);

        var result = await controller.GetRoster(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Plan_workflow_resume_lore_and_obligation_resolution()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var project = new FictionProject { Id = Guid.NewGuid(), Title = "Saga" };
        var plan = new FictionPlan { Id = Guid.NewGuid(), FictionProjectId = project.Id, FictionProject = project, Name = "Saga Draft", PrimaryBranchSlug = "main" };
        var backlog = new FictionPlanBacklogItem
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            BacklogId = "outline-core-conflicts",
            Description = "Outline conflicts",
            Status = FictionPlanBacklogStatus.InProgress
        };
        var conversationPlan = new ConversationPlan
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            PersonaId = Guid.NewGuid(),
            Title = "Backlog Conversation",
            CreatedAt = DateTime.UtcNow,
            Tasks = new List<ConversationTask>()
        };
        var task = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = conversationPlan.Id,
            StepNumber = 1,
            ToolName = "fiction.weaver.visionPlanner",
            BacklogItemId = backlog.BacklogId,
            Status = "Failed",
            ArgsJson = "{}",
            CreatedAt = DateTime.UtcNow
        };
        conversationPlan.Tasks.Add(task);
        plan.CurrentConversationPlanId = conversationPlan.Id;

        var requirement = new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            RequirementSlug = "fracture-gate",
            Title = "Fracture Gate Protocol",
            Status = FictionLoreRequirementStatus.Planned
        };

        var persona = new Persona { Id = Guid.NewGuid(), Name = "Architect", Role = "planner" };
        var obligation = new FictionPersonaObligation
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            PersonaId = persona.Id,
            Persona = persona,
            ObligationSlug = "story-hook",
            Title = "Document fracture fallout",
            Description = "Capture consequences before next scene.",
            SourceBacklogId = backlog.BacklogId,
            Status = FictionPersonaObligationStatus.Open
        };

        db.FictionProjects.Add(project);
        db.FictionPlans.Add(plan);
        db.FictionPlanBacklogItems.Add(backlog);
        db.ConversationPlans.Add(conversationPlan);
        db.FictionLoreRequirements.Add(requirement);
        db.Personas.Add(persona);
        db.FictionPersonaObligations.Add(obligation);
        await db.SaveChangesAsync();

        var lifecycle = Substitute.For<ICharacterLifecycleService>();
        lifecycle.ProcessAsync(Arg.Any<CharacterLifecycleRequest>(), Arg.Any<CancellationToken>())
            .Returns(CharacterLifecycleResult.Empty);

        var controller = CreateController(db, lifecycle, registry: null, backlogScheduler: Substitute.For<IFictionBacklogScheduler>());

        var resumeRequest = new FictionPlansController.ResumeBacklogRequest(
            conversationPlan.ConversationId,
            conversationPlan.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            task.Id,
            "main");
        await controller.ResumeBacklog(plan.Id, backlog.BacklogId, resumeRequest, CancellationToken.None);

        var fulfillRequest = new FictionPlansController.FulfillLoreRequirementRequest(
            WorldBibleEntryId: null,
            Notes: "Automated via test",
            ConversationId: Guid.NewGuid(),
            PlanPassId: Guid.NewGuid(),
            BranchSlug: "main",
            BranchLineage: new[] { "main" },
            Source: "test");
        await controller.FulfillLoreRequirement(plan.Id, requirement.Id, fulfillRequest, CancellationToken.None);

        var obligationRequest = new FictionPlansController.ResolvePersonaObligationRequest(
            Notes: "Captured fallout in lore notes.",
            Source: "console",
            Action: "resolve");
        await controller.ResolvePersonaObligation(plan.Id, obligation.Id, obligationRequest, CancellationToken.None);

        (await db.FictionPlanBacklogItems.SingleAsync(b => b.Id == backlog.Id)).Status.Should().Be(FictionPlanBacklogStatus.Pending);
        (await db.FictionLoreRequirements.SingleAsync(r => r.Id == requirement.Id)).Status.Should().Be(FictionLoreRequirementStatus.Ready);
        var updatedObligation = await db.FictionPersonaObligations.SingleAsync(o => o.Id == obligation.Id);
        updatedObligation.Status.Should().Be(FictionPersonaObligationStatus.Resolved);
        updatedObligation.ResolvedAtUtc.Should().NotBeNull();
        updatedObligation.MetadataJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPlans_ReturnsOrderedPlans()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var project = new FictionProject { Id = Guid.NewGuid(), Title = "Atlas" };
        var planA = new FictionPlan { Id = Guid.NewGuid(), FictionProjectId = project.Id, FictionProject = project, Name = "Plan A", Status = FictionPlanStatus.InProgress };
        var planB = new FictionPlan { Id = Guid.NewGuid(), FictionProjectId = project.Id, FictionProject = project, Name = "Plan B", Status = FictionPlanStatus.Draft };
        db.FictionProjects.Add(project);
        db.FictionPlans.AddRange(planB, planA);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetPlans(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var plans = ok!.Value as IReadOnlyList<FictionPlansController.FictionPlanSummary>;
        plans.Should().NotBeNull();
        plans!.Should().HaveCount(2);
        plans![0].Name.Should().Be("Plan A");
        plans![1].Name.Should().Be("Plan B");
    }

    [Fact]
    public async Task CreatePlan_ReturnsSummary_OnSuccess()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var project = new FictionProject { Id = Guid.NewGuid(), Title = "Atlas" };
        var plan = new FictionPlan { Id = Guid.NewGuid(), FictionProjectId = project.Id, FictionProject = project, Name = "New Saga" };
        var creator = Substitute.For<IFictionPlanCreator>();
        creator.CreatePlanAsync(Arg.Any<FictionPlanCreationOptions>(), Arg.Any<CancellationToken>())
            .Returns(plan);

        var controller = CreateController(db, planCreator: creator);
        var request = new FictionPlansController.CreateFictionPlanRequest(
            ProjectId: project.Id,
            ProjectTitle: null,
            ProjectLogline: null,
            Name: "New Saga",
            Description: null,
            BranchSlug: "main",
            PersonaId: Guid.NewGuid(),
            AgentId: Guid.NewGuid());

        var response = await controller.CreatePlan(request, CancellationToken.None);

        var created = response.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        var summary = created!.Value as FictionPlansController.FictionPlanSummary;
        summary.Should().NotBeNull();
        summary!.Id.Should().Be(plan.Id);
        summary.Name.Should().Be("New Saga");
        summary.ProjectTitle.Should().Be("Atlas");
        await creator.Received().CreatePlanAsync(Arg.Any<FictionPlanCreationOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePlan_ReturnsBadRequest_OnValidationFailure()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var creator = Substitute.For<IFictionPlanCreator>();
        creator.CreatePlanAsync(Arg.Any<FictionPlanCreationOptions>(), Arg.Any<CancellationToken>())
            .Returns<Task<FictionPlan>>(_ => throw new ValidationException("invalid"));

        var controller = CreateController(db, planCreator: creator);
        var request = new FictionPlansController.CreateFictionPlanRequest(
            ProjectId: Guid.NewGuid(),
            ProjectTitle: null,
            ProjectLogline: null,
            Name: "Saga",
            Description: null,
            BranchSlug: null,
            PersonaId: Guid.NewGuid(),
            AgentId: null);

        var response = await controller.CreatePlan(request, CancellationToken.None);

        var badRequest = response.Result as BadRequestObjectResult;
        badRequest.Should().NotBeNull();
        badRequest!.Value.Should().Be("invalid");
    }

    [Fact]
    public async Task GetRoster_ReturnsCharactersAndLore()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var project = new FictionProject { Id = Guid.NewGuid(), Title = "Atlas", Status = FictionProjectStatus.Active };
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = project.Id,
            FictionProject = project,
            Name = "Atlas Draft",
            PrimaryBranchSlug = "main"
        };

        var persona = new Persona { Id = Guid.NewGuid(), Name = "Captain Mira", Role = "Protagonist", Voice = "Measured" };
        var agent = new Agent { Id = Guid.NewGuid(), PersonaId = persona.Id, Persona = persona, RolePlay = true };
        var worldBible = new FictionWorldBible { Id = Guid.NewGuid(), FictionPlanId = plan.Id, FictionPlan = plan, Domain = "core", BranchSlug = "main" };
        var entry = new FictionWorldBibleEntry
        {
            Id = Guid.NewGuid(),
            FictionWorldBibleId = worldBible.Id,
            FictionWorldBible = worldBible,
            EntrySlug = "characters:captain-mira",
            EntryName = "Captain Mira",
            Content = new FictionWorldBibleEntryContent { Category = "characters", Summary = "Lore summary", Status = "active", ContinuityNotes = new[] { "note" } },
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var character = new FictionCharacter
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            Slug = "captain-mira",
            DisplayName = "Captain Mira",
            Role = "protagonist",
            Importance = "high",
            Summary = "Relentless captain",
            PersonaId = persona.Id,
            Persona = persona,
            AgentId = agent.Id,
            Agent = agent,
            WorldBibleEntryId = entry.Id,
            WorldBibleEntry = entry,
            ProvenanceJson = "{\"source\":\"vision\",\"branchSlug\":\"draft\",\"branchLineage\":[\"main\",\"draft\"]}",
            CreatedAtUtc = DateTime.UtcNow
        };

        var lore = new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            RequirementSlug = "fracture-gate",
            Title = "Fracture Gate Protocol",
            Status = FictionLoreRequirementStatus.Planned,
            MetadataJson = "{\"requiredFor\":[\"scroll\"],\"branchSlug\":\"draft\",\"branchLineage\":[\"main\",\"draft\"]}",
            WorldBibleEntryId = entry.Id,
            WorldBibleEntry = entry,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.FictionProjects.Add(project);
        db.FictionPlans.Add(plan);
        db.Personas.Add(persona);
        db.Agents.Add(agent);
        db.FictionWorldBibles.Add(worldBible);
        db.FictionWorldBibleEntries.Add(entry);
        db.FictionCharacters.Add(character);
        db.FictionLoreRequirements.Add(lore);
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var action = await controller.GetRoster(plan.Id, CancellationToken.None);

        var ok = action.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var roster = ok!.Value as FictionPlansController.FictionPlanRosterResponse;
        roster.Should().NotBeNull();
        roster!.PlanId.Should().Be(plan.Id);
        roster.PlanName.Should().Be("Atlas Draft");
        roster.ProjectTitle.Should().Be("Atlas");
        roster.Characters.Should().ContainSingle();
        var rosterCharacter = roster.Characters[0];
        rosterCharacter.DisplayName.Should().Be("Captain Mira");
        rosterCharacter.Persona.Should().NotBeNull();
        rosterCharacter.Persona!.Name.Should().Be("Captain Mira");
        rosterCharacter.WorldBible.Should().NotBeNull();
        rosterCharacter.WorldBible!.EntrySlug.Should().Be("characters:captain-mira");
        rosterCharacter.BranchSlug.Should().Be("draft");
        rosterCharacter.BranchLineage.Should().NotBeNull();
        rosterCharacter.BranchLineage!.Should().ContainInOrder("main", "draft");
        roster.LoreRequirements.Should().ContainSingle();
        roster.LoreRequirements[0].RequirementSlug.Should().Be("fracture-gate");
        roster.LoreRequirements[0].WorldBible.Should().NotBeNull();
        roster.LoreRequirements[0].BranchSlug.Should().Be("draft");
        roster.LoreRequirements[0].BranchLineage.Should().NotBeNull();
        roster.LoreRequirements[0].BranchLineage!.Should().ContainInOrder("main", "draft");
    }

    [Fact]
    public async Task GetRoster_IncludesProvenanceMetadata()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var project = new FictionProject { Id = Guid.NewGuid(), Title = "Origin", Status = FictionProjectStatus.Active };
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = project.Id,
            FictionProject = project,
            Name = "Origin Plan",
            PrimaryBranchSlug = "main"
        };

        var persona = new Persona { Id = Guid.NewGuid(), Name = "Scout", Role = "explorer", Voice = "curious" };
        var agent = new Agent { Id = Guid.NewGuid(), PersonaId = persona.Id, Persona = persona, RolePlay = false };
        var worldBible = new FictionWorldBible { Id = Guid.NewGuid(), FictionPlanId = plan.Id, FictionPlan = plan, Domain = "core", BranchSlug = "main" };
        var entry = new FictionWorldBibleEntry
        {
            Id = Guid.NewGuid(),
            FictionWorldBibleId = worldBible.Id,
            FictionWorldBible = worldBible,
            EntrySlug = "lore:origin",
            EntryName = "Origin",
            Content = new FictionWorldBibleEntryContent { Category = "lore", Summary = "Origin lore", Status = "active" },
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var provenance = new JObject
        {
            ["source"] = "vision",
            ["backlogId"] = "outline-core",
            ["planPassId"] = Guid.NewGuid(),
            ["branchLineage"] = new JArray("main", "draft")
        };

        var character = new FictionCharacter
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            Slug = "scout",
            DisplayName = "Scout",
            Role = "support",
            Importance = "medium",
            PersonaId = persona.Id,
            Persona = persona,
            AgentId = agent.Id,
            Agent = agent,
            WorldBibleEntryId = entry.Id,
            WorldBibleEntry = entry,
            ProvenanceJson = provenance.ToString(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var loreMetadata = new JObject
        {
            ["requiredFor"] = new JArray("scroll"),
            ["backlogId"] = "lore-setup",
            ["planPassId"] = Guid.NewGuid(),
            ["branchLineage"] = new JArray("main", "draft")
        };

        var lore = new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            RequirementSlug = "origin-lore",
            Title = "Origin lore",
            Status = FictionLoreRequirementStatus.Blocked,
            MetadataJson = loreMetadata.ToString(),
            CreatedAtUtc = DateTime.UtcNow
        };

        db.FictionProjects.Add(project);
        db.FictionPlans.Add(plan);
        db.Personas.Add(persona);
        db.Agents.Add(agent);
        db.FictionWorldBibles.Add(worldBible);
        db.FictionWorldBibleEntries.Add(entry);
        db.FictionCharacters.Add(character);
        db.FictionLoreRequirements.Add(lore);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var action = await controller.GetRoster(plan.Id, CancellationToken.None);
        var ok = action.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var roster = ok!.Value as FictionPlansController.FictionPlanRosterResponse;
        roster.Should().NotBeNull();

        roster!.Characters.Should().ContainSingle();
        var rosterCharacter = roster.Characters.Single();
        rosterCharacter.Provenance.Should().NotBeNull();
        var characterProvenance = rosterCharacter.Provenance!.Value;
        characterProvenance.TryGetProperty("source", out var sourceProp).Should().BeTrue();
        sourceProp.GetString().Should().Be("vision");
        characterProvenance.TryGetProperty("backlogId", out var backlogProp).Should().BeTrue();
        backlogProp.GetString().Should().Be("outline-core");
        characterProvenance.TryGetProperty("planPassId", out var planPassProp).Should().BeTrue();
        planPassProp.GetGuid().Should().Be(provenance.Value<Guid>("planPassId"));
        rosterCharacter.BranchLineage.Should().ContainInOrder("main", "draft");

        roster.LoreRequirements.Should().ContainSingle();
        var rosterLore = roster.LoreRequirements.Single();
        rosterLore.Metadata.Should().NotBeNull();
        var loreMeta = rosterLore.Metadata!.Value;
        loreMeta.TryGetProperty("backlogId", out var loreBacklogProp).Should().BeTrue();
        loreBacklogProp.GetString().Should().Be("lore-setup");
        loreMeta.TryGetProperty("planPassId", out var lorePassProp).Should().BeTrue();
        lorePassProp.GetGuid().Should().Be(loreMetadata.Value<Guid>("planPassId"));
        rosterLore.BranchLineage.Should().ContainInOrder("main", "draft");
    }

    [Fact]
    public async Task GetBacklog_returns_items_with_task_metadata()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Backlog Plan", PrimaryBranchSlug = "main" };
        var backlog = new FictionPlanBacklogItem
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            BacklogId = "outline-core-conflicts",
            Description = "Outline conflicts",
            Status = FictionPlanBacklogStatus.Pending
        };
        var conversationPlan = new ConversationPlan
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            PersonaId = Guid.NewGuid(),
            Title = "Backlog Conversation",
            CreatedAt = DateTime.UtcNow,
            Tasks = new List<ConversationTask>()
        };
        var agentId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var task = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = conversationPlan.Id,
            StepNumber = 1,
            ToolName = "fiction.weaver.visionPlanner",
            BacklogItemId = backlog.BacklogId,
            Status = "Pending",
            Thought = "Plan vision",
            CreatedAt = DateTime.UtcNow,
            ArgsJson = JsonSerializer.Serialize(new
            {
                conversationId = conversationPlan.ConversationId,
                agentId,
                providerId,
                modelId,
                branchSlug = "draft"
            })
        };
        conversationPlan.Tasks.Add(task);
        plan.CurrentConversationPlanId = conversationPlan.Id;

        db.FictionPlans.Add(plan);
        db.FictionPlanBacklogItems.Add(backlog);
        db.ConversationPlans.Add(conversationPlan);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetBacklog(plan.Id, CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var items = ok!.Value as IReadOnlyList<FictionPlansController.BacklogItemResponse>;
        items.Should().NotBeNull();
        items.Should().ContainSingle();
        var item = items![0];
        item.BacklogId.Should().Be("outline-core-conflicts");
        item.TaskId.Should().Be(task.Id);
        item.StepNumber.Should().Be(1);
        item.ToolName.Should().Be("fiction.weaver.visionPlanner");
        item.ConversationPlanId.Should().Be(conversationPlan.Id);
        item.ConversationId.Should().Be(conversationPlan.ConversationId);
        item.AgentId.Should().Be(agentId);
        item.ProviderId.Should().Be(providerId);
        item.ModelId.Should().Be(modelId);
        item.BranchSlug.Should().Be("draft");
    }

    [Fact]
    public async Task ResumeBacklog_updates_task_and_invokes_scheduler()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Resume Plan", PrimaryBranchSlug = "main" };
        var backlog = new FictionPlanBacklogItem
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            BacklogId = "outline-core-conflicts",
            Description = "Outline conflicts",
            Status = FictionPlanBacklogStatus.InProgress
        };
        var conversationPlan = new ConversationPlan
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            PersonaId = Guid.NewGuid(),
            Title = "Backlog Conversation",
            CreatedAt = DateTime.UtcNow,
            Tasks = new List<ConversationTask>()
        };
        var task = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = conversationPlan.Id,
            StepNumber = 1,
            ToolName = "fiction.weaver.visionPlanner",
            BacklogItemId = backlog.BacklogId,
            Status = "Failed",
            ArgsJson = "{}",
            CreatedAt = DateTime.UtcNow
        };
        conversationPlan.Tasks.Add(task);
        plan.CurrentConversationPlanId = conversationPlan.Id;

        db.FictionPlans.Add(plan);
        db.FictionPlanBacklogItems.Add(backlog);
        db.ConversationPlans.Add(conversationPlan);
        await db.SaveChangesAsync();

        var lifecycle = Substitute.For<ICharacterLifecycleService>();
        var scheduler = Substitute.For<IFictionBacklogScheduler>();
        var controller = CreateController(db, lifecycle, registry: null, backlogScheduler: scheduler);
        var request = new FictionPlansController.ResumeBacklogRequest(
            conversationPlan.ConversationId,
            conversationPlan.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            task.Id,
            "draft");

        var response = await controller.ResumeBacklog(plan.Id, backlog.BacklogId, request, CancellationToken.None);

        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var backlogResponse = ok!.Value as FictionPlansController.BacklogItemResponse;
        backlogResponse.Should().NotBeNull();
        backlogResponse!.TaskStatus.Should().Be("Pending");
        backlogResponse.AgentId.Should().Be(request.AgentId);
        backlogResponse.ProviderId.Should().Be(request.ProviderId);
        backlogResponse.ModelId.Should().Be(request.ModelId);
        backlogResponse.ConversationId.Should().Be(request.ConversationId);
        backlogResponse.ConversationPlanId.Should().Be(request.ConversationPlanId);
        backlogResponse.BranchSlug.Should().Be("draft");

        var updatedTask = await db.ConversationTasks.SingleAsync(t => t.Id == task.Id);
        updatedTask.Status.Should().Be("Pending");
        updatedTask.ProviderId.Should().Be(request.ProviderId);
        updatedTask.AgentId.Should().Be(request.AgentId);
        updatedTask.ModelId.Should().Be(request.ModelId);
        var argsDoc = JsonDocument.Parse(updatedTask.ArgsJson!);
        argsDoc.RootElement.GetProperty("providerId").GetGuid().Should().Be(request.ProviderId);
        argsDoc.RootElement.GetProperty("branchSlug").GetString().Should().Be("draft");

        var updatedBacklog = await db.FictionPlanBacklogItems.SingleAsync(b => b.Id == backlog.Id);
        updatedBacklog.Status.Should().Be(FictionPlanBacklogStatus.Pending);

        await scheduler.Received(1).ScheduleAsync(
            Arg.Is<FictionPlan>(p => p.Id == plan.Id),
            FictionPhase.VisionPlanner,
            Arg.Any<FictionPhaseResult>(),
            Arg.Any<FictionPhaseExecutionContext>(),
            Arg.Any<CancellationToken>());

        var workflowEvent = await db.WorkflowEvents.SingleAsync();
        workflowEvent.Kind.Should().Be("fiction.backlog.action");
        workflowEvent.Payload.Value<Guid>("planId").Should().Be(plan.Id);
        workflowEvent.Payload.Value<string>("action").Should().Be("resume");
        workflowEvent.Payload.Value<string>("backlogId").Should().Be(backlog.BacklogId);
    }

    [Fact]
    public async Task ResumeBacklog_requires_agent_and_provider_metadata()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Metadata Plan", PrimaryBranchSlug = "main" };
        var backlog = new FictionPlanBacklogItem
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            BacklogId = "outline-core-conflicts",
            Description = "Outline conflicts",
            Status = FictionPlanBacklogStatus.InProgress
        };
        var conversationPlan = new ConversationPlan
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            PersonaId = Guid.NewGuid(),
            Title = "Backlog Conversation",
            CreatedAt = DateTime.UtcNow,
            Tasks = new List<ConversationTask>()
        };
        var task = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = conversationPlan.Id,
            StepNumber = 1,
            ToolName = "fiction.weaver.visionPlanner",
            BacklogItemId = backlog.BacklogId,
            Status = "Failed",
            ArgsJson = "{}",
            CreatedAt = DateTime.UtcNow
        };
        conversationPlan.Tasks.Add(task);
        plan.CurrentConversationPlanId = conversationPlan.Id;

        db.FictionPlans.Add(plan);
        db.FictionPlanBacklogItems.Add(backlog);
        db.ConversationPlans.Add(conversationPlan);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Substitute.For<ICharacterLifecycleService>(), registry: null, backlogScheduler: Substitute.For<IFictionBacklogScheduler>());
        var baseRequest = new FictionPlansController.ResumeBacklogRequest(
            conversationPlan.ConversationId,
            conversationPlan.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            task.Id,
            "draft");

        var missingAgentRequest = baseRequest with { AgentId = Guid.Empty };
        var missingAgentResult = await controller.ResumeBacklog(plan.Id, backlog.BacklogId, missingAgentRequest, CancellationToken.None);
        missingAgentResult.Result.Should().BeOfType<BadRequestObjectResult>();

        var missingProviderRequest = baseRequest with { ProviderId = Guid.Empty, AgentId = Guid.NewGuid() };
        var missingProviderResult = await controller.ResumeBacklog(plan.Id, backlog.BacklogId, missingProviderRequest, CancellationToken.None);
        missingProviderResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task FulfillLoreRequirement_updates_status_and_emits_telemetry()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var project = new FictionProject { Id = Guid.NewGuid(), Title = "Nova" };
        var plan = new FictionPlan { Id = Guid.NewGuid(), FictionProjectId = project.Id, FictionProject = project, Name = "Nova Draft", PrimaryBranchSlug = "main" };
        var requirement = new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            RequirementSlug = "fracture-gate",
            Title = "Fracture Gate Protocol",
            Status = FictionLoreRequirementStatus.Planned
        };

        db.FictionProjects.Add(project);
        db.FictionPlans.Add(plan);
        db.FictionLoreRequirements.Add(requirement);
        await db.SaveChangesAsync();

        var lifecycle = Substitute.For<ICharacterLifecycleService>();
        lifecycle.ProcessAsync(Arg.Any<CharacterLifecycleRequest>(), Arg.Any<CancellationToken>())
            .Returns(CharacterLifecycleResult.Empty);

        var controller = CreateController(db, lifecycle);
        var request = new FictionPlansController.FulfillLoreRequirementRequest(
            WorldBibleEntryId: null,
            Notes: "Documented manually",
            ConversationId: Guid.NewGuid(),
            PlanPassId: Guid.NewGuid(),
            BranchSlug: "draft",
            BranchLineage: new[] { "main", "draft" },
            Source: "console");

        var response = await controller.FulfillLoreRequirement(plan.Id, requirement.Id, request, CancellationToken.None);

        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var rosterItem = ok!.Value as FictionPlansController.LoreRequirementRosterItem;
        rosterItem.Should().NotBeNull();
        rosterItem!.Status.Should().Be(FictionLoreRequirementStatus.Ready);
        rosterItem.BranchSlug.Should().Be("draft");

        var updated = await db.FictionLoreRequirements.SingleAsync(r => r.Id == requirement.Id);
        updated.Status.Should().Be(FictionLoreRequirementStatus.Ready);
        _ = lifecycle.Received(1).ProcessAsync(
            Arg.Is<CharacterLifecycleRequest>(r => r.BranchSlug == "draft" && r.Source == "console"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLoreSummary_groups_counts_by_branch()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Lore Plan", PrimaryBranchSlug = "main" };
        var planned = new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            RequirementSlug = "seed-one",
            Title = "Seed One",
            Status = FictionLoreRequirementStatus.Planned,
            MetadataJson = "{\"branchSlug\":\"main\"}"
        };
        var blockedDraft = new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            RequirementSlug = "draft-one",
            Title = "Draft One",
            Status = FictionLoreRequirementStatus.Blocked,
            MetadataJson = "{\"branchSlug\":\"draft\",\"branchLineage\":[\"main\",\"draft\"]}"
        };
        var readyDraft = new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            RequirementSlug = "draft-two",
            Title = "Draft Two",
            Status = FictionLoreRequirementStatus.Ready,
            MetadataJson = "{\"branchSlug\":\"draft\",\"branchLineage\":[\"main\",\"draft\"]}"
        };

        db.FictionPlans.Add(plan);
        db.FictionLoreRequirements.AddRange(planned, blockedDraft, readyDraft);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var response = await controller.GetLoreSummary(plan.Id, CancellationToken.None);

        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var summaries = ok!.Value as IReadOnlyList<FictionPlansController.LoreBranchSummary>;
        summaries.Should().NotBeNull();
        var summaryList = summaries!;
        summaryList.Should().HaveCount(2);
        summaryList.Single(s => s.BranchSlug == "main").Planned.Should().Be(1);
        var draftSummary = summaryList.Single(s => s.BranchSlug == "draft");
        draftSummary.Blocked.Should().Be(1);
        draftSummary.Ready.Should().Be(1);
        draftSummary.BranchLineage.Should().ContainInOrder("main", "draft");
    }

    [Fact]
    public async Task GetAuthorPersona_returns_context_from_registry()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Persona Plan" };
        db.FictionPlans.Add(plan);
        await db.SaveChangesAsync();

        var personaContext = new AuthorPersonaContext(
            Guid.NewGuid(),
            "Author",
            "Summary",
            new[] { "Memory A" },
            new[] { "Note A" });

        var registry = Substitute.For<IAuthorPersonaRegistry>();
        registry.GetForPlanAsync(plan.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AuthorPersonaContext?>(personaContext));

        var controller = CreateController(db, registry: registry);
        var response = await controller.GetAuthorPersona(plan.Id, CancellationToken.None);

        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var payload = ok!.Value as FictionPlansController.AuthorPersonaContextResponse;
        payload.Should().NotBeNull();
        payload!.PersonaName.Should().Be("Author");
        payload.Memories.Should().ContainSingle().Which.Should().Be("Memory A");
        payload.WorldNotes.Should().ContainSingle().Which.Should().Be("Note A");
    }

    [Fact]
    public async Task GetBacklogActions_returns_recent_entries_for_plan()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Action Plan" };
        db.FictionPlans.Add(plan);
        await db.SaveChangesAsync();

        db.WorkflowEvents.Add(new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Kind = "fiction.backlog.action",
            Payload = JObject.FromObject(new
            {
                planId = plan.Id,
                backlogId = "outline-alpha",
                description = "Outline conflicts",
                action = "resume",
                branch = "main",
                actor = "Tester",
                source = "console"
            }),
            Timestamp = DateTime.UtcNow
        });

        db.WorkflowEvents.Add(new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Kind = "fiction.backlog.action",
            Payload = JObject.FromObject(new
            {
                planId = Guid.NewGuid(),
                backlogId = "other-plan",
                action = "resume",
                branch = "alt"
            }),
            Timestamp = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var response = await controller.GetBacklogActions(plan.Id, CancellationToken.None);

        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var items = ok!.Value as IReadOnlyList<FictionPlansController.BacklogActionLogResponse>;
        items.Should().NotBeNull();
        items!.Should().HaveCount(1);
        var log = items![0];
        log.Action.Should().Be("resume");
        log.BacklogId.Should().Be("outline-alpha");
        log.Actor.Should().Be("Tester");
        log.Source.Should().Be("console");
    }

    [Fact]
    public async Task GetLoreHistory_returns_fulfillment_events()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Lore Plan", PrimaryBranchSlug = "main" };
        var requirement = new FictionLoreRequirement
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            RequirementSlug = "stellar-key",
            Title = "Stellar Key",
            Status = FictionLoreRequirementStatus.Blocked,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        db.FictionPlans.Add(plan);
        db.FictionLoreRequirements.Add(requirement);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var fulfillRequest = new FictionPlansController.FulfillLoreRequirementRequest(
            WorldBibleEntryId: Guid.NewGuid(),
            Notes: "captured via console",
            ConversationId: Guid.NewGuid(),
            PlanPassId: Guid.NewGuid(),
            BranchSlug: "main",
            BranchLineage: new[] { "main" },
            Source: "console");

        await controller.FulfillLoreRequirement(plan.Id, requirement.Id, fulfillRequest, CancellationToken.None);

        db.WorkflowEvents.Add(new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            ConversationId = fulfillRequest.ConversationId ?? Guid.Empty,
            Kind = "fiction.lore.fulfillment",
            Payload = JObject.FromObject(new
            {
                planId = plan.Id,
                requirementId = requirement.Id,
                requirementSlug = requirement.RequirementSlug,
                branch = "main",
                worldBibleEntryId = fulfillRequest.WorldBibleEntryId
            })
        });
        await db.SaveChangesAsync();

        var history = await controller.GetLoreHistory(plan.Id, CancellationToken.None);

        var ok = history.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var payload = ok!.Value as IReadOnlyList<FictionPlansController.LoreFulfillmentLogResponse>;
        payload.Should().NotBeNull();
        payload!.Should().ContainSingle(entry => entry.RequirementSlug == "stellar-key");
        var historyEntry = payload![0];
        historyEntry.Source.Should().Be("api");
        historyEntry.WorldBibleEntryId.Should().Be(fulfillRequest.WorldBibleEntryId);
    }

    [Fact]
    public async Task GetPersonaObligations_returns_entries_for_plan()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Obligation Plan" };
        var persona = new Persona { Id = Guid.NewGuid(), Name = "Archivist Dera", Role = "Keeper" };
        var character = new FictionCharacter
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            PersonaId = persona.Id,
            Persona = persona,
            Slug = "archivist-dera",
            DisplayName = "Archivist Dera"
        };
        var obligation = new FictionPersonaObligation
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            PersonaId = persona.Id,
            Persona = persona,
            FictionCharacterId = character.Id,
            FictionCharacter = character,
            ObligationSlug = "protect-codex",
            Title = "Protect the Whisperglass Codex",
            Description = "Keep the codex hidden.",
            Status = FictionPersonaObligationStatus.Open,
            SourcePhase = "vision",
            BranchSlug = "main",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-10)
        };

        db.FictionPlans.Add(plan);
        db.Personas.Add(persona);
        db.FictionCharacters.Add(character);
        db.FictionPersonaObligations.Add(obligation);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetPersonaObligations(plan.Id, cancellationToken: CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var payload = ok!.Value as FictionPlansController.PersonaObligationListResponse;
        payload.Should().NotBeNull();
        payload!.Items.Should().ContainSingle();
        payload.TotalCount.Should().Be(1);
        var obligationResponse = payload.Items[0];
        obligationResponse.Title.Should().Be("Protect the Whisperglass Codex");
        obligationResponse.Status.Should().Be(FictionPersonaObligationStatus.Open);
        obligationResponse.PersonaName.Should().Be("Archivist Dera");
    }

    [Fact]
    public async Task GetPersonaObligations_supports_pagination()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Pagination Plan" };
        var persona = new Persona { Id = Guid.NewGuid(), Name = "Planner", Role = "Support" };
        db.FictionPlans.Add(plan);
        db.Personas.Add(persona);
        for (var i = 0; i < 3; i++)
        {
            db.FictionPersonaObligations.Add(new FictionPersonaObligation
            {
                Id = Guid.NewGuid(),
                FictionPlanId = plan.Id,
                FictionPlan = plan,
                PersonaId = persona.Id,
                Persona = persona,
                ObligationSlug = $"obligation-{i}",
                Title = $"Obligation {i}",
                Status = FictionPersonaObligationStatus.Open,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetPersonaObligations(plan.Id, page: 2, pageSize: 1, cancellationToken: CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var payload = ok!.Value as FictionPlansController.PersonaObligationListResponse;
        payload.Should().NotBeNull();
        payload!.TotalCount.Should().Be(3);
        payload.Page.Should().Be(2);
        payload.PageSize.Should().Be(1);
        payload.Items.Should().ContainSingle();
        payload.Items[0].Title.Should().Be("Obligation 1");
    }

    [Fact]
    public async Task ResolvePersonaObligation_marks_resolved_and_logs_event()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Resolve Plan" };
        var persona = new Persona { Id = Guid.NewGuid(), Name = "Captain Mira", Role = "Lead" };
        var character = new FictionCharacter
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            PersonaId = persona.Id,
            Persona = persona,
            Slug = "captain-mira",
            DisplayName = "Captain Mira"
        };
        var obligation = new FictionPersonaObligation
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            PersonaId = persona.Id,
            Persona = persona,
            FictionCharacterId = character.Id,
            FictionCharacter = character,
            ObligationSlug = "repay-debt",
            Title = "Repay Admiral Kerr",
            Status = FictionPersonaObligationStatus.Open,
            SourcePhase = "vision",
            BranchSlug = "main",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
        };

        db.FictionPlans.Add(plan);
        db.Personas.Add(persona);
        db.FictionCharacters.Add(character);
        db.FictionPersonaObligations.Add(obligation);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var request = new FictionPlansController.ResolvePersonaObligationRequest("Debt cleared.", "console", null, obligation.SourceBacklogId ?? "backlog-1", TaskId: "task-123", ConversationId: Guid.NewGuid().ToString());
        var response = await controller.ResolvePersonaObligation(plan.Id, obligation.Id, request, CancellationToken.None);

        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var updated = await db.FictionPersonaObligations.SingleAsync(o => o.Id == obligation.Id);
        updated.Status.Should().Be(FictionPersonaObligationStatus.Resolved);
        updated.ResolvedAtUtc.Should().NotBeNull();
        updated.MetadataJson.Should().NotBeNull();
        var metadata = JsonDocument.Parse(updated.MetadataJson!);
        metadata.RootElement.GetProperty("resolutionNotes").EnumerateArray().First().GetProperty("note").GetString().Should().Be("Debt cleared.");
        metadata.RootElement.GetProperty("resolvedSource").GetString().Should().Be("console");
        metadata.RootElement.GetProperty("resolvedBacklogId").GetString().Should().Be(obligation.SourceBacklogId ?? "backlog-1");
        metadata.RootElement.GetProperty("resolvedTaskId").GetString().Should().Be("task-123");

        var workflow = await db.WorkflowEvents.SingleAsync();
        workflow.Kind.Should().Be("fiction.persona.obligation");
        workflow.Payload.Value<string>("action").Should().Be("resolved");
        workflow.Payload["obligationId"]?.ToString().Should().Be(obligation.Id.ToString());
    }

    [Fact]
    public async Task ResolvePersonaObligation_allows_dismiss_action()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Dismiss Plan" };
        var persona = new Persona { Id = Guid.NewGuid(), Name = "Analyst Brek", Role = "Support" };
        var character = new FictionCharacter
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            PersonaId = persona.Id,
            Persona = persona,
            Slug = "analyst-brek",
            DisplayName = "Analyst Brek"
        };
        var obligation = new FictionPersonaObligation
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            PersonaId = persona.Id,
            Persona = persona,
            FictionCharacterId = character.Id,
            FictionCharacter = character,
            ObligationSlug = "investigate-signal",
            Title = "Investigate rogue signal",
            Status = FictionPersonaObligationStatus.Open,
            SourcePhase = "vision",
            BranchSlug = "main",
            SourceBacklogId = "draft-scout-report",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-3)
        };

        db.FictionPlans.Add(plan);
        db.Personas.Add(persona);
        db.FictionCharacters.Add(character);
        db.FictionPersonaObligations.Add(obligation);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var request = new FictionPlansController.ResolvePersonaObligationRequest("Dismissed due to scope change.", "console", "dismiss", obligation.SourceBacklogId ?? "backlog-2");
        var response = await controller.ResolvePersonaObligation(plan.Id, obligation.Id, request, CancellationToken.None);

        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();

        var updated = await db.FictionPersonaObligations.SingleAsync(o => o.Id == obligation.Id);
        updated.Status.Should().Be(FictionPersonaObligationStatus.Dismissed);
        updated.ResolvedAtUtc.Should().NotBeNull();
        updated.SourceBacklogId.Should().Be("draft-scout-report");

        var workflow = await db.WorkflowEvents.SingleAsync();
        workflow.Payload.Value<string>("action").Should().Be("dismissed");
        workflow.Payload.Value<string>("sourceBacklogId").Should().Be("draft-scout-report");
    }

    [Fact]
    public async Task ResolvePersonaObligation_requires_notes()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Notes Plan" };
        var persona = new Persona { Id = Guid.NewGuid(), Name = "Archivist" };
        var obligation = new FictionPersonaObligation
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            PersonaId = persona.Id,
            Persona = persona,
            ObligationSlug = "log-notes",
            Title = "Log notes",
            Status = FictionPersonaObligationStatus.Open,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        db.FictionPlans.Add(plan);
        db.Personas.Add(persona);
        db.FictionPersonaObligations.Add(obligation);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var request = new FictionPlansController.ResolvePersonaObligationRequest(null, "console", "resolve");
        var response = await controller.ResolvePersonaObligation(plan.Id, obligation.Id, request, CancellationToken.None);

        response.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResolvePersonaObligation_sets_voice_drift_and_context()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var plan = new FictionPlan { Id = Guid.NewGuid(), Name = "Drift Plan", PrimaryBranchSlug = "beta" };
        var persona = new Persona { Id = Guid.NewGuid(), Name = "Navigator" };
        var obligation = new FictionPersonaObligation
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            PersonaId = persona.Id,
            Persona = persona,
            ObligationSlug = "course-correct",
            Title = "Course-correct voice",
            Status = FictionPersonaObligationStatus.Open,
            SourceBacklogId = "backlog-course",
            BranchSlug = "beta",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-5)
        };
        db.FictionPlans.Add(plan);
        db.Personas.Add(persona);
        db.FictionPersonaObligations.Add(obligation);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var request = new FictionPlansController.ResolvePersonaObligationRequest(
            "Adjusted voice to match persona.",
            "console",
            "resolve",
            BacklogId: "backlog-course",
            ConversationId: Guid.NewGuid().ToString(),
            VoiceDrift: true);

        var result = await controller.ResolvePersonaObligation(plan.Id, obligation.Id, request, CancellationToken.None);
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();

        var updated = await db.FictionPersonaObligations.SingleAsync(o => o.Id == obligation.Id);
        updated.Status.Should().Be(FictionPersonaObligationStatus.Resolved);
        var metadata = JsonDocument.Parse(updated.MetadataJson!);
        metadata.RootElement.GetProperty("voiceDrift").GetBoolean().Should().BeTrue();
        metadata.RootElement.GetProperty("resolvedBacklogId").GetString().Should().Be("backlog-course");

        var workflow = await db.WorkflowEvents.SingleAsync();
        workflow.Kind.Should().Be("fiction.persona.obligation");
        workflow.Payload.Value<bool?>("voiceDrift").Should().BeTrue();
        workflow.Payload.Value<string>("resolvedBacklogId").Should().Be("backlog-course");
    }

    private static FictionPlansController CreateController(
        CognitionDbContext db,
        ICharacterLifecycleService? lifecycle = null,
        IAuthorPersonaRegistry? registry = null,
        IFictionBacklogScheduler? backlogScheduler = null,
        IFictionPlanCreator? planCreator = null,
        Microsoft.Extensions.Options.IOptions<FictionAutomationOptions>? automationOptions = null)
        => new FictionPlansController(
            db,
            lifecycle ?? Substitute.For<ICharacterLifecycleService>(),
            registry ?? Substitute.For<IAuthorPersonaRegistry>(),
            backlogScheduler ?? Substitute.For<IFictionBacklogScheduler>(),
            planCreator ?? Substitute.For<IFictionPlanCreator>(),
            automationOptions ?? Microsoft.Extensions.Options.Options.Create(new FictionAutomationOptions()));
}

internal sealed class FictionPlansTestDbContext : CognitionDbContext
{
    public FictionPlansTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var allowed = new HashSet<Type>
        {
            typeof(FictionProject),
            typeof(FictionPlan),
            typeof(FictionCharacter),
            typeof(FictionLoreRequirement),
            typeof(FictionWorldBible),
            typeof(FictionWorldBibleEntry),
            typeof(FictionPersonaObligation),
            typeof(Persona),
            typeof(Agent),
            typeof(FictionPlanBacklogItem),
            typeof(ConversationPlan),
            typeof(ConversationTask),
            typeof(Conversation),
            typeof(ConversationParticipant),
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
        modelBuilder.Entity<Agent>().Ignore(a => a.ToolBindings);
        modelBuilder.Entity<Persona>().Ignore(p => p.SignatureTraits);
        modelBuilder.Entity<Persona>().Ignore(p => p.NarrativeThemes);
        modelBuilder.Entity<Persona>().Ignore(p => p.DomainExpertise);
        modelBuilder.Entity<Persona>().Ignore(p => p.KnownPersonas);
        modelBuilder.Entity<FictionPlan>().Ignore(p => p.PersonaObligations);
        modelBuilder.Entity<Conversation>().Ignore(c => c.Metadata);
    }
}
