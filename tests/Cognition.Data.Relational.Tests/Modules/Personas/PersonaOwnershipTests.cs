using System;
using System.Threading.Tasks;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Personas;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cognition.Data.Relational.Tests.Modules.Personas;

public class PersonaOwnershipTests
{
    private static CognitionDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new CognitionDbContext(options);
    }

    [Fact]
    public async Task PersonaPersonas_Allows_user_and_agent_to_own_child_personas()
    {
        await using var db = CreateDb();

        var userPersona = new Persona { Id = Guid.NewGuid(), Name = "User", OwnedBy = OwnedBy.User, Type = PersonaType.User };
        var agentPersona = new Persona { Id = Guid.NewGuid(), Name = "Agent Persona", OwnedBy = OwnedBy.Persona, Type = PersonaType.Agent };
        var characterPersona = new Persona { Id = Guid.NewGuid(), Name = "Character", OwnedBy = OwnedBy.Persona, Type = PersonaType.RolePlayCharacter };

        db.Personas.AddRange(userPersona, agentPersona, characterPersona);
        await db.SaveChangesAsync();

        db.PersonaPersonas.AddRange(
            new PersonaPersonas { FromPersonaId = userPersona.Id, ToPersonaId = characterPersona.Id, IsOwner = true, Label = "user-owner" },
            new PersonaPersonas { FromPersonaId = agentPersona.Id, ToPersonaId = characterPersona.Id, IsOwner = true, Label = "agent-owner" });

        await db.SaveChangesAsync();

        var links = await db.PersonaPersonas.AsNoTracking().ToListAsync();
        Assert.Collection(links,
            link =>
            {
                Assert.Equal(userPersona.Id, link.FromPersonaId);
                Assert.Equal(characterPersona.Id, link.ToPersonaId);
                Assert.True(link.IsOwner);
            },
            link =>
            {
                Assert.Equal(agentPersona.Id, link.FromPersonaId);
                Assert.Equal(characterPersona.Id, link.ToPersonaId);
                Assert.True(link.IsOwner);
            });
    }
}
