using System.Collections.Generic;
using Cognition.Clients.Tools.Fiction.Authoring;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Agents;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cognition.Clients.Tests.Fiction;

public class AuthorPersonaRegistryTests
{
    [Fact]
    public async Task GetForPlanAsync_returns_summary_memories_and_world_notes()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new AuthorPersonaTestDbContext(options);
        var persona = new Persona
        {
            Id = Guid.NewGuid(),
            Name = "Author One",
            Voice = "Lyrical",
            CommunicationStyle = "Intimate",
            Essence = "Curious optimist",
            Background = "Travel writer"
        };

        var agent = new Agent { Id = Guid.NewGuid(), PersonaId = persona.Id, Persona = persona };
        var conversation = new Conversation { Id = Guid.NewGuid(), AgentId = agent.Id, Agent = agent };
        var conversationPlan = new ConversationPlan
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            PersonaId = persona.Id,
            Persona = persona,
            Title = "Author conversation plan"
        };

        var project = new FictionProject { Id = Guid.NewGuid(), Title = "Author Project" };
        var plan = new FictionPlan
        {
            Id = Guid.NewGuid(),
            FictionProjectId = project.Id,
            FictionProject = project,
            Name = "Plan",
            CurrentConversationPlanId = conversationPlan.Id,
            CurrentConversationPlan = conversationPlan
        };
        project.FictionPlans.Add(plan);

        db.Personas.Add(persona);
        db.Agents.Add(agent);
        db.Conversations.Add(conversation);
        db.ConversationPlans.Add(conversationPlan);
        db.FictionProjects.Add(project);
        db.FictionPlans.Add(plan);
        db.PersonaMemories.AddRange(
            new PersonaMemory { Id = Guid.NewGuid(), PersonaId = persona.Id, Title = "Memory A", Content = "Content A" },
            new PersonaMemory { Id = Guid.NewGuid(), PersonaId = persona.Id, Title = "Memory B", Content = "Content B" });

        var bible = new FictionWorldBible { Id = Guid.NewGuid(), FictionPlanId = plan.Id, FictionPlan = plan, Domain = "core" };
        db.FictionWorldBibles.Add(bible);
        db.FictionWorldBibleEntries.Add(new FictionWorldBibleEntry
        {
            Id = Guid.NewGuid(),
            FictionWorldBibleId = bible.Id,
            FictionWorldBible = bible,
            EntrySlug = "entry",
            EntryName = "Entry",
            Content = new FictionWorldBibleEntryContent { Summary = "Lore summary" }
        });

        await db.SaveChangesAsync();

        var registry = new AuthorPersonaRegistry(db, Options.Create(new AuthorPersonaOptions { MemoryWindow = 2, WorldNotesWindow = 1 }), NullLogger<AuthorPersonaRegistry>.Instance);
        var context = await registry.GetForPlanAsync(plan.Id);

        Assert.NotNull(context);
        context!.PersonaName.Should().Be("Author One");
        context.Summary.Should().Contain("Lyrical");
        context.Memories.Should().Contain(m => m.Contains("Memory A"));
        context.WorldNotes.Should().ContainSingle();
    }

    [Fact]
    public async Task AppendMemoryAsync_persists_persona_memory_with_properties()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new AuthorPersonaTestDbContext(options);
        var persona = new Persona { Id = Guid.NewGuid(), Name = "Author Two" };
        db.Personas.Add(persona);
        await db.SaveChangesAsync();

        var registry = new AuthorPersonaRegistry(db, Options.Create(new AuthorPersonaOptions()), NullLogger<AuthorPersonaRegistry>.Instance);
        var entry = new AuthorPersonaMemoryEntry(
            "Scene Draft",
            "Scene summary",
            PlanId: Guid.NewGuid(),
            ScrollId: Guid.NewGuid(),
            SceneId: Guid.NewGuid(),
            SourcePhase: "scene");

        await registry.AppendMemoryAsync(persona.Id, entry);

        var saved = await db.PersonaMemories.SingleAsync(m => m.PersonaId == persona.Id);
        saved.Title.Should().Be("Scene Draft");
        saved.Content.Should().Contain("Scene summary");
        saved.Source.Should().Be("scene");
        saved.Properties.Should().ContainKey("planId");
        saved.Properties.Should().ContainKey("scrollId");
        saved.Properties.Should().ContainKey("sceneId");
    }
}

internal sealed class AuthorPersonaTestDbContext : CognitionDbContext
{
    private static readonly HashSet<Type> Allowed = new()
    {
        typeof(Persona),
        typeof(Agent),
        typeof(Conversation),
        typeof(ConversationPlan),
        typeof(FictionProject),
        typeof(FictionPlan),
        typeof(PersonaMemory),
        typeof(FictionWorldBible),
        typeof(FictionWorldBibleEntry)
    };

    public AuthorPersonaTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (entityType.ClrType is not null && !Allowed.Contains(entityType.ClrType))
            {
                modelBuilder.Ignore(entityType.ClrType);
            }
        }

        modelBuilder.Entity<Agent>().Ignore(a => a.State);
        modelBuilder.Entity<Conversation>().Ignore(c => c.Metadata);
        modelBuilder.Entity<ConversationPlan>().Ignore(p => p.Tasks);
        modelBuilder.Entity<PersonaMemory>().Ignore(m => m.Properties);
    }
}
