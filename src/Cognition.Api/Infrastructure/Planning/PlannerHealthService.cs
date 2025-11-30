using System;
using System.Collections.Concurrent;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Planning;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Planning;
using Cognition.Data.Relational.Modules.Prompts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Newtonsoft.Json.Linq;

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
    Guid? AgentId,
    Guid? PersonaId,
    Guid? SourcePlanPassId,
    Guid? SourceConversationId,
    string? SourceBacklogId,
    string? BranchSlug,
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
    IReadOnlyList<PlannerHealthBacklogItem> OrphanedItems,
    IReadOnlyList<PlannerBacklogTelemetry> TelemetryEvents,
    IReadOnlyList<PlannerBacklogActionLog> ActionLogs);

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

public sealed record PlannerBacklogTelemetry(
    Guid PlanId,
    string PlanName,
    string BacklogId,
    string Phase,
    string Status,
    string? PreviousStatus,
    string Reason,
    string Branch,
    int? Iteration,
    DateTime TimestampUtc,
    IReadOnlyDictionary<string, string?>? Metadata,
    PlannerBacklogTelemetryCharacterMetrics? CharacterMetrics,
    PlannerBacklogTelemetryLoreMetrics? LoreMetrics,
    IReadOnlyList<PlannerBacklogTelemetryCharacter> RecentCharacters,
    IReadOnlyList<PlannerBacklogTelemetryLore> PendingLore);

public sealed record PlannerBacklogTelemetryCharacter(
    Guid Id,
    string Slug,
    string DisplayName,
    Guid? PersonaId,
    Guid? WorldBibleEntryId,
    string? Role,
    string? Importance,
    DateTime UpdatedAtUtc);

public sealed record PlannerBacklogTelemetryLore(
    Guid Id,
    string RequirementSlug,
    string Title,
    string Status,
    Guid? WorldBibleEntryId,
    DateTime UpdatedAtUtc);

public sealed record PlannerBacklogActionLog(
    Guid PlanId,
    string PlanName,
    string BacklogId,
    string? Description,
    string Action,
    string Branch,
    string? Actor,
    string? ActorId,
    string Source,
    Guid? ProviderId,
    Guid? ModelId,
    Guid? AgentId,
    string? Status,
    Guid? ConversationId,
    Guid? ConversationPlanId,
    Guid? TaskId,
    DateTime TimestampUtc);

public sealed record PlannerBacklogTelemetryCharacterMetrics(int Total, int PersonaLinked, int WorldBibleLinked);

public sealed record PlannerBacklogTelemetryLoreMetrics(int Total, int Ready, int Blocked);

public sealed record PlannerHealthTelemetry(
    int TotalExecutions,
    DateTime? LastExecutionUtc,
    IReadOnlyDictionary<string, int> OutcomeCounts,
    IReadOnlyDictionary<string, int> CritiqueStatusCounts,
    IReadOnlyList<PlannerHealthExecutionFailure> RecentFailures);

internal sealed record LorePlanStatus(int Blocked, int Planned);

