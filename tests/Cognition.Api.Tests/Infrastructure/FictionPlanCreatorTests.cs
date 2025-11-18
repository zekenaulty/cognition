using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Planning;
using Cognition.Api.Tests.Controllers;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cognition.Api.Tests.Infrastructure;

public class FictionPlanCreatorTests
{
    [Fact]
    public async Task CreatePlan_CreatesBacklogAndConversation()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var project = new FictionProject { Id = Guid.NewGuid(), Title = "Atlas" };
        var persona = new Persona { Id = Guid.NewGuid(), Name = "Author" };
        var agent = new Agent { Id = Guid.NewGuid(), PersonaId = persona.Id, Persona = persona };
        db.FictionProjects.Add(project);
        db.Personas.Add(persona);
        db.Agents.Add(agent);
        await db.SaveChangesAsync();

        var creator = new FictionPlanCreator(db, NullLogger<FictionPlanCreator>.Instance);
        var created = await creator.CreatePlanAsync(new FictionPlanCreationOptions(
            project.Id,
            null,
            null,
            "Pilot Plan",
            "desc",
            "branch-alpha",
            persona.Id,
            agent.Id), CancellationToken.None);

        created.Id.Should().NotBe(Guid.Empty);
        created.CurrentConversationPlanId.Should().NotBeNull();

        (await db.FictionPlanBacklogItems.Where(b => b.FictionPlanId == created.Id).CountAsync()).Should().Be(3);
        (await db.ConversationPlans.CountAsync()).Should().Be(1);
        (await db.Conversations.CountAsync()).Should().Be(1);
        (await db.ConversationTasks.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task CreatePlan_CreatesProjectWhenMissingProjectId()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var persona = new Persona { Id = Guid.NewGuid(), Name = "Author" };
        var agent = new Agent { Id = Guid.NewGuid(), PersonaId = persona.Id, Persona = persona };
        db.Personas.Add(persona);
        db.Agents.Add(agent);
        await db.SaveChangesAsync();

        var creator = new FictionPlanCreator(db, NullLogger<FictionPlanCreator>.Instance);
        var created = await creator.CreatePlanAsync(new FictionPlanCreationOptions(
            null,
            "New Project",
            "World logline",
            "Pilot Plan",
            null,
            null,
            persona.Id,
            agent.Id), CancellationToken.None);

        var storedProject = await db.FictionProjects.SingleAsync();
        storedProject.Title.Should().Be("New Project");
        created.FictionProjectId.Should().Be(storedProject.Id);
    }

    [Fact]
    public async Task CreatePlan_ThrowsWhenPersonaMissing()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var project = new FictionProject { Id = Guid.NewGuid(), Title = "Atlas" };
        db.FictionProjects.Add(project);
        await db.SaveChangesAsync();

        var creator = new FictionPlanCreator(db, NullLogger<FictionPlanCreator>.Instance);
        await FluentActions.Invoking(() => creator.CreatePlanAsync(new FictionPlanCreationOptions(
                project.Id,
                null,
                null,
                "Pilot Plan",
                null,
                null,
                Guid.NewGuid(),
                null), CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>();
    }
}
