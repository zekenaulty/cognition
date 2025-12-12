using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Controllers;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Planning;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Tools;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cognition.Api.Tests.Controllers;

public class ToolExecutionControllerTests
{
    [Fact]
    public async Task Execute_AppendsPlanIdToArgsAndMetadata()
    {
        var dispatcher = new RecordingDispatcher();
        var services = new ServiceCollection().BuildServiceProvider();
        var db = new CognitionDbContext(new DbContextOptionsBuilder<CognitionDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var controller = new ToolExecutionController(dispatcher, services, db);
        var toolId = Guid.NewGuid();
        db.Tools.Add(new Tool { Id = toolId, Name = "t1", IsActive = true, ClassPath = "x" });
        await db.SaveChangesAsync();
        var planId = Guid.NewGuid();
        var request = new ToolExecutionController.ExecRequest
        {
            Args = new Dictionary<string, object?>(),
            AgentId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            PersonaId = Guid.NewGuid(),
            FictionPlanId = planId
        };

        await controller.Execute(toolId, request, CancellationToken.None);

        dispatcher.CapturedArgs.Should().NotBeNull();
        dispatcher.CapturedArgs!.Should().ContainKey("planId").WhoseValue.Should().Be(planId);
        dispatcher.CapturedContext.Should().NotBeNull();
        dispatcher.CapturedContext!.Metadata.Should().NotBeNull();
        dispatcher.CapturedContext.Metadata!.Should().ContainKey("planId").WhoseValue.Should().Be(planId);
    }

    [Fact]
    public async Task Execute_DoesNotOverwriteExistingPlanIdArg()
    {
        var dispatcher = new RecordingDispatcher();
        var services = new ServiceCollection().BuildServiceProvider();
        var db = new CognitionDbContext(new DbContextOptionsBuilder<CognitionDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var controller = new ToolExecutionController(dispatcher, services, db);
        var toolId = Guid.NewGuid();
        db.Tools.Add(new Tool { Id = toolId, Name = "t1", IsActive = true, ClassPath = "x" });
        await db.SaveChangesAsync();
        var existingPlan = Guid.NewGuid();
        var args = new Dictionary<string, object?> { ["planId"] = existingPlan };
        var request = new ToolExecutionController.ExecRequest
        {
            Args = args,
            AgentId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            PersonaId = Guid.NewGuid(),
            FictionPlanId = Guid.NewGuid()
        };

        await controller.Execute(toolId, request, CancellationToken.None);

        dispatcher.CapturedArgs.Should().NotBeNull();
        dispatcher.CapturedArgs!["planId"].Should().Be(existingPlan);
    }

    private sealed class RecordingDispatcher : IToolDispatcher
    {
        public ToolContext? CapturedContext { get; private set; }
        public IDictionary<string, object?>? CapturedArgs { get; private set; }

        public Task<(bool ok, object? result, string? error)> ExecuteAsync(Guid toolId, ToolContext ctx, IDictionary<string, object?> args, bool log = true)
        {
            CapturedContext = ctx;
            CapturedArgs = new Dictionary<string, object?>(args, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult<(bool, object?, string?)>((true, new { ok = true }, null));
        }

        public Task<(bool ok, PlannerResult? result, string? error)> ExecutePlannerAsync(Guid toolId, PlannerContext ctx, PlannerParameters parameters, bool log = true)
            => throw new NotImplementedException();
    }
}
