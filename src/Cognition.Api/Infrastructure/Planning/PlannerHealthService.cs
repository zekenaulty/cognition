using System;
using System.Collections.Concurrent;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Planning;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Planning;
using Cognition.Data.Relational.Modules.Prompts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Cognition.Api.Infrastructure.Planning;

public interface IPlannerHealthService
{
    Task<PlannerHealthReport> GetReportAsync(CancellationToken ct = default);
}

public enum PlannerHealthStatus
{
    Healthy,
    Degraded,
    Critical
}

public sealed record PlannerHealthReport(
    DateTime GeneratedAtUtc,
    PlannerHealthStatus Status,
    IReadOnlyList<PlannerHealthPlanner> Planners,
    PlannerHealthBacklog Backlog,
    PlannerHealthWorldBibleReport WorldBible,
    PlannerHealthTelemetry Telemetry,
    IReadOnlyList<PlannerHealthAlert> Alerts,
    IReadOnlyList<string> Warnings);

public sealed record PlannerHealthWorldBibleReport(
    IReadOnlyList<PlannerHealthWorldBiblePlan> Plans);

public sealed record PlannerHealthWorldBiblePlan(
    Guid PlanId,
    string PlanName,
    Guid WorldBibleId,
    string Domain,
    string? BranchSlug,
    DateTime? LastUpdatedUtc,
    IReadOnlyList<PlannerHealthWorldBibleEntry> ActiveEntries);

public sealed record PlannerHealthWorldBibleEntry(
    string Category,
    string EntrySlug,
    string EntryName,
    string Summary,
    string Status,
    IReadOnlyList<string> ContinuityNotes,
    int Version,
    bool IsActive,
    int? IterationIndex,
    string? BacklogItemId,
    DateTime UpdatedAtUtc);

public sealed record PlannerHealthPlanner(
    string Name,
    string Description,
    IReadOnlyList<string> Capabilities,
    List<PlannerHealthStepTemplate> Steps);

public sealed record PlannerHealthStepTemplate(
    string StepId,
    string DisplayName,
    string? TemplateId,
    bool TemplateFound,
    bool TemplateActive,
    string? Issue);

public sealed record PlannerHealthBacklog(
    int TotalItems,
    int Pending,
    int InProgress,
    int Complete,
    IReadOnlyList<PlannerHealthBacklogPlanSummary> Plans,
    IReadOnlyList<PlannerHealthBacklogTransition> RecentTransitions,
    IReadOnlyList<PlannerHealthBacklogItem> StaleItems,
    IReadOnlyList<PlannerHealthBacklogItem> OrphanedItems);

public sealed record PlannerHealthBacklogPlanSummary(
    Guid PlanId,
    string PlanName,
    int Pending,
    int InProgress,
    int Complete,
    DateTime? LastUpdatedUtc,
    DateTime? LastCompletedUtc);

public sealed record PlannerHealthBacklogItem(
    Guid PlanId,
    string PlanName,
    string BacklogId,
    string Description,
    FictionPlanBacklogStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    TimeSpan? StaleDuration);

public sealed record PlannerHealthBacklogTransition(
    Guid PlanId,
    string PlanName,
    string BacklogId,
    string Description,
    FictionPlanBacklogStatus Status,
    DateTime OccurredAtUtc,
    TimeSpan Age);

public sealed record PlannerHealthTelemetry(
    int TotalExecutions,
    DateTime? LastExecutionUtc,
    IReadOnlyDictionary<string, int> OutcomeCounts,
    IReadOnlyDictionary<string, int> CritiqueStatusCounts,
    IReadOnlyList<PlannerHealthExecutionFailure> RecentFailures);

public enum PlannerHealthAlertSeverity
{
    Info,
    Warning,
    Error
}

public sealed record PlannerHealthAlert(
    string Id,
    PlannerHealthAlertSeverity Severity,
    string Title,
    string Description,
    DateTime GeneratedAtUtc);

public sealed record PlannerHealthExecutionFailure(
    Guid ExecutionId,
    string PlannerName,
    string Outcome,
    DateTime CreatedAtUtc,
    IReadOnlyDictionary<string, string>? Diagnostics,
    Guid? ConversationId,
    Guid? ConversationMessageId,
    string? TranscriptRole,
    string? TranscriptSnippet);

