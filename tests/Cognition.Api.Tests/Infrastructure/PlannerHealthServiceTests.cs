using System;
using System.Collections.Generic;
using System.Threading;
using System.Text.Json;
using Cognition.Api.Infrastructure.Planning;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Planning;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Planning;
using Cognition.Data.Relational.Modules.Prompts;
using Cognition.Data.Relational.Modules.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cognition.Api.Tests.Infrastructure;

public class PlannerHealthServiceTests
{
    [Fact]
    public async Task GetReportAsync_flags_missing_templates_as_critical()
    {
        await using var db = CreateDbContext();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<TestPlannerTool>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var registry = new StubToolRegistry(typeof(TestPlannerTool));
        var alertPublisher = new TestAlertPublisher();
        var health = new PlannerHealthService(db, registry, scope.ServiceProvider, alertPublisher, NullLogger<PlannerHealthService>.Instance);

        var report = await health.GetReportAsync(CancellationToken.None);

        report.Status.Should().Be(PlannerHealthStatus.Critical);
        report.Planners.Should().ContainSingle();
        var planner = report.Planners.Single();
        planner.Steps.Should().ContainSingle();
        var step = planner.Steps.Single();
        step.TemplateFound.Should().BeFalse();
        step.TemplateActive.Should().BeFalse();
        step.Issue.Should().Be("missing");
        report.Warnings.Should().Contain(w => w.Contains("not found", StringComparison.OrdinalIgnoreCase));
        report.Backlog.Plans.Should().BeEmpty();
        report.WorldBible.Plans.Should().BeEmpty();
        report.Alerts.Should().Contain(a => a.Id.StartsWith("template-missing", StringComparison.OrdinalIgnoreCase));
        alertPublisher.Published.Should().HaveCount(1);
        alertPublisher.Published.Single().Should().Contain(a => a.Id.StartsWith("template-missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetReportAsync_marks_stale_backlog_and_recent_failures_as_degraded()
    {
        await using var db = CreateDbContext();

        // Active template so we don't trip the critical path.
        db.PromptTemplates.Add(new PromptTemplate
        {
            Id = Guid.NewGuid(),
            Name = TestPlannerTool.TemplateId,
            PromptType = PromptType.SystemInstruction,
            Template = "You are a planner.",
            IsActive = true
        });

        var planId = Guid.NewGuid();
        db.FictionPlans.Add(new FictionPlan
        {
            Id = planId,
            FictionProjectId = Guid.NewGuid(),
            Name = "Voyager Plan",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-6)
        });

        db.FictionPlanBacklogItems.Add(new FictionPlanBacklogItem
        {
            Id = Guid.NewGuid(),
            FictionPlanId = planId,
            BacklogId = "outline-core-conflicts",
            Description = "Outline the core conflicts.",
            Status = FictionPlanBacklogStatus.InProgress,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-5),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-3),
            InProgressAtUtc = DateTime.UtcNow.AddHours(-3)
        });

        db.PlannerExecutions.Add(new PlannerExecution
        {
            Id = Guid.NewGuid(),
            PlannerName = TestPlannerTool.PlannerName,
            Outcome = nameof(PlannerOutcome.Success),
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            Diagnostics = new Dictionary<string, string>
            {
                ["critiqueStatus"] = "used"
            }
        });

        var failureConversationId = Guid.NewGuid();
        var transcriptMessageId = Guid.NewGuid();
        db.PlannerExecutions.Add(new PlannerExecution
        {
            Id = Guid.NewGuid(),
            PlannerName = TestPlannerTool.PlannerName,
            Outcome = nameof(PlannerOutcome.Failed),
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ConversationId = failureConversationId,
            Diagnostics = new Dictionary<string, string>
            {
                ["error"] = "LLM refused",
                ["critiqueStatus"] = "count-exhausted"
            },
            Transcript = new List<PlannerExecutionTranscriptEntry>
            {
                new()
                {
                    TimestampUtc = DateTime.UtcNow.AddMinutes(-9),
                    Role = "assistant",
                    Message = "Planner failed to satisfy guard rails.",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["conversationMessageId"] = transcriptMessageId
                    }
                }
            }
        });

