using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Controllers;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        var controller = new FictionPlansController(db);

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

        var controller = new FictionPlansController(db);
        var result = await controller.GetPlans(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var plans = ok!.Value as IReadOnlyList<FictionPlansController.FictionPlanSummary>;
        plans.Should().NotBeNull();
        plans!.Should().HaveCount(2);
        plans[0].Name.Should().Be("Plan A");
        plans[1].Name.Should().Be("Plan B");
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
            ProvenanceJson = "{\"source\":\"vision\"}",
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
            MetadataJson = "{\"requiredFor\":[\"scroll\"]}",
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

        var controller = new FictionPlansController(db);

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
        roster.LoreRequirements.Should().ContainSingle();
        roster.LoreRequirements[0].RequirementSlug.Should().Be("fracture-gate");
        roster.LoreRequirements[0].WorldBible.Should().NotBeNull();
    }
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
            typeof(Agent)
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
