using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Cognition.Clients.Tools.Fiction.Lifecycle;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Agents;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cognition.Clients.Tests.Fiction;

public sealed class CharacterLifecycleServiceTests
{
    [Fact]
    public async Task ProcessAsync_mints_persona_agent_and_memory_for_tracked_character()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new CharacterLifecycleTestDbContext(options);
        var project = new FictionProject { Id = Guid.NewGuid(), Title = "My Saga" };
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = project.Id,
            FictionProject = project,
            Name = "Plan A"
        };

        db.FictionProjects.Add(project);
        db.FictionPlans.Add(plan);
        await db.SaveChangesAsync();

        var service = new CharacterLifecycleService(db, NullLogger<CharacterLifecycleService>.Instance);
        var descriptor = new CharacterLifecycleDescriptor(
            Name: "Captain Mira",
            Track: true,
            Slug: "captain-mira",
            Role: "protagonist",
            Importance: "high",
            Summary: "Relentless captain balancing duty and loyalty.",
            Notes: "Owes Admiral Kerr, fears betrayal.");
        var request = new CharacterLifecycleRequest(
            plan.Id,
            ConversationId: null,
            PlanPassId: Guid.NewGuid(),
            new[] { descriptor },
            Array.Empty<LoreRequirementDescriptor>(),
            Source: "vision");

        var result = await service.ProcessAsync(request, CancellationToken.None);

        result.CreatedCharacters.Should().HaveCount(1);
        var character = await db.FictionCharacters.SingleAsync();
        character.DisplayName.Should().Be("Captain Mira");
        character.PersonaId.Should().NotBeNull();
        character.AgentId.Should().NotBeNull();

        var persona = await db.Personas.SingleAsync();
        persona.Name.Should().Be("Captain Mira");
        persona.Background.Should().Contain("Relentless captain");
        persona.Type.Should().Be(PersonaType.RolePlayCharacter);

        var agent = await db.Agents.SingleAsync();
        agent.PersonaId.Should().Be(persona.Id);

        var memory = await db.PersonaMemories.SingleAsync();
        memory.PersonaId.Should().Be(persona.Id);
        memory.Content.Should().Contain("Relentless captain");
        memory.Properties.Should().ContainKey("planId");
        memory.Properties.Should().ContainKey("characterSlug");
    }

    [Fact]
    public async Task ProcessAsync_links_characters_to_existing_world_bible_entry()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new CharacterLifecycleTestDbContext(options);
        var project = new FictionProject { Id = Guid.NewGuid(), Title = "Lore Project" };
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = project.Id,
            FictionProject = project,
            Name = "Plan Lore"
        };

        var bible = new FictionWorldBible
        {
            Id = Guid.NewGuid(),
            FictionPlanId = plan.Id,
            FictionPlan = plan,
            Domain = "core",
            BranchSlug = "main"
        };

        var entry = new FictionWorldBibleEntry
        {
            Id = Guid.NewGuid(),
            FictionWorldBibleId = bible.Id,
            FictionWorldBible = bible,
            EntrySlug = "characters:captain-mira",
            EntryName = "Captain Mira",
            Content = new FictionWorldBibleEntryContent { Summary = "World bible summary." },
            Sequence = 1,
            Version = 1,
            IsActive = true
        };

        db.FictionProjects.Add(project);
        db.FictionPlans.Add(plan);
        db.FictionWorldBibles.Add(bible);
        db.FictionWorldBibleEntries.Add(entry);
        await db.SaveChangesAsync();

        var service = new CharacterLifecycleService(db, NullLogger<CharacterLifecycleService>.Instance);
        var descriptor = new CharacterLifecycleDescriptor(
            Name: "Captain Mira",
            Track: true,
            Slug: "captain-mira",
            Role: "protagonist",
            Importance: "high",
            Summary: "Relentless captain balancing duty and loyalty.",
            Notes: "Owes Admiral Kerr.");

        var request = new CharacterLifecycleRequest(
            plan.Id,
            ConversationId: null,
            PlanPassId: Guid.NewGuid(),
            new[] { descriptor },
            Array.Empty<LoreRequirementDescriptor>(),
            Source: "world-bible");

        await service.ProcessAsync(request, CancellationToken.None);

        var character = await db.FictionCharacters.SingleAsync();
        character.WorldBibleEntryId.Should().Be(entry.Id);
    }

    [Fact]
    public async Task ProcessAsync_includes_branch_metadata_for_characters_and_lore()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new CharacterLifecycleTestDbContext(options);
        var project = new FictionProject { Id = Guid.NewGuid(), Title = "Branch Saga" };
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = project.Id,
            FictionProject = project,
            Name = "Branch Plan",
            PrimaryBranchSlug = "main"
        };

        db.FictionProjects.Add(project);
        db.FictionPlans.Add(plan);
        await db.SaveChangesAsync();

        var service = new CharacterLifecycleService(db, NullLogger<CharacterLifecycleService>.Instance);
        var characterDescriptor = new CharacterLifecycleDescriptor(
            Name: "Captain Mira",
            Track: true,
            Slug: "captain-mira",
            Role: "protagonist",
            Importance: "high",
            Summary: "Relentless captain balancing duty and loyalty.",
            Notes: "Owes Admiral Kerr.");
        var loreDescriptor = new LoreRequirementDescriptor(
            Title: "Fracture Gate Protocol",
            RequirementSlug: "fracture-gate");

        var request = new CharacterLifecycleRequest(
            plan.Id,
            ConversationId: null,
            PlanPassId: Guid.NewGuid(),
            new[] { characterDescriptor },
            new[] { loreDescriptor },
            Source: "vision",
            BranchSlug: "draft",
            BranchLineage: new[] { "main", "draft" });

        await service.ProcessAsync(request, CancellationToken.None);

        var character = await db.FictionCharacters.SingleAsync();
        character.ProvenanceJson.Should().NotBeNull();
        using (var document = JsonDocument.Parse(character.ProvenanceJson!))
        {
            var root = document.RootElement;
            root.GetProperty("branchSlug").GetString().Should().Be("draft");
            root.GetProperty("branchLineage").EnumerateArray().Select(x => x.GetString()).Should().ContainInOrder("main", "draft");
        }

        var lore = await db.FictionLoreRequirements.SingleAsync();
        lore.MetadataJson.Should().NotBeNull();
        using (var metadata = JsonDocument.Parse(lore.MetadataJson!))
        {
            var root = metadata.RootElement;
            root.GetProperty("branchSlug").GetString().Should().Be("draft");
            root.GetProperty("branchLineage").EnumerateArray().Select(x => x.GetString()).Should().ContainInOrder("main", "draft");
        }

        var memory = await db.PersonaMemories.SingleAsync();
        memory.Properties.Should().NotBeNull();
        memory.Properties!.Should().ContainKey("branchSlug");
        memory.Properties!["branchSlug"].Should().Be("draft");
        memory.Properties.Should().ContainKey("branchLineage");
    }
}