internal sealed record PlannerTemplateState(bool Exists, bool Active, bool HasContent);

public sealed class PlannerHealthService : IPlannerHealthService
{
    private static readonly TimeSpan StaleBacklogThreshold = TimeSpan.FromHours(2);
    private static readonly TimeSpan RecentFailureWindow = TimeSpan.FromHours(24);

    private readonly CognitionDbContext _db;
    private readonly IToolRegistry _toolRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPlannerAlertPublisher _alertPublisher;
    private readonly ILogger<PlannerHealthService> _logger;

    public PlannerHealthService(
        CognitionDbContext db,
        IToolRegistry toolRegistry,
        IServiceProvider serviceProvider,
        IPlannerAlertPublisher alertPublisher,
        ILogger<PlannerHealthService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _alertPublisher = alertPublisher ?? throw new ArgumentNullException(nameof(alertPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PlannerHealthReport> GetReportAsync(CancellationToken ct = default)
    {
        var generatedAt = DateTime.UtcNow;
        var warnings = new List<string>();

        var plannerSnapshot = LoadPlannerMetadata();
        var templateStates = await LoadTemplateStatesAsync(plannerSnapshot.TemplateIds, ct).ConfigureAwait(false);
        var plannerReports = ApplyTemplateStates(plannerSnapshot.Planners, templateStates, warnings);

        var backlog = await BuildBacklogReportAsync(ct).ConfigureAwait(false);
        if (backlog.StaleItems.Count > 0)
        {
            warnings.Add($"{backlog.StaleItems.Count} backlog item(s) have been in progress longer than {StaleBacklogThreshold.TotalHours:n0}h.");
        }
        if (backlog.OrphanedItems.Count > 0)
        {
            warnings.Add($"{backlog.OrphanedItems.Count} backlog item(s) reference missing plans.");
        }

        var worldBible = await BuildWorldBibleReportAsync(ct).ConfigureAwait(false);

        var telemetry = await BuildTelemetryReportAsync(ct).ConfigureAwait(false);
        if (telemetry.TotalExecutions == 0)
        {
            warnings.Add("No planner executions have been recorded yet.");
        }
        if (telemetry.RecentFailures.Count > 0)
        {
            warnings.Add($"{telemetry.RecentFailures.Count} planner execution(s) failed in the last {RecentFailureWindow.TotalHours:n0}h.");
        }
        if (telemetry.CritiqueStatusCounts.TryGetValue("count-exhausted", out var countExhausted) && countExhausted > 0)
        {
            warnings.Add($"{countExhausted} planner execution(s) exhausted critique count budgets.");
        }
        if (telemetry.CritiqueStatusCounts.TryGetValue("token-exhausted", out var tokenExhausted) && tokenExhausted > 0)
        {
            warnings.Add($"{tokenExhausted} planner execution(s) exceeded critique token budgets.");
        }

        var alerts = BuildAlerts(plannerReports, backlog, telemetry, generatedAt);
        var status = DetermineStatus(plannerReports, backlog, telemetry);

        var report = new PlannerHealthReport(
            GeneratedAtUtc: generatedAt,
            Status: status,
            Planners: plannerReports,
            Backlog: backlog,
            WorldBible: worldBible,
            Telemetry: telemetry,
            Alerts: alerts,
            Warnings: warnings);

        await _alertPublisher.PublishAsync(alerts, ct).ConfigureAwait(false);
        return report;
    }

    private PlannerSnapshot LoadPlannerMetadata()
    {
        var planners = new List<PlannerHealthPlanner>();
        var templateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<Type>();
        foreach (var type in _toolRegistry.Map.Values.Where(t => typeof(IPlannerTool).IsAssignableFrom(t)))
        {
            if (!seen.Add(type))
            {
                continue;
            }

            if (_serviceProvider.GetService(type) is not IPlannerTool planner)
            {
                _logger.LogWarning("Planner type {PlannerType} is registered but could not be resolved from the service provider.", type.FullName);
                continue;
            }

            var metadata = planner.Metadata;
            var steps = new List<PlannerHealthStepTemplate>(metadata.Steps.Count);
            foreach (var step in metadata.Steps)
            {
                if (!string.IsNullOrWhiteSpace(step.TemplateId))
                {
                    templateIds.Add(step.TemplateId);
                    steps.Add(new PlannerHealthStepTemplate(step.Id, step.DisplayName, step.TemplateId, false, false, "unchecked"));
                }
                else
                {
                    steps.Add(new PlannerHealthStepTemplate(step.Id, step.DisplayName, null, true, true, null));
                }
            }

            planners.Add(new PlannerHealthPlanner(metadata.Name, metadata.Description, metadata.Capabilities, steps));
        }

        return new PlannerSnapshot(planners, templateIds);
    }

    private async Task<Dictionary<string, PlannerTemplateState>> LoadTemplateStatesAsync(HashSet<string> templateIds, CancellationToken ct)
    {
        if (templateIds.Count == 0)
        {
            return new Dictionary<string, PlannerTemplateState>(StringComparer.OrdinalIgnoreCase);
        }

        var templates = await _db.Set<PromptTemplate>()
            .AsNoTracking()
            .Where(t => templateIds.Contains(t.Name))
            .Select(t => new
            {
                t.Name,
                t.IsActive,
                t.Template
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var lookup = new Dictionary<string, PlannerTemplateState>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in templates.GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
        {
            var hasActive = group.Any(t => t.IsActive && !string.IsNullOrWhiteSpace(t.Template));
            var hasContent = group.Any(t => !string.IsNullOrWhiteSpace(t.Template));
            lookup[group.Key] = new PlannerTemplateState(Exists: true, Active: hasActive, HasContent: hasContent);
        }

        return lookup;
    }

    private List<PlannerHealthPlanner> ApplyTemplateStates(
        IReadOnlyList<PlannerHealthPlanner> planners,
        IReadOnlyDictionary<string, PlannerTemplateState> templateStates,
        List<string> warnings)
    {
        var updated = new List<PlannerHealthPlanner>(planners.Count);
        foreach (var planner in planners)
        {
            var steps = new List<PlannerHealthStepTemplate>(planner.Steps.Count);
            foreach (var step in planner.Steps)
            {
                if (string.IsNullOrWhiteSpace(step.TemplateId))
                {
                    steps.Add(step with { TemplateFound = true, TemplateActive = true, Issue = null });
                    continue;
                }

                if (!templateStates.TryGetValue(step.TemplateId, out var state))
                {
                    warnings.Add($"Planner '{planner.Name}' step '{step.StepId}' requires template '{step.TemplateId}', but it was not found.");
                    steps.Add(step with { TemplateFound = false, TemplateActive = false, Issue = "missing" });
                }
                else if (!state.Active)
                {
                    var issue = state.HasContent ? "inactive" : "empty";
                    warnings.Add($"Planner '{planner.Name}' step '{step.StepId}' template '{step.TemplateId}' is {issue}.");
                    steps.Add(step with { TemplateFound = true, TemplateActive = false, Issue = issue });
                }
                else
                {
                    steps.Add(step with { TemplateFound = true, TemplateActive = true, Issue = null });
                }
            }

            updated.Add(planner with { Steps = steps });
        }

        return updated;
    }

    private async Task<PlannerHealthBacklog> BuildBacklogReportAsync(CancellationToken ct)
    {
        var backlogItems = await _db.FictionPlanBacklogItems
            .AsNoTracking()
            .Include(x => x.FictionPlan)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var total = backlogItems.Count;
        var pending = backlogItems.Count(x => x.Status == FictionPlanBacklogStatus.Pending);
        var inProgress = backlogItems.Count(x => x.Status == FictionPlanBacklogStatus.InProgress);
        var complete = backlogItems.Count(x => x.Status == FictionPlanBacklogStatus.Complete);

        var now = DateTime.UtcNow;
        var transitionCutoff = now - TimeSpan.FromHours(24);
        var staleItems = new List<PlannerHealthBacklogItem>();
        var orphaned = new List<PlannerHealthBacklogItem>();
        var recentTransitions = new List<PlannerHealthBacklogTransition>();
        var planSummaries = new Dictionary<Guid, PlanSummaryAccumulator>();

        foreach (var item in backlogItems)
        {
            var planName = item.FictionPlan?.Name ?? "(missing plan)";
            var planSummary = GetPlanSummaryAccumulator(planSummaries, item.FictionPlanId, planName);

            switch (item.Status)
            {
                case FictionPlanBacklogStatus.Pending:
                    planSummary.Pending++;
                    break;
                case FictionPlanBacklogStatus.InProgress:
                    planSummary.InProgress++;
                    break;
                case FictionPlanBacklogStatus.Complete:
                    planSummary.Complete++;
                    break;
            }

            if (item.UpdatedAtUtc is DateTime updatedUtc &&
                (!planSummary.LastUpdatedUtc.HasValue || updatedUtc > planSummary.LastUpdatedUtc))
            {
                planSummary.LastUpdatedUtc = updatedUtc;
            }

            if (item.CompletedAtUtc is DateTime completedUtc &&
                (!planSummary.LastCompletedUtc.HasValue || completedUtc > planSummary.LastCompletedUtc))
            {
                planSummary.LastCompletedUtc = completedUtc;
            }

            if (item.FictionPlan is null)
            {
                orphaned.Add(new PlannerHealthBacklogItem(
                    item.FictionPlanId,
                    planName,
                    item.BacklogId,
                    item.Description,
                    item.Status,
                    item.CreatedAtUtc,
                    item.UpdatedAtUtc,
                    null));
            }

            if (item.Status == FictionPlanBacklogStatus.InProgress && item.UpdatedAtUtc is DateTime updated)
            {
                var age = now - updated;
                if (age > StaleBacklogThreshold)
                {
                    staleItems.Add(new PlannerHealthBacklogItem(
                        item.FictionPlanId,
                        planName,
                        item.BacklogId,
                        item.Description,
                        item.Status,
                        item.CreatedAtUtc,
                        item.UpdatedAtUtc,
                        age));
                }
            }

            if (item.InProgressAtUtc is DateTime inProgressAt && inProgressAt >= transitionCutoff)
            {
                recentTransitions.Add(new PlannerHealthBacklogTransition(
                    item.FictionPlanId,
                    planName,
                    item.BacklogId,
                    item.Description,
                    FictionPlanBacklogStatus.InProgress,
                    inProgressAt,
                    now - inProgressAt));
            }

            if (item.CompletedAtUtc is DateTime completedAt && completedAt >= transitionCutoff)
            {
                recentTransitions.Add(new PlannerHealthBacklogTransition(
                    item.FictionPlanId,
                    planName,
                    item.BacklogId,
                    item.Description,
                    FictionPlanBacklogStatus.Complete,
                    completedAt,
                    now - completedAt));
            }
        }

        recentTransitions.Sort((a, b) => b.OccurredAtUtc.CompareTo(a.OccurredAtUtc));
        var planSummariesList = planSummaries
            .Select(kvp => new PlannerHealthBacklogPlanSummary(
                kvp.Key,
                kvp.Value.Name,
                kvp.Value.Pending,
                kvp.Value.InProgress,
                kvp.Value.Complete,
                kvp.Value.LastUpdatedUtc,
                kvp.Value.LastCompletedUtc))
            .OrderByDescending(summary => summary.LastUpdatedUtc ?? DateTime.MinValue)
            .ThenBy(summary => summary.PlanName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PlannerHealthBacklog(
            TotalItems: total,
            Pending: pending,
            InProgress: inProgress,
            Complete: complete,
            Plans: planSummariesList,
            RecentTransitions: recentTransitions,
            StaleItems: staleItems,
            OrphanedItems: orphaned);
    }

    
    private async Task<PlannerHealthWorldBibleReport> BuildWorldBibleReportAsync(CancellationToken ct)
    {
        var worldBibles = await _db.FictionWorldBibles
            .AsNoTracking()
            .Include(b => b.FictionPlan)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (worldBibles.Count == 0)
        {
            return new PlannerHealthWorldBibleReport(Array.Empty<PlannerHealthWorldBiblePlan>());
        }

        var bibleIds = worldBibles.Select(b => b.Id).ToArray();
        var entries = await _db.FictionWorldBibleEntries
            .AsNoTracking()
            .Where(e => bibleIds.Contains(e.FictionWorldBibleId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var entryLookup = entries
            .GroupBy(e => e.FictionWorldBibleId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var plans = new List<PlannerHealthWorldBiblePlan>(worldBibles.Count);
        foreach (var bible in worldBibles)
        {
            entryLookup.TryGetValue(bible.Id, out var entryList);
            entryList ??= new List<FictionWorldBibleEntry>();

            var activeEntries = entryList
                .Where(e => e.IsActive)
                .OrderBy(e => e.EntrySlug)
                .Select(e => new PlannerHealthWorldBibleEntry(
                    Category: e.Content.Category,
                    EntrySlug: e.EntrySlug,
                    EntryName: e.EntryName,
                    Summary: e.Content.Summary,
                    Status: e.Content.Status,
                    ContinuityNotes: e.Content.ContinuityNotes ?? Array.Empty<string>(),
                    Version: e.Version,
                    IsActive: e.IsActive,
                    IterationIndex: e.Content.IterationIndex,
                    BacklogItemId: e.Content.BacklogItemId,
                    UpdatedAtUtc: e.Content.UpdatedAtUtc != default ? e.Content.UpdatedAtUtc : (e.UpdatedAtUtc ?? e.CreatedAtUtc)))
                .ToList();

            var lastUpdated = entryList.Count == 0
                ? (DateTime?)null
                : entryList.Max(e => e.UpdatedAtUtc ?? e.CreatedAtUtc);

            var planName = bible.FictionPlan?.Name;
            if (string.IsNullOrWhiteSpace(planName))
            {
                planName = $"Plan {bible.FictionPlanId:N}";
            }

            plans.Add(new PlannerHealthWorldBiblePlan(
                PlanId: bible.FictionPlanId,
                PlanName: planName,
                WorldBibleId: bible.Id,
                Domain: bible.Domain,
                BranchSlug: bible.BranchSlug,
                LastUpdatedUtc: lastUpdated,
                ActiveEntries: activeEntries));
        }

        return new PlannerHealthWorldBibleReport(
            plans.OrderByDescending(p => p.LastUpdatedUtc ?? DateTime.MinValue).ToList());
    }

private async Task<PlannerHealthTelemetry> BuildTelemetryReportAsync(CancellationToken ct)
    {
        var executions = _db.PlannerExecutions.AsNoTracking();
        var total = await executions.CountAsync(ct).ConfigureAwait(false);

        PlannerExecution? latest = null;
        if (total > 0)
        {
            latest = await executions
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }

        var outcomeCounts = await executions
            .GroupBy(x => x.Outcome)
            .Select(g => new
            {
                Outcome = string.IsNullOrWhiteSpace(g.Key) ? "Unknown" : g.Key,
                Count = g.Count()
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var outcomeMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in outcomeCounts)
        {
            outcomeMap[entry.Outcome] = entry.Count;
        }

        var cutoff = DateTime.UtcNow - RecentFailureWindow;
        var failureRows = await executions
            .Where(x => x.Outcome != nameof(PlannerOutcome.Success) && x.CreatedAtUtc >= cutoff)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(10)
            .Select(x => new
            {
                x.Id,
                x.PlannerName,
                x.Outcome,
                x.CreatedAtUtc,
                x.Diagnostics,
                x.ConversationId,
                x.Transcript
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var failures = new List<PlannerHealthExecutionFailure>(failureRows.Count);
        foreach (var failure in failureRows)
        {
            var (messageId, role, snippet) = ExtractTranscriptSummary(failure.Transcript);
            failures.Add(new PlannerHealthExecutionFailure(
                failure.Id,
                failure.PlannerName,
                string.IsNullOrWhiteSpace(failure.Outcome) ? "Unknown" : failure.Outcome,
                failure.CreatedAtUtc,
                failure.Diagnostics,
                failure.ConversationId,
                messageId,
                role,
                snippet));
        }

        var critiqueStatusCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var recentDiagnostics = await executions
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .Select(x => x.Diagnostics)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var diagnostics in recentDiagnostics)
        {
            if (diagnostics is null)
            {
                continue;
            }

            if (!diagnostics.TryGetValue("critiqueStatus", out var status) || string.IsNullOrWhiteSpace(status))
            {
                continue;
            }

            var normalized = status.Trim().ToLowerInvariant();
            if (critiqueStatusCounts.TryGetValue(normalized, out var count))
            {
                critiqueStatusCounts[normalized] = count + 1;
            }
            else
            {
                critiqueStatusCounts[normalized] = 1;
            }
        }

        return new PlannerHealthTelemetry(
            TotalExecutions: total,
            LastExecutionUtc: latest?.CreatedAtUtc,
            OutcomeCounts: outcomeMap,
            CritiqueStatusCounts: critiqueStatusCounts,
            RecentFailures: failures);
    }

    private static PlannerHealthStatus DetermineStatus(
        IReadOnlyList<PlannerHealthPlanner> planners,
        PlannerHealthBacklog backlog,
        PlannerHealthTelemetry telemetry)
    {
        var hasCriticalTemplateIssue = planners
            .SelectMany(p => p.Steps)
            .Any(s => !s.TemplateFound || !s.TemplateActive);

        if (hasCriticalTemplateIssue)
        {
            return PlannerHealthStatus.Critical;
        }

        var hasDegradedBacklog = backlog.StaleItems.Count > 0 || backlog.OrphanedItems.Count > 0;
        var hasRecentFailures = telemetry.RecentFailures.Count > 0;
        var hasNoExecutions = telemetry.TotalExecutions == 0;
        var hasCritiqueAlerts = telemetry.CritiqueStatusCounts.Keys.Any(k =>
            string.Equals(k, "count-exhausted", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(k, "token-exhausted", StringComparison.OrdinalIgnoreCase));

        if (hasDegradedBacklog || hasRecentFailures || hasNoExecutions || hasCritiqueAlerts)
        {
            return PlannerHealthStatus.Degraded;
        }

        return PlannerHealthStatus.Healthy;
    }

    private static IReadOnlyList<PlannerHealthAlert> BuildAlerts(
        IReadOnlyList<PlannerHealthPlanner> planners,
        PlannerHealthBacklog backlog,
        PlannerHealthTelemetry telemetry,
        DateTime generatedAtUtc)
    {
        var alerts = new List<PlannerHealthAlert>();

        void AddAlert(string id, PlannerHealthAlertSeverity severity, string title, string description)
        {
            alerts.Add(new PlannerHealthAlert(id, severity, title, description, generatedAtUtc));
        }

        foreach (var planner in planners)
        {
            foreach (var step in planner.Steps)
            {
                if (!step.TemplateFound)
                {
                    var description = $"Planner '{planner.Name}' is missing template '{step.TemplateId ?? "(not set)"}' for step '{step.DisplayName}'.";
                    AddAlert(BuildAlertId("template-missing", $"{planner.Name}:{step.StepId}"), PlannerHealthAlertSeverity.Error, "Planner template missing", description);
                }
                else if (!step.TemplateActive)
                {
                    var description = $"Planner '{planner.Name}' template '{step.TemplateId}' for step '{step.DisplayName}' is inactive.";
                    AddAlert(BuildAlertId("template-inactive", $"{planner.Name}:{step.StepId}"), PlannerHealthAlertSeverity.Warning, "Planner template inactive", description);
                }
            }
        }

        if (backlog.StaleItems.Count > 0)
        {
            AddAlert("backlog:stale", PlannerHealthAlertSeverity.Warning, "Stale backlog items detected", $"{backlog.StaleItems.Count} backlog item(s) exceeded the freshness SLO.");
        }

        if (backlog.OrphanedItems.Count > 0)
        {
            AddAlert("backlog:orphaned", PlannerHealthAlertSeverity.Warning, "Orphaned backlog items detected", $"{backlog.OrphanedItems.Count} backlog item(s) are no longer attached to a plan.");
        }

        if (telemetry.RecentFailures.Count > 0)
        {
            AddAlert("planner:recent-failures", PlannerHealthAlertSeverity.Error, "Recent planner failures", $"{telemetry.RecentFailures.Count} planner execution(s) failed in the last 24h.");
        }

        if (telemetry.TotalExecutions == 0)
        {
            AddAlert("planner:no-executions", PlannerHealthAlertSeverity.Info, "No planner executions observed", "Planner telemetry has not observed any executions yet.");
        }

        foreach (var kvp in telemetry.CritiqueStatusCounts)
        {
            if (kvp.Key.Contains("exhausted", StringComparison.OrdinalIgnoreCase))
            {
                AddAlert(BuildAlertId("planner:critique", kvp.Key), PlannerHealthAlertSeverity.Warning, "Critique budget exhausted", $"{kvp.Value} execution(s) hit critique budget '{kvp.Key}'.");
            }
        }

        foreach (var flap in DetectFlappingTransitions(backlog.RecentTransitions))
        {
            AddAlert(
                BuildAlertId("backlog:flapping", $"{flap.PlanId}:{flap.BacklogId}"),
                PlannerHealthAlertSeverity.Warning,
                "Backlog item flapping",
                $"{flap.PlanName} • {flap.BacklogId} returned to pending {flap.PendingCount} time(s) in the latest window.");
        }

        return alerts;
    }

    private static IEnumerable<FlapTracker> DetectFlappingTransitions(IReadOnlyList<PlannerHealthBacklogTransition> transitions)
    {
        var map = new Dictionary<string, FlapTracker>(StringComparer.OrdinalIgnoreCase);
        foreach (var transition in transitions)
        {
            var key = $"{transition.PlanId:N}:{transition.BacklogId}";
            var tracker = map.TryGetValue(key, out var existing)
                ? existing
                : new FlapTracker(transition.PlanId, transition.PlanName, transition.BacklogId, 0);

            if (transition.Status == FictionPlanBacklogStatus.Pending)
            {
                tracker = tracker with { PendingCount = tracker.PendingCount + 1 };
            }

            map[key] = tracker;
        }

        return map.Values.Where(v => v.PendingCount >= 3);
    }

    private static string BuildAlertId(string prefix, string value)
        => $"{prefix}:{value}".Replace(" ", "-", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();

    private static PlanSummaryAccumulator GetPlanSummaryAccumulator(
        IDictionary<Guid, PlanSummaryAccumulator> map,
        Guid planId,
        string planName)
    {
        if (!map.TryGetValue(planId, out var accumulator))
        {
            accumulator = new PlanSummaryAccumulator(planName);
            map[planId] = accumulator;
        }

        return accumulator;
    }

    private static (Guid? messageId, string? role, string? snippet) ExtractTranscriptSummary(
        IReadOnlyList<PlannerExecutionTranscriptEntry>? transcript)
    {
        if (transcript is null || transcript.Count == 0)
        {
            return (null, null, null);
        }

        PlannerExecutionTranscriptEntry? latest = null;
        foreach (var entry in transcript)
        {
            if (latest is null || entry.TimestampUtc > latest.TimestampUtc)
            {
                latest = entry;
            }
        }

        if (latest is null)
        {
            return (null, null, null);
        }

        Guid? messageId = null;
        if (latest.Metadata is not null && latest.Metadata.TryGetValue("conversationMessageId", out var rawValue) && rawValue is not null)
        {
            if (rawValue is Guid guid)
            {
                messageId = guid;
            }
            else if (rawValue is string s && Guid.TryParse(s, out var parsed))
            {
                messageId = parsed;
            }
            else if (rawValue is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out var elementGuid))
                {
                    messageId = elementGuid;
                }
            }
        }

        var snippet = latest.Message;
        if (!string.IsNullOrWhiteSpace(snippet) && snippet!.Length > 512)
        {
            snippet = snippet.Substring(0, 512);
        }

        return (messageId, latest.Role, snippet);
    }

    private sealed record PlannerSnapshot(
        IReadOnlyList<PlannerHealthPlanner> Planners,
        HashSet<string> TemplateIds);

    private sealed class PlanSummaryAccumulator
    {
        public PlanSummaryAccumulator(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public int Pending;
        public int InProgress;
        public int Complete;
        public DateTime? LastUpdatedUtc;
        public DateTime? LastCompletedUtc;
    }

    private sealed record FlapTracker(Guid PlanId, string PlanName, string BacklogId, int PendingCount);
}