        await db.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<TestPlannerTool>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var registry = new StubToolRegistry(typeof(TestPlannerTool));
        var alertPublisher = new TestAlertPublisher();
        var health = new PlannerHealthService(db, registry, scope.ServiceProvider, alertPublisher, NullLogger<PlannerHealthService>.Instance);

        var report = await health.GetReportAsync(CancellationToken.None);

        report.Status.Should().Be(PlannerHealthStatus.Degraded);
        report.Planners.Single().Steps.Single().TemplateActive.Should().BeTrue();
        report.Backlog.StaleItems.Should().ContainSingle();
        report.Backlog.RecentTransitions.Should().Contain(t => t.BacklogId == "outline-core-conflicts" && t.Status == FictionPlanBacklogStatus.InProgress);
        var failure = report.Telemetry.RecentFailures.Should().ContainSingle(f => f.Outcome == nameof(PlannerOutcome.Failed)).Subject;
        failure.ConversationId.Should().Be(failureConversationId);
        report.Alerts.Should().Contain(a => a.Id == "backlog:stale");
        report.Alerts.Should().Contain(a => a.Id == "planner:recent-failures");
        alertPublisher.Published.Should().HaveCount(1);
        alertPublisher.Published.Single().Should().Contain(a => a.Id == "planner:recent-failures");
        failure.ConversationMessageId.Should().Be(transcriptMessageId);
        failure.TranscriptRole.Should().Be("assistant");
        failure.TranscriptSnippet.Should().Contain("guard rails");
        report.Backlog.Plans.Should().ContainSingle();
        var planSummary = report.Backlog.Plans.Single();
        planSummary.PlanName.Should().Be("Voyager Plan");
        planSummary.InProgress.Should().Be(1);
        planSummary.Pending.Should().Be(0);
        planSummary.Complete.Should().Be(0);
        planSummary.LastUpdatedUtc.Should().NotBeNull();
        report.Telemetry.CritiqueStatusCounts.Should().ContainKey("count-exhausted").WhoseValue.Should().Be(1);
        report.Warnings.Should().Contain(w => w.Contains("in progress", StringComparison.OrdinalIgnoreCase));
        report.Warnings.Should().Contain(w => w.Contains("failed", StringComparison.OrdinalIgnoreCase));
        report.Warnings.Should().Contain(w => w.Contains("critique", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetReportAsync_flags_world_bible_missing_entries()
    {
        await using var db = CreateDbContext();

        db.PromptTemplates.Add(new PromptTemplate
        {
            Id = Guid.NewGuid(),
            Name = TestPlannerTool.TemplateId,
            PromptType = PromptType.SystemInstruction,
            Template = "template",
            IsActive = true
        });

        var planId = Guid.NewGuid();
        db.FictionPlans.Add(new FictionPlan
        {
            Id = planId,
            FictionProjectId = Guid.NewGuid(),
            Name = "Loreless Plan",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-3)
        });

        db.FictionWorldBibles.Add(new FictionWorldBible
        {
            Id = Guid.NewGuid(),
            FictionPlanId = planId,
            Domain = "core",
            BranchSlug = "main",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
            UpdatedAtUtc = DateTime.UtcNow.AddHours(-3)
        });

        await db.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<TestPlannerTool>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var registry = new StubToolRegistry(typeof(TestPlannerTool));
        var alertPublisher = new TestAlertPublisher();
        var health = new PlannerHealthService(db, registry, scope.ServiceProvider, alertPublisher, NullLogger<PlannerHealthService>.Instance);

        var report = await health.GetReportAsync(CancellationToken.None);

        report.Status.Should().Be(PlannerHealthStatus.Degraded);
        report.Alerts.Should().Contain(a => a.Id.StartsWith("worldbible:missing", StringComparison.OrdinalIgnoreCase));
        report.Warnings.Should().Contain(w => w.Contains("world-bible", StringComparison.OrdinalIgnoreCase));
        alertPublisher.Published.Should().HaveCount(1);
        alertPublisher.Published.Single().Should().Contain(a => a.Id.StartsWith("worldbible:missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetReportAsync_flags_world_bible_stale_entries()
    {
        await using var db = CreateDbContext();

        db.PromptTemplates.Add(new PromptTemplate
        {
            Id = Guid.NewGuid(),
            Name = TestPlannerTool.TemplateId,
            PromptType = PromptType.SystemInstruction,
            Template = "template",
            IsActive = true
        });

        var planId = Guid.NewGuid();
        var worldBibleId = Guid.NewGuid();
        var staleTimestamp = DateTime.UtcNow.AddHours(-8);

        db.FictionPlans.Add(new FictionPlan
        {
            Id = planId,
            FictionProjectId = Guid.NewGuid(),
            Name = "Stale Lore Plan",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-10),
            UpdatedAtUtc = staleTimestamp
        });

        db.FictionWorldBibles.Add(new FictionWorldBible
        {
            Id = worldBibleId,
            FictionPlanId = planId,
            Domain = "core",
            BranchSlug = "main",
            CreatedAtUtc = staleTimestamp,
            UpdatedAtUtc = staleTimestamp
        });

        db.FictionWorldBibleEntries.Add(new FictionWorldBibleEntry
        {
            Id = Guid.NewGuid(),
            FictionWorldBibleId = worldBibleId,
            EntrySlug = "hero",
            EntryName = "Primary Hero",
            Content = new FictionWorldBibleEntryContent
            {
                Category = "characters",
                Summary = "Hero summary",
                Status = "Active",
                ContinuityNotes = new[] { "Keep arc consistent." },
                UpdatedAtUtc = staleTimestamp
            },
            Version = 1,
            ChangeType = FictionWorldBibleChangeType.Seed,
            Sequence = 1,
            IsActive = true,
            CreatedAtUtc = staleTimestamp,
            UpdatedAtUtc = staleTimestamp
        });

        await db.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<TestPlannerTool>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var registry = new StubToolRegistry(typeof(TestPlannerTool));
        var alertPublisher = new TestAlertPublisher();
        var health = new PlannerHealthService(db, registry, scope.ServiceProvider, alertPublisher, NullLogger<PlannerHealthService>.Instance);

        var report = await health.GetReportAsync(CancellationToken.None);

        report.Status.Should().Be(PlannerHealthStatus.Degraded);
        report.Alerts.Should().Contain(a => a.Id.StartsWith("worldbible:stale", StringComparison.OrdinalIgnoreCase));
        report.Warnings.Should().Contain(w => w.Contains("stale", StringComparison.OrdinalIgnoreCase));
        alertPublisher.Published.Should().HaveCount(1);
        alertPublisher.Published.Single().Should().Contain(a => a.Id.StartsWith("worldbible:stale", StringComparison.OrdinalIgnoreCase));
    }

    private static CognitionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new PlannerHealthTestDbContext(options);
    }

    private sealed class TestAlertPublisher : IPlannerAlertPublisher
    {
        public List<IReadOnlyList<PlannerHealthAlert>> Published { get; } = new();

        public Task PublishAsync(IReadOnlyList<PlannerHealthAlert> alerts, CancellationToken ct)
        {
            Published.Add(alerts);
            return Task.CompletedTask;
        }
    }

    private sealed class StubToolRegistry : IToolRegistry
    {
        private readonly Dictionary<string, Type> _map;
        private readonly Type _plannerType;

        public StubToolRegistry(Type plannerType)
        {
            _plannerType = plannerType ?? throw new ArgumentNullException(nameof(plannerType));
            var asmName = plannerType.Assembly.GetName().Name;
            var fullName = plannerType.FullName ?? plannerType.Name;
            var aqn = $"{fullName}, {asmName}";

            _map = new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [fullName] = plannerType,
                [aqn] = plannerType
            };
        }

        public IReadOnlyDictionary<string, Type> Map => _map;

        public bool TryResolveByClassPath(string classPath, out Type type) => _map.TryGetValue(classPath, out type!);

        public bool IsKnownClassPath(string classPath) => _map.ContainsKey(classPath);

        public IReadOnlyCollection<Type> GetPlannersByCapability(string capability)
        {
            return new[] { _plannerType };
        }
    }

    private sealed class TestPlannerTool : IPlannerTool
    {
        public const string TemplateId = "planner.test.step";
        public const string PlannerName = "Test Planner";

        private static readonly PlannerMetadata MetadataDefinition = PlannerMetadata.Create(
            name: PlannerName,
            description: "Test-only planner.",
            capabilities: new[] { "test" },
            steps: new[]
            {
                new PlannerStepDescriptor("step-1", "Test Step", TemplateId: TemplateId)
            });

        public string Name => PlannerName;

        public string ClassPath => $"{GetType().FullName}, {GetType().Assembly.GetName().Name}";

        public PlannerMetadata Metadata => MetadataDefinition;

        public Task<PlannerResult> PlanAsync(PlannerContext context, PlannerParameters parameters, CancellationToken ct = default)
            => Task.FromResult(PlannerResult.Success());

        public Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
            => Task.FromResult<object?>(null);
    }

    private sealed class PlannerHealthTestDbContext : CognitionDbContext
    {
        public PlannerHealthTestDbContext(DbContextOptions<CognitionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var allowed = new HashSet<Type>
            {
                typeof(PromptTemplate),
                typeof(FictionPlan),
                typeof(FictionPlanBacklogItem),
                typeof(PlannerExecution),
                typeof(FictionWorldBible),
                typeof(FictionWorldBibleEntry)
            };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
            {
                if (entityType.ClrType is not null && !allowed.Contains(entityType.ClrType))
                {
                    modelBuilder.Ignore(entityType.ClrType);
                }
            }

            modelBuilder.Entity<FictionPlanBacklogItem>().Ignore(x => x.Inputs);
            modelBuilder.Entity<FictionPlanBacklogItem>().Ignore(x => x.Outputs);

            modelBuilder.Entity<FictionPlan>().Ignore(x => x.Passes);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.ChapterBlueprints);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.Checkpoints);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.Backlog);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.Transcripts);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.StoryMetrics);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.WorldBibles);
            modelBuilder.Entity<FictionPlan>().Ignore(x => x.FictionProject);

            modelBuilder.Entity<PlannerExecution>(b =>
            {
                b.Property(x => x.ConversationState)
                    .HasConversion(
                        v => Serialize(v),
                        v => Deserialize<Dictionary<string, object?>>(v));
                b.Property(x => x.Artifacts)
                    .HasConversion(
                        v => Serialize(v),
                        v => Deserialize<Dictionary<string, object?>>(v));
                b.Property(x => x.Metrics)
                    .HasConversion(
                        v => Serialize(v),
                        v => Deserialize<Dictionary<string, double>>(v));
                b.Property(x => x.Diagnostics)
                    .HasConversion(
                        v => Serialize(v),
                        v => Deserialize<Dictionary<string, string>>(v));
                b.Property(x => x.Transcript)
                    .HasConversion(
                        v => Serialize(v),
                        v => Deserialize<List<PlannerExecutionTranscriptEntry>>(v));
            });

            modelBuilder.Entity<PromptTemplate>().Ignore(x => x.Tokens);
            modelBuilder.Entity<PromptTemplate>().Ignore(x => x.Example);
        }

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = false
        };

        private static string? Serialize<T>(T value)
        {
            if (value is null)
            {
                return null;
            }

            return JsonSerializer.Serialize(value, SerializerOptions);
        }

        private static T? Deserialize<T>(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value, SerializerOptions);
        }
    }
}