internal sealed class CharacterLifecycleTestDbContext : CognitionDbContext
{
    private static readonly HashSet<Type> AllowedEntities = new()
    {
        typeof(FictionProject),
        typeof(FictionPlan),
        typeof(FictionCharacter),
        typeof(FictionLoreRequirement),
        typeof(FictionWorldBible),
        typeof(FictionWorldBibleEntry),
        typeof(Persona),
        typeof(Agent),
        typeof(PersonaMemory)
    };

    public CharacterLifecycleTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (entityType.ClrType is not null && !AllowedEntities.Contains(entityType.ClrType))
            {
                modelBuilder.Ignore(entityType.ClrType);
            }
        }

        modelBuilder.Entity<FictionPlan>().Ignore(x => x.Passes);
        modelBuilder.Entity<FictionPlan>().Ignore(x => x.ChapterBlueprints);
        modelBuilder.Entity<FictionPlan>().Ignore(x => x.Checkpoints);
        modelBuilder.Entity<FictionPlan>().Ignore(x => x.Backlog);
        modelBuilder.Entity<FictionPlan>().Ignore(x => x.Transcripts);
        modelBuilder.Entity<FictionPlan>().Ignore(x => x.StoryMetrics);

        modelBuilder.Entity<Agent>().Ignore(a => a.State);
        modelBuilder.Entity<Persona>().Ignore(p => p.SignatureTraits);
        modelBuilder.Entity<Persona>().Ignore(p => p.NarrativeThemes);
        modelBuilder.Entity<Persona>().Ignore(p => p.DomainExpertise);
        modelBuilder.Entity<Persona>().Ignore(p => p.KnownPersonas);
        modelBuilder.Entity<PersonaMemory>().Ignore(m => m.Properties);
    }
}