internal sealed record ObligationPlanStatus(int Open, int Drift, int Aging);

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
    private static readonly TimeSpan WorldBibleStaleThreshold = TimeSpan.FromHours(6);

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
        var worldBibleIssues = DetectWorldBibleIssues(worldBible);
        foreach (var issue in worldBibleIssues)
        {
            switch (issue.Type)
            {
                case WorldBibleIssueType.MissingEntries:
                    warnings.Add($"Plan '{issue.Plan.PlanName}' has no active world-bible entries.");
                    break;
                case WorldBibleIssueType.Stale when issue.Age is TimeSpan age:
                    warnings.Add($"Plan '{issue.Plan.PlanName}' world-bible is stale ({age:g} since last update).");
                    break;
            }
        }

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

        if (worldBible.Plans.Count == 0)
        {
            warnings.Add("No world-bible snapshots have been recorded yet.");
        }

        var loreStatus = await LoadLoreStatusAsync(ct).ConfigureAwait(false);
        var obligationStatus = await LoadObligationStatusAsync(ct).ConfigureAwait(false);

        var alerts = BuildAlerts(plannerReports, backlog, worldBibleIssues, telemetry, loreStatus, obligationStatus, generatedAt);
        var status = DetermineStatus(plannerReports, backlog, worldBibleIssues, telemetry, alerts);

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

        var telemetryEvents = await LoadBacklogTelemetryAsync(ct).ConfigureAwait(false);
        var actionLogs = await LoadBacklogActionLogsAsync(ct).ConfigureAwait(false);

        return new PlannerHealthBacklog(
            TotalItems: total,
            Pending: pending,
            InProgress: inProgress,
            Complete: complete,
            Plans: planSummariesList,
            RecentTransitions: recentTransitions,
            StaleItems: staleItems,
            OrphanedItems: orphaned,
            TelemetryEvents: telemetryEvents,
            ActionLogs: actionLogs);
    }

    
    private async Task<Dictionary<Guid, LorePlanStatus>> LoadLoreStatusAsync(CancellationToken ct)
    {
        if (_db.Model.FindEntityType(typeof(FictionLoreRequirement)) is null)
        {
            return new Dictionary<Guid, LorePlanStatus>();
        }

        var stats = await _db.FictionLoreRequirements
            .AsNoTracking()
            .GroupBy(r => r.FictionPlanId)
            .Select(g => new
            {
                PlanId = g.Key,
                Blocked = g.Count(r => r.Status == FictionLoreRequirementStatus.Blocked),
                Planned = g.Count(r => r.Status == FictionLoreRequirementStatus.Planned)
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return stats.ToDictionary(
            s => s.PlanId,
            s => new LorePlanStatus(s.Blocked, s.Planned));
    }

    private async Task<Dictionary<Guid, ObligationPlanStatus>> LoadObligationStatusAsync(CancellationToken ct)
    {
        if (_db.Model.FindEntityType(typeof(FictionPersonaObligation)) is null)
        {
            return new Dictionary<Guid, ObligationPlanStatus>();
        }

        var cutoff = DateTime.UtcNow.AddHours(-12);
        var obligations = await _db.FictionPersonaObligations
            .AsNoTracking()
            .Select(o => new
            {
                o.FictionPlanId,
                o.Status,
                o.MetadataJson,
                o.CreatedAtUtc
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var grouped = obligations.GroupBy(o => o.FictionPlanId);
        var result = new Dictionary<Guid, ObligationPlanStatus>();

        foreach (var group in grouped)
        {
            var open = group.Count(o => o.Status == FictionPersonaObligationStatus.Open);
            var drift = group.Count(o => o.MetadataJson != null && o.MetadataJson.Contains("voiceDrift", StringComparison.OrdinalIgnoreCase));
            var aging = group.Count(o => o.Status == FictionPersonaObligationStatus.Open && o.CreatedAtUtc < cutoff);
            result[group.Key] = new ObligationPlanStatus(open, drift, aging);
        }

        return result;
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
                    AgentId: e.AgentId,
                    PersonaId: e.PersonaId,
                    SourcePlanPassId: e.SourcePlanPassId,
                    SourceConversationId: e.SourceConversationId,
                    SourceBacklogId: e.SourceBacklogId,
                    BranchSlug: e.BranchSlug,
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

    private async Task<IReadOnlyList<PlannerBacklogTelemetry>> LoadBacklogTelemetryAsync(CancellationToken ct)
    {
        var events = await _db.WorkflowEvents
            .AsNoTracking()
            .Where(e => e.Kind == "fiction.backlog.telemetry" || e.Kind == "fiction.backlog.contract")
            .OrderByDescending(e => e.Timestamp)
            .Take(50)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (events.Count == 0)
        {
            return Array.Empty<PlannerBacklogTelemetry>();
        }

        var planIds = events
            .Select(e => e.Payload is JObject o ? ReadGuid(o, "planId") : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .Distinct()
            .ToArray();

        var planLookup = planIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _db.FictionPlans
                .AsNoTracking()
                .Where(p => planIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct)
                .ConfigureAwait(false);

        var list = new List<PlannerBacklogTelemetry>(events.Count);
        foreach (var evt in events)
        {
            if (evt.Payload is not JObject payload)
            {
                continue;
            }

            var planId = ReadGuid(payload, "planId");
            if (!planId.HasValue)
            {
                continue;
            }

            var planName = planLookup.TryGetValue(planId.Value, out var resolvedName)
                ? resolvedName
                : $"Plan {planId.Value:N}";

            if (string.Equals(evt.Kind, "fiction.backlog.contract", StringComparison.OrdinalIgnoreCase))
            {
                var metadata = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["code"] = payload.Value<string>("code"),
                    ["providerId"] = payload.Value<string>("providerId"),
                    ["modelId"] = payload.Value<string>("modelId"),
                    ["agentId"] = payload.Value<string>("agentId"),
                    ["conversationId"] = payload.Value<string>("conversationId"),
                    ["conversationPlanId"] = payload.Value<string>("conversationPlanId"),
                    ["taskId"] = payload.Value<string>("taskId")
                };

                list.Add(new PlannerBacklogTelemetry(
                    PlanId: planId.Value,
                    PlanName: planName,
                    BacklogId: payload.Value<string>("backlogId") ?? "(unknown)",
                    Phase: "resume",
                    Status: "contract",
                    PreviousStatus: payload.Value<string>("backlogStatus") ?? payload.Value<string>("status"),
                    Reason: payload.Value<string>("code") ?? "contract-mismatch",
                    Branch: payload.Value<string>("branch") ?? "main",
                    Iteration: null,
                    TimestampUtc: evt.Timestamp,
                    Metadata: metadata,
                    CharacterMetrics: null,
                    LoreMetrics: null,
                    RecentCharacters: Array.Empty<PlannerBacklogTelemetryCharacter>(),
                    PendingLore: Array.Empty<PlannerBacklogTelemetryLore>()));
                continue;
            }

            var characterMetrics = payload["characterMetrics"] is JObject charMetrics
                ? new PlannerBacklogTelemetryCharacterMetrics(
                    charMetrics.Value<int?>("total") ?? 0,
                    charMetrics.Value<int?>("personaLinked") ?? 0,
                    charMetrics.Value<int?>("worldBibleLinked") ?? 0)
                : null;

            var loreMetrics = payload["loreMetrics"] is JObject loreMetricsObj
                ? new PlannerBacklogTelemetryLoreMetrics(
                    loreMetricsObj.Value<int?>("total") ?? 0,
                    loreMetricsObj.Value<int?>("ready") ?? 0,
                    loreMetricsObj.Value<int?>("blocked") ?? 0)
                : null;

            var telemetry = new PlannerBacklogTelemetry(
                PlanId: planId.Value,
                PlanName: planName,
                BacklogId: payload.Value<string>("backlogId") ?? "(unknown)",
                Phase: payload.Value<string>("phase") ?? "unknown",
                Status: payload.Value<string>("status") ?? "unknown",
                PreviousStatus: payload.Value<string>("previousStatus"),
                Reason: payload.Value<string>("reason") ?? string.Empty,
                Branch: payload.Value<string>("branch") ?? "main",
                Iteration: payload.Value<int?>("iteration"),
                TimestampUtc: evt.Timestamp,
                Metadata: ConvertMetadata(payload["metadata"]),
                CharacterMetrics: characterMetrics,
                LoreMetrics: loreMetrics,
                RecentCharacters: ReadCharacterSnapshots(payload["recentCharacters"]),
                PendingLore: ReadLoreSnapshots(payload["pendingLore"]));

            list.Add(telemetry);
        }

        return list;
    }

    private async Task<IReadOnlyList<PlannerBacklogActionLog>> LoadBacklogActionLogsAsync(CancellationToken ct)
    {
        var events = await _db.WorkflowEvents
            .AsNoTracking()
            .Where(e => e.Kind == "fiction.backlog.action")
            .OrderByDescending(e => e.Timestamp)
            .Take(50)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (events.Count == 0)
        {
            return Array.Empty<PlannerBacklogActionLog>();
        }

        var planIds = events
            .Select(e => e.Payload is JObject o ? ReadGuid(o, "planId") : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .Distinct()
            .ToArray();

        var planLookup = planIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _db.FictionPlans
                .AsNoTracking()
                .Where(p => planIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct)
                .ConfigureAwait(false);

        var logs = new List<PlannerBacklogActionLog>(events.Count);
        foreach (var evt in events)
        {
            if (evt.Payload is not JObject payload)
            {
                continue;
            }

            var planId = ReadGuid(payload, "planId");
            if (!planId.HasValue)
            {
                continue;
            }

            var planName = planLookup.TryGetValue(planId.Value, out var resolved)
                ? resolved
                : $"Plan {planId.Value:N}";

            var statusValue = payload.Value<string>("status");
            FictionPlanBacklogStatus? status = null;
            if (!string.IsNullOrWhiteSpace(statusValue) &&
                Enum.TryParse(statusValue, true, out FictionPlanBacklogStatus parsedStatus))
            {
                status = parsedStatus;
            }

            logs.Add(new PlannerBacklogActionLog(
                PlanId: planId.Value,
                PlanName: planName,
                BacklogId: payload.Value<string>("backlogId") ?? "(unknown)",
                Description: payload.Value<string>("description"),
                Action: payload.Value<string>("action") ?? "unknown",
                Branch: payload.Value<string>("branch") ?? "main",
                Actor: payload.Value<string>("actor"),
                ActorId: payload.Value<string>("actorId"),
                Source: payload.Value<string>("source") ?? "api",
                ProviderId: ReadGuid(payload, "providerId"),
                ModelId: ReadGuid(payload, "modelId"),
                AgentId: ReadGuid(payload, "agentId"),
                Status: status?.ToString(),
                ConversationId: ReadGuid(payload, "conversationId"),
                ConversationPlanId: ReadGuid(payload, "conversationPlanId"),
                TaskId: ReadGuid(payload, "taskId"),
                TimestampUtc: evt.Timestamp));
        }

        return logs;
    }

    private static IReadOnlyList<PlannerBacklogTelemetryCharacter> ReadCharacterSnapshots(JToken? token)
    {
        if (token is not JArray array || array.Count == 0)
        {
            return Array.Empty<PlannerBacklogTelemetryCharacter>();
        }

        var results = new List<PlannerBacklogTelemetryCharacter>(array.Count);
        foreach (var obj in array.OfType<JObject>())
        {
            var id = ReadGuid(obj, "id") ?? Guid.NewGuid();
            results.Add(new PlannerBacklogTelemetryCharacter(
                Id: id,
                Slug: obj.Value<string>("slug") ?? id.ToString("N"),
                DisplayName: obj.Value<string>("displayName") ?? obj.Value<string>("name") ?? "(unknown character)",
                PersonaId: ReadGuid(obj, "personaId"),
                WorldBibleEntryId: ReadGuid(obj, "worldBibleEntryId"),
                Role: obj.Value<string>("role"),
                Importance: obj.Value<string>("importance"),
                UpdatedAtUtc: obj.Value<DateTime?>("updatedAtUtc") ?? DateTime.UtcNow));
        }

        return results;
    }

    private static IReadOnlyList<PlannerBacklogTelemetryLore> ReadLoreSnapshots(JToken? token)
    {
        if (token is not JArray array || array.Count == 0)
        {
            return Array.Empty<PlannerBacklogTelemetryLore>();
        }

        var results = new List<PlannerBacklogTelemetryLore>(array.Count);
        foreach (var obj in array.OfType<JObject>())
        {
            var id = ReadGuid(obj, "id") ?? Guid.NewGuid();
            results.Add(new PlannerBacklogTelemetryLore(
                Id: id,
                RequirementSlug: obj.Value<string>("slug") ?? obj.Value<string>("requirementSlug") ?? id.ToString("N"),
                Title: obj.Value<string>("title") ?? "(unknown lore)",
                Status: obj.Value<string>("status") ?? "unknown",
                WorldBibleEntryId: ReadGuid(obj, "worldBibleEntryId"),
                UpdatedAtUtc: obj.Value<DateTime?>("updatedAtUtc") ?? DateTime.UtcNow));
        }

        return results;
    }

    private static IReadOnlyDictionary<string, string?>? ConvertMetadata(JToken? token)
    {
        if (token is not JObject obj || obj.Count == 0)
        {
            return null;
        }

        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in obj.Properties())
        {
            dict[prop.Name] = prop.Value.Type == JTokenType.Null ? null : prop.Value.ToString();
        }

        return dict;
    }

    private static Guid? ReadGuid(JObject obj, string property)
    {
        var value = obj.Value<string>(property);
        return Guid.TryParse(value, out var result) ? result : null;
    }

    private IReadOnlyList<WorldBibleIssue> DetectWorldBibleIssues(PlannerHealthWorldBibleReport report)
    {
        if (report.Plans.Count == 0)
        {
            return Array.Empty<WorldBibleIssue>();
        }

        var now = DateTime.UtcNow;
        var issues = new List<WorldBibleIssue>();
        foreach (var plan in report.Plans)
        {
            if (plan.ActiveEntries.Count == 0)
            {
                issues.Add(new WorldBibleIssue(plan, WorldBibleIssueType.MissingEntries, null));
                continue;
            }

            if (!plan.LastUpdatedUtc.HasValue)
            {
                issues.Add(new WorldBibleIssue(plan, WorldBibleIssueType.MissingEntries, null));
                continue;
            }

            var age = now - plan.LastUpdatedUtc.Value;
            if (age > WorldBibleStaleThreshold)
            {
                issues.Add(new WorldBibleIssue(plan, WorldBibleIssueType.Stale, age));
            }
        }

        return issues;
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
        IReadOnlyList<WorldBibleIssue> worldBibleIssues,
        PlannerHealthTelemetry telemetry,
        IReadOnlyList<PlannerHealthAlert> alerts)
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
        var hasWorldBibleIssues = worldBibleIssues.Count > 0;
        var hasWorldBibleAlerts = alerts.Any(a => a.Id.StartsWith("worldbible:", StringComparison.OrdinalIgnoreCase));
        var hasContractAlerts = alerts.Any(a => a.Id.StartsWith("backlog:contract", StringComparison.OrdinalIgnoreCase));
        var hasCriticalContractAlerts = alerts.Any(a =>
            a.Id.StartsWith("backlog:contract", StringComparison.OrdinalIgnoreCase) &&
            a.Severity == PlannerHealthAlertSeverity.Error);
        var hasLoreAlerts = alerts.Any(a => a.Id.StartsWith("lore:", StringComparison.OrdinalIgnoreCase));
        var hasObligationAlerts = alerts.Any(a => a.Id.StartsWith("obligation:", StringComparison.OrdinalIgnoreCase));

        if (hasCriticalContractAlerts)
        {
            return PlannerHealthStatus.Critical;
        }

        if (hasDegradedBacklog || hasRecentFailures || hasNoExecutions || hasCritiqueAlerts || hasWorldBibleIssues || hasWorldBibleAlerts || hasContractAlerts || hasLoreAlerts || hasObligationAlerts)
        {
            return PlannerHealthStatus.Degraded;
        }

        return PlannerHealthStatus.Healthy;
    }

    private static IReadOnlyList<PlannerHealthAlert> BuildAlerts(
        IReadOnlyList<PlannerHealthPlanner> planners,
        PlannerHealthBacklog backlog,
        IReadOnlyList<WorldBibleIssue> worldBibleIssues,
        PlannerHealthTelemetry telemetry,
        IReadOnlyDictionary<Guid, LorePlanStatus> loreStatus,
        IReadOnlyDictionary<Guid, ObligationPlanStatus> obligationStatus,
        DateTime generatedAtUtc)
    {
        var alerts = new List<PlannerHealthAlert>();

        void AddAlert(string id, PlannerHealthAlertSeverity severity, string title, string description)
        {
            alerts.Add(new PlannerHealthAlert(id, severity, title, description, generatedAtUtc));
        }

        string ResolvePlanName(Guid planId)
        {
            var plan = backlog.Plans.FirstOrDefault(p => p.PlanId == planId);
            return plan?.PlanName ?? planId.ToString("N");
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

        var loreGaps = loreStatus
            .Where(kvp => kvp.Value.Blocked + kvp.Value.Planned > 0)
            .ToList();
        if (loreGaps.Count > 0)
        {
            var planNames = loreGaps.Select(kvp => $"{ResolvePlanName(kvp.Key)} ({kvp.Value.Blocked} blocked, {kvp.Value.Planned} planned)").ToArray();
            AddAlert("lore:blocked", PlannerHealthAlertSeverity.Warning, "Lore gaps detected", $"Lore requirements remain blocked or unfulfilled for {loreGaps.Count} plan(s): {string.Join("; ", planNames)}.");
        }

        var obligationIssues = obligationStatus
            .Where(kvp => kvp.Value.Open > 0 || kvp.Value.Drift > 0 || kvp.Value.Aging > 0)
            .ToList();
        if (obligationIssues.Count > 0)
        {
            var planNames = obligationIssues.Select(kvp =>
                $"{ResolvePlanName(kvp.Key)} (open {kvp.Value.Open}, drift {kvp.Value.Drift}, aging {kvp.Value.Aging})").ToArray();
            AddAlert("obligation:open", PlannerHealthAlertSeverity.Warning, "Persona obligations require attention", $"Persona obligations remain open for {obligationIssues.Count} plan(s): {string.Join("; ", planNames)}.");
        }

        var contractEvents = backlog.TelemetryEvents
            .Where(evt => string.Equals(evt.Status, "contract", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (contractEvents.Count > 0)
        {
            var plans = contractEvents
                .GroupBy(evt => evt.PlanId)
                .Select(group => $"{group.First().PlanName} ({group.Count()})")
                .ToArray();

            var severity = contractEvents.Count >= 2
                ? PlannerHealthAlertSeverity.Error
                : PlannerHealthAlertSeverity.Warning;

            var description = $"{contractEvents.Count} backlog contract drift event(s) detected across {plans.Length} plan(s): {string.Join(", ", plans)}.";
            AddAlert("backlog:contract-drift", severity, "Backlog contract drift", description);
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

        foreach (var issue in worldBibleIssues)
        {
            switch (issue.Type)
            {
                case WorldBibleIssueType.MissingEntries:
                    AddAlert(
                        BuildAlertId("worldbible:missing", $"{issue.Plan.PlanId:N}"),
                        PlannerHealthAlertSeverity.Warning,
                        "World-bible entries missing",
                        $"Plan '{issue.Plan.PlanName}' has no active world-bible entries.");
                    break;
                case WorldBibleIssueType.Stale:
                    var age = issue.Age ?? TimeSpan.Zero;
                    AddAlert(
                        BuildAlertId("worldbible:stale", $"{issue.Plan.PlanId:N}"),
                        PlannerHealthAlertSeverity.Warning,
                        "World-bible entries stale",
                        $"Plan '{issue.Plan.PlanName}' world-bible has not been updated for {age:g}.");
                    break;
            }
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

    private sealed record WorldBibleIssue(
        PlannerHealthWorldBiblePlan Plan,
        WorldBibleIssueType Type,
        TimeSpan? Age);

    private enum WorldBibleIssueType
    {
        MissingEntries,
        Stale
    }

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
