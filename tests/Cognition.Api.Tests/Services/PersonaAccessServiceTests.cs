using System;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.ErrorHandling;
using Cognition.Api.Services.Personas;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Data.Relational.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cognition.Api.Tests.Services;

public class PersonaAccessServiceTests
{
    private static CognitionDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CognitionDbContext(options);
    }

    [Fact]
    public async Task GrantAccess_AllowsOwner()
    {
        using var db = CreateDb();
        var ownerId = Guid.NewGuid();
        var targetUser = Guid.NewGuid();
        db.Users.Add(new User { Id = ownerId });
        db.Users.Add(new User { Id = targetUser });
        var persona = new Persona { Id = Guid.NewGuid(), Name = "p1", OwnedBy = OwnedBy.User, Type = PersonaType.Assistant };
        db.Personas.Add(persona);
        db.UserPersonas.Add(new UserPersonas { UserId = ownerId, PersonaId = persona.Id, IsOwner = true });
        await db.SaveChangesAsync();

        var svc = new PersonaAccessService(db);
        var linkId = await svc.GrantAccessAsync(persona.Id, targetUser, true, "label", ownerId, isAdmin: false, CancellationToken.None);

        var link = await db.UserPersonas.FirstAsync(x => x.Id == linkId);
        Assert.True(link.IsDefault);
        Assert.Equal(targetUser, link.UserId);
    }

    [Fact]
    public async Task GrantAccess_ForbidsNonOwner()
    {
        using var db = CreateDb();
        var ownerId = Guid.NewGuid();
        var caller = Guid.NewGuid();
        var targetUser = Guid.NewGuid();
        db.Users.Add(new User { Id = ownerId });
        db.Users.Add(new User { Id = targetUser });
        db.Users.Add(new User { Id = caller });
        var persona = new Persona { Id = Guid.NewGuid(), Name = "p1", OwnedBy = OwnedBy.User, Type = PersonaType.Assistant };
        db.Personas.Add(persona);
        db.UserPersonas.Add(new UserPersonas { UserId = ownerId, PersonaId = persona.Id, IsOwner = true });
        await db.SaveChangesAsync();

        var svc = new PersonaAccessService(db);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.GrantAccessAsync(persona.Id, targetUser, false, null, caller, isAdmin: false, CancellationToken.None));
    }
}
