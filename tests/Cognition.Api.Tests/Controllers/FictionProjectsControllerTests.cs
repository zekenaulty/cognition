using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Controllers;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cognition.Api.Tests.Controllers;

public class FictionProjectsControllerTests
{
    [Fact]
    public async Task GetProjects_ReturnsSummaries()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var projectA = new FictionProject { Id = Guid.NewGuid(), Title = "Atlas" };
        var projectB = new FictionProject { Id = Guid.NewGuid(), Title = "Borealis" };
        db.FictionProjects.AddRange(projectA, projectB);
        db.FictionPlans.Add(new FictionPlan { Id = Guid.NewGuid(), FictionProjectId = projectA.Id, FictionProject = projectA, Name = "Plan 1" });
        db.FictionPlans.Add(new FictionPlan { Id = Guid.NewGuid(), FictionProjectId = projectB.Id, FictionProject = projectB, Name = "Plan 2", Status = FictionPlanStatus.Archived });
        await db.SaveChangesAsync();

        var controller = new FictionProjectsController(db);
        var response = await controller.GetProjects(CancellationToken.None);

        var ok = response.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var payload = ok!.Value as IReadOnlyList<FictionProjectsController.FictionProjectSummary>;
        payload.Should().NotBeNull();
        var summaries = payload!;
        summaries.Should().HaveCount(2);
        summaries.Single(p => p.Id == projectA.Id).PlanCount.Should().Be(1);
        summaries.Single(p => p.Id == projectA.Id).ActivePlanCount.Should().Be(1);
        summaries.Single(p => p.Id == projectB.Id).ActivePlanCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateProject_PersistsProject()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var controller = new FictionProjectsController(db);
        var request = new FictionProjectsController.CreateFictionProjectRequest("New Project", "Logline");

        var response = await controller.CreateProject(request, CancellationToken.None);

        var created = response.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        (await db.FictionProjects.CountAsync()).Should().Be(1);
        var project = await db.FictionProjects.SingleAsync();
        project.Title.Should().Be("New Project");
        project.Logline.Should().Be("Logline");
    }

    [Fact]
    public async Task CreateProject_RequiresTitle()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new FictionPlansTestDbContext(options);
        var controller = new FictionProjectsController(db);
        var request = new FictionProjectsController.CreateFictionProjectRequest("   ", null);

        var response = await controller.CreateProject(request, CancellationToken.None);

        response.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
