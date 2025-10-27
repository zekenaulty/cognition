using System;
using System.Collections.Generic;
using Cognition.Api.Controllers;
using Cognition.Api.Infrastructure.Planning;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Cognition.Api.Tests.Controllers;

public class PlannerDiagnosticsControllerTests
{
    [Fact]
    public async Task GetAsync_ReturnsPlannerHealthReport()
    {
        var report = CreateSampleReport();
        var controller = new PlannerDiagnosticsController(new StubPlannerHealthService(report));

        var result = await controller.GetAsync(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeSameAs(report);
    }

    private static PlannerHealthReport CreateSampleReport()
    {
        return new PlannerHealthReport(
            GeneratedAtUtc: DateTime.UtcNow,
            Status: PlannerHealthStatus.Healthy,
            Planners: Array.Empty<PlannerHealthPlanner>(),
            Backlog: new PlannerHealthBacklog(
                TotalItems: 0,
                Pending: 0,
                InProgress: 0,
                Complete: 0,
                Plans: Array.Empty<PlannerHealthBacklogPlanSummary>(),
                RecentTransitions: Array.Empty<PlannerHealthBacklogTransition>(),
                StaleItems: Array.Empty<PlannerHealthBacklogItem>(),
                OrphanedItems: Array.Empty<PlannerHealthBacklogItem>()),
            Telemetry: new PlannerHealthTelemetry(
                TotalExecutions: 0,
                LastExecutionUtc: null,
                OutcomeCounts: new Dictionary<string, int>(),
                CritiqueStatusCounts: new Dictionary<string, int>(),
                RecentFailures: Array.Empty<PlannerHealthExecutionFailure>()),
            Alerts: Array.Empty<PlannerHealthAlert>(),
            Warnings: Array.Empty<string>());
    }

    private sealed class StubPlannerHealthService : IPlannerHealthService
    {
        private readonly PlannerHealthReport _report;

        public StubPlannerHealthService(PlannerHealthReport report)
        {
            _report = report;
        }

        public Task<PlannerHealthReport> GetReportAsync(CancellationToken ct = default)
            => Task.FromResult(_report);
    }
}
