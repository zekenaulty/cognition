using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Controllers;
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
        var task = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = conversationPlan.Id,
            StepNumber = 1,
            ToolName = "fiction.weaver.visionPlanner",
            BacklogItemId = backlog.BacklogId,
            Status = "Pending",
            Thought = "Plan vision",
            CreatedAt = DateTime.UtcNow
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

        var updatedTask = await db.ConversationTasks.SingleAsync(t => t.Id == task.Id);
        updatedTask.Status.Should().Be("Pending");
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
    private static FictionPlansController CreateController(
        CognitionDbContext db,
        ICharacterLifecycleService? lifecycle = null,
        IAuthorPersonaRegistry? registry = null,
        IFictionBacklogScheduler? backlogScheduler = null)
        => new FictionPlansController(
            db,
            lifecycle ?? Substitute.For<ICharacterLifecycleService>(),
            registry ?? Substitute.For<IAuthorPersonaRegistry>(),
            backlogScheduler ?? Substitute.For<IFictionBacklogScheduler>());
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
            typeof(Persona),
            typeof(Agent),
            typeof(FictionPlanBacklogItem),
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

        modelBuilder.Entity<Agent>().Ignore(a => a.State);
        modelBuilder.Entity<Agent>().Ignore(a => a.ToolBindings);
        modelBuilder.Entity<Persona>().Ignore(p => p.SignatureTraits);
        modelBuilder.Entity<Persona>().Ignore(p => p.NarrativeThemes);
        modelBuilder.Entity<Persona>().Ignore(p => p.DomainExpertise);
        modelBuilder.Entity<Persona>().Ignore(p => p.KnownPersonas);
    }
}
