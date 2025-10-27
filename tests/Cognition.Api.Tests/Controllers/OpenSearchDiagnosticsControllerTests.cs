using System;
using System.Collections.Generic;
using Cognition.Api.Controllers;
using Cognition.Api.Infrastructure.OpenSearch;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Cognition.Api.Tests.Controllers;

public class OpenSearchDiagnosticsControllerTests
{
    [Fact]
    public async Task GetAsync_ReturnsDiagnosticsReport()
    {
        var report = new OpenSearchDiagnosticsReport(
            CheckedAtUtc: DateTime.UtcNow,
            Endpoint: "http://localhost:9200",
            DefaultIndex: "vectors-knowledge",
            PipelineId: "vectors-embed",
            ModelId: "model-123",
            ClusterAvailable: true,
            ClusterStatus: "green",
            IndexExists: true,
            PipelineExists: true,
            ModelState: "DEPLOYED",
            ModelDeployState: "COMPLETED",
            Notes: Array.Empty<string>());

        var controller = new OpenSearchDiagnosticsController(
            new StubDiagnosticsService(report),
            new StubOpenSearchBootstrapper());

        var result = await controller.GetAsync(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeSameAs(report);
    }

    [Fact]
    public async Task BootstrapAsync_ReturnsBootstrapResult()
    {
        var expected = new OpenSearchBootstrapResult(
            ModelId: "model-abc",
            ModelCreated: true,
            ModelDeployed: true,
            PipelineCreated: true,
            IndexCreated: true,
            Notes: Array.Empty<string>());

        var controller = new OpenSearchDiagnosticsController(
            new StubDiagnosticsService(null),
            new StubOpenSearchBootstrapper(expected));

        var result = await controller.BootstrapAsync(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeSameAs(expected);
    }

    private sealed class StubDiagnosticsService : IOpenSearchDiagnosticsService
    {
        private readonly OpenSearchDiagnosticsReport? _report;

        public StubDiagnosticsService(OpenSearchDiagnosticsReport? report)
        {
            _report = report;
        }

        public Task<OpenSearchDiagnosticsReport> GetReportAsync(CancellationToken ct)
        {
            if (_report is null)
            {
                throw new InvalidOperationException("No report configured.");
            }

            return Task.FromResult(_report);
        }
    }

    private sealed class StubOpenSearchBootstrapper : IOpenSearchBootstrapper
    {
        private readonly OpenSearchBootstrapResult _result;

        public StubOpenSearchBootstrapper(OpenSearchBootstrapResult? result = null)
        {
            _result = result ?? new OpenSearchBootstrapResult(
                ModelId: "model",
                ModelCreated: false,
                ModelDeployed: false,
                PipelineCreated: false,
                IndexCreated: false,
                Notes: Array.Empty<string>());
        }

        public Task<OpenSearchBootstrapResult> BootstrapAsync(CancellationToken ct)
            => Task.FromResult(_result);
    }
}
