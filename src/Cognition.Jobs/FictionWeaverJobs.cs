using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Clients.Tools.Planning;
using Cognition.Contracts;
using Cognition.Contracts.Events;
using Cognition.Contracts.Scopes;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rebus.Bus;

namespace Cognition.Jobs;

public class FictionWeaverJobs
{
    private readonly CognitionDbContext _db;
    private readonly ILogger<FictionWeaverJobs> _logger;
    private readonly IReadOnlyDictionary<FictionPhase, IFictionPhaseRunner> _runnerLookup;
    private readonly IBus _bus;
    private readonly IPlanProgressNotifier _planNotifier;
    private readonly WorkflowEventLogger _workflowLogger;
    private readonly IFictionBacklogScheduler _backlogScheduler;
    private readonly IScopePathBuilder _scopePaths;
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public FictionWeaverJobs(
        CognitionDbContext db,
        IEnumerable<IFictionPhaseRunner> runners,
        IBus bus,
        IPlanProgressNotifier notifier,
        WorkflowEventLogger workflowLogger,
        IFictionBacklogScheduler backlogScheduler,
        IScopePathBuilder scopePathBuilder,
        ILogger<FictionWeaverJobs> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runnerLookup = runners?.ToDictionary(r => r.Phase) ?? throw new ArgumentNullException(nameof(runners));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _planNotifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _workflowLogger = workflowLogger ?? throw new ArgumentNullException(nameof(workflowLogger));
        _backlogScheduler = backlogScheduler ?? throw new ArgumentNullException(nameof(backlogScheduler));
        _scopePaths = scopePathBuilder ?? throw new ArgumentNullException(nameof(scopePathBuilder));
    }

    public Task<FictionPhaseResult> RunVisionPlannerAsync(
        Guid planId,
        Guid agentId,
        Guid conversationId,
        Guid providerId,
        Guid? modelId = null,
        string branchSlug = "main",
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default) =>
        ExecutePhaseAsync(
            FictionPhase.VisionPlanner,
            CreateContext(planId, agentId, conversationId, branchSlug, providerId, modelId, metadata),
            cancellationToken);

    public Task<FictionPhaseResult> RunWorldBibleManagerAsync(
        Guid planId,
        Guid agentId,
        Guid conversationId,
        Guid providerId,
        Guid? modelId = null,
        string branchSlug = "main",
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default) =>
        ExecutePhaseAsync(
            FictionPhase.WorldBibleManager,
            CreateContext(planId, agentId, conversationId, branchSlug, providerId, modelId, metadata),
            cancellationToken);

    public Task<FictionPhaseResult> RunIterativePlannerAsync(
        Guid planId,
        Guid agentId,
        Guid conversationId,
        int iterationIndex,
        Guid providerId,
        Guid? modelId = null,
        string branchSlug = "main",
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default) =>
        ExecutePhaseAsync(
            FictionPhase.IterativePlanner,
            CreateContext(planId, agentId, conversationId, branchSlug, providerId, modelId, metadata, iterationIndex: iterationIndex),
            cancellationToken);

    public Task<FictionPhaseResult> RunChapterArchitectAsync(
        Guid planId,
        Guid agentId,
        Guid conversationId,
        Guid chapterBlueprintId,
        Guid providerId,
        Guid? modelId = null,
        string branchSlug = "main",
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default) =>
        ExecutePhaseAsync(
            FictionPhase.ChapterArchitect,
            CreateContext(planId, agentId, conversationId, branchSlug, providerId, modelId, metadata, chapterBlueprintId: chapterBlueprintId),
            cancellationToken);

    public Task<FictionPhaseResult> RunScrollRefinerAsync(
        Guid planId,
        Guid agentId,
        Guid conversationId,
        Guid chapterScrollId,
        Guid providerId,
        Guid? modelId = null,
        string branchSlug = "main",
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default) =>
        ExecutePhaseAsync(
            FictionPhase.ScrollRefiner,
            CreateContext(planId, agentId, conversationId, branchSlug, providerId, modelId, metadata, chapterScrollId: chapterScrollId),
            cancellationToken);

    public Task<FictionPhaseResult> RunSceneWeaverAsync(
        Guid planId,
        Guid agentId,
        Guid conversationId,
        Guid chapterSceneId,
        Guid providerId,
        Guid? modelId = null,
        string branchSlug = "main",
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default) =>
        ExecutePhaseAsync(
            FictionPhase.SceneWeaver,
            CreateContext(planId, agentId, conversationId, branchSlug, providerId, modelId, metadata, chapterSceneId: chapterSceneId),
            cancellationToken);

    private static FictionPhaseExecutionContext CreateContext(
        Guid planId,
        Guid agentId,
        Guid conversationId,
        string branchSlug,
        Guid providerId,
        Guid? modelId,
        IReadOnlyDictionary<string, string>? metadata,
        Guid? chapterBlueprintId = null,
        Guid? chapterScrollId = null,
        Guid? chapterSceneId = null,
        int? iterationIndex = null,
        string? invokedByJobId = null)
    {
        var merged = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);

        merged["providerId"] = providerId.ToString();
        if (modelId.HasValue)
        {
            merged["modelId"] = modelId.Value.ToString();
        }
        else
        {
            merged.Remove("modelId");
        }

        return new FictionPhaseExecutionContext(
            planId,
            agentId,
            conversationId,
            branchSlug,
            chapterBlueprintId,
            chapterScrollId,
            chapterSceneId,
            iterationIndex,
            invokedByJobId,
            merged);
    }

    private async Task<FictionPhaseResult> ExecutePhaseAsync(FictionPhase phase, FictionPhaseExecutionContext context, CancellationToken cancellationToken)
    {
        if (!_runnerLookup.TryGetValue(phase, out var runner))
        {
            throw new InvalidOperationException($"No runner registered for phase {phase}.");
        }

        var plan = await _db.Set<FictionPlan>()
            .FirstOrDefaultAsync(p => p.Id == context.PlanId, cancellationToken)
            .ConfigureAwait(false);
        if (plan is null)
        {
            throw new InvalidOperationException($"Fiction plan {context.PlanId} was not found.");
        }

        var effectiveContext = NormalizeContext(context, plan.PrimaryBranchSlug);
        effectiveContext = AttachScopeContext(effectiveContext, plan);
        var backlogItemId = GetBacklogItemId(effectiveContext);
        EnsureExecutionMetadata(phase, effectiveContext, backlogItemId);
        var conversationTask = await FindConversationTaskAsync(effectiveContext, cancellationToken).ConfigureAwait(false);

        if (plan.Status == FictionPlanStatus.Draft)
        {
            plan.Status = FictionPlanStatus.InProgress;
            plan.UpdatedAtUtc = DateTime.UtcNow;
        }

        var phaseKey = BuildPhaseKey(phase, effectiveContext);
        var checkpoint = await GetOrCreateCheckpointAsync(plan.Id, phaseKey, cancellationToken).ConfigureAwait(false);

        if (IsMetadataFlagSet(effectiveContext, "cancel"))
        {
            CancelCheckpoint(phase, checkpoint, effectiveContext);
            UpdateConversationTaskStatus(conversationTask, "Cancelled", null, "Cancellation requested prior to execution.");
            if (!string.IsNullOrEmpty(backlogItemId))
            {
                await SetBacklogStatusAsync(plan.Id, backlogItemId!, FictionPlanBacklogStatus.Pending, phase, effectiveContext, "cancel-requested", cancellationToken).ConfigureAwait(false);
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublishProgressAsync(phase, plan, checkpoint, effectiveContext, "cancelled", "Cancellation requested prior to execution.", null, null, null, cancellationToken).ConfigureAwait(false);
            return FictionPhaseResult.Cancelled(phase, "Cancellation requested.");
        }

        if (checkpoint.Status == FictionPlanCheckpointStatus.Cancelled && !IsMetadataFlagSet(effectiveContext, "resume"))
        {
            if (!string.IsNullOrEmpty(backlogItemId))
            {
                await SetBacklogStatusAsync(plan.Id, backlogItemId!, FictionPlanBacklogStatus.Pending, phase, effectiveContext, "cancelled-checkpoint", cancellationToken).ConfigureAwait(false);
            }

            UpdateConversationTaskStatus(conversationTask, "Cancelled", null, "Phase remains cancelled; skipping execution.");
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublishProgressAsync(phase, plan, checkpoint, effectiveContext, "cancelled", "Phase remains cancelled; skipping execution.", null, null, null, cancellationToken).ConfigureAwait(false);
            return FictionPhaseResult.Cancelled(phase, "Phase cancelled for this branch.");
        }

        if (checkpoint.Status == FictionPlanCheckpointStatus.Cancelled && IsMetadataFlagSet(effectiveContext, "resume"))
        {
            checkpoint.Status = FictionPlanCheckpointStatus.Pending;
            checkpoint.LockedByAgentId = null;
            checkpoint.LockedByConversationId = null;
            checkpoint.LockedAtUtc = null;
            checkpoint.Progress = BuildProgressSnapshot(phase, effectiveContext, "resumed", "Branch resumed.");
            checkpoint.UpdatedAtUtc = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(backlogItemId))
            {
                await SetBacklogStatusAsync(plan.Id, backlogItemId!, FictionPlanBacklogStatus.Pending, phase, effectiveContext, "resume", cancellationToken).ConfigureAwait(false);
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublishProgressAsync(phase, plan, checkpoint, effectiveContext, "resumed", "Branch resumed.", null, null, null, cancellationToken).ConfigureAwait(false);
        }

        checkpoint.Status = FictionPlanCheckpointStatus.InProgress;
        checkpoint.LockedByAgentId = effectiveContext.AgentId;
        checkpoint.LockedByConversationId = effectiveContext.ConversationId;
        checkpoint.LockedAtUtc = DateTime.UtcNow;
        checkpoint.CompletedCount ??= 0;
        checkpoint.TargetCount ??= 1;
        checkpoint.Progress = BuildProgressSnapshot(phase, effectiveContext, "started", "Phase execution started.");
        checkpoint.UpdatedAtUtc = DateTime.UtcNow;

        UpdateConversationTaskStatus(conversationTask, "Running");
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var startedAtUtc = DateTime.UtcNow;
        await PublishProgressAsync(phase, plan, checkpoint, effectiveContext, "started", "Phase execution started.", null, null, startedAtUtc, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(backlogItemId))
        {
            await SetBacklogStatusAsync(plan.Id, backlogItemId!, FictionPlanBacklogStatus.InProgress, phase, effectiveContext, "phase-start", cancellationToken).ConfigureAwait(false);
        }

        try
        {
            _logger.LogInformation("Running fiction phase {Phase} for plan {PlanId} (branch {Branch}).", phase, plan.Id, effectiveContext.BranchSlug);

            var result = await runner.RunAsync(effectiveContext, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(backlogItemId))
            {
                result = AnnotateBacklogResult(result, backlogItemId!);
            }

            if (phase == FictionPhase.VisionPlanner)
            {
                await ApplyVisionPlannerBacklogAsync(plan.Id, result, effectiveContext, cancellationToken).ConfigureAwait(false);
            }
            else if (!string.IsNullOrEmpty(backlogItemId))
            {
                var finalStatus = MapPhaseStatusToBacklog(result.Status);
                await SetBacklogStatusAsync(plan.Id, backlogItemId!, finalStatus, phase, effectiveContext, "phase-complete", cancellationToken).ConfigureAwait(false);
            }

            UpdateConversationTaskStatus(
                conversationTask,
                MapConversationTaskStatus(result.Status),
                result.Summary,
                result.Status == FictionPhaseStatus.Cancelled ? result.Summary : null);

            await PersistTranscriptsAsync(plan, checkpoint, effectiveContext, result, cancellationToken).ConfigureAwait(false);

            checkpoint.LockedByAgentId = null;
            checkpoint.LockedByConversationId = null;
            checkpoint.LockedAtUtc = null;
            checkpoint.UpdatedAtUtc = DateTime.UtcNow;

            var progressStatus = MapPhaseStatusToProgress(result.Status);
            checkpoint.Progress = BuildProgressSnapshot(phase, effectiveContext, progressStatus, result.Summary, result, null, startedAtUtc);

            switch (result.Status)
            {
                case FictionPhaseStatus.Completed:
                    checkpoint.Status = FictionPlanCheckpointStatus.Complete;
                    checkpoint.CompletedCount = checkpoint.TargetCount ?? 1;
                    break;
                case FictionPhaseStatus.Cancelled:
                    checkpoint.Status = FictionPlanCheckpointStatus.Cancelled;
                    break;
                default:
                    checkpoint.Status = FictionPlanCheckpointStatus.Pending;
                    break;
            }

            plan.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await PublishProgressAsync(phase, plan, checkpoint, effectiveContext, progressStatus, result.Summary, result, null, startedAtUtc, cancellationToken).ConfigureAwait(false);
            await _backlogScheduler.ScheduleAsync(plan, phase, result, effectiveContext, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Finished fiction phase {Phase} for plan {PlanId} with status {Status}.", phase, plan.Id, result.Status);
            return result;
        }
        catch (Exception ex)
        {
            var isCancellation = ex is OperationCanceledException && cancellationToken.IsCancellationRequested;
            var saveToken = isCancellation ? CancellationToken.None : cancellationToken;

            _logger.LogError(ex, "Phase {Phase} failed for plan {PlanId}.", phase, plan.Id);

            if (!string.IsNullOrEmpty(backlogItemId))
            {
                await SetBacklogStatusAsync(plan.Id, backlogItemId!, FictionPlanBacklogStatus.Pending, phase, effectiveContext, "phase-failed", saveToken).ConfigureAwait(false);
            }

            checkpoint.LockedByAgentId = null;
            checkpoint.LockedByConversationId = null;
            checkpoint.LockedAtUtc = null;
            checkpoint.UpdatedAtUtc = DateTime.UtcNow;

            var failureSummary = isCancellation ? "Phase execution cancelled." : ex.Message;
            var progressStatus = isCancellation ? "cancelled" : "failed";
            checkpoint.Progress = BuildProgressSnapshot(phase, effectiveContext, progressStatus, failureSummary, null, isCancellation ? null : ex, startedAtUtc);
            checkpoint.Status = isCancellation ? FictionPlanCheckpointStatus.Cancelled : FictionPlanCheckpointStatus.Pending;

            UpdateConversationTaskStatus(conversationTask, isCancellation ? "Cancelled" : "Failed", null, failureSummary);

            await _db.SaveChangesAsync(saveToken).ConfigureAwait(false);
            await PublishProgressAsync(phase, plan, checkpoint, effectiveContext, progressStatus, failureSummary, null, isCancellation ? null : ex, startedAtUtc, saveToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<ConversationTask?> FindConversationTaskAsync(FictionPhaseExecutionContext context, CancellationToken cancellationToken)
    {
        if (!TryReadMetadataGuid(context.Metadata, "taskId", out var taskId))
        {
            return null;
        }

        return await _db.ConversationTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken).ConfigureAwait(false);
    }

    private static void UpdateConversationTaskStatus(ConversationTask? task, string status, string? observation = null, string? error = null)
    {
        if (task is null)
        {
            return;
        }

        task.Status = status;

        if (observation is not null)
        {
            task.Observation = observation;
        }

        if (error is not null)
        {
            task.Error = error;
        }
    }

    private static string MapConversationTaskStatus(FictionPhaseStatus status) => status switch
    {
        FictionPhaseStatus.Completed => "Completed",
        FictionPhaseStatus.Cancelled => "Cancelled",
        FictionPhaseStatus.Blocked => "Blocked",
        _ => "Completed"
    };

    private static bool TryReadMetadataGuid(IReadOnlyDictionary<string, string>? metadata, string key, out Guid value)
    {
        value = Guid.Empty;
        if (metadata is null)
        {
            return false;
        }

        if (!metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return Guid.TryParse(raw, out value);
    }

    private static FictionPhaseExecutionContext NormalizeContext(FictionPhaseExecutionContext context, string? defaultBranch)
    {
        var branch = string.IsNullOrWhiteSpace(context.BranchSlug)
            ? (string.IsNullOrWhiteSpace(defaultBranch) ? "main" : defaultBranch)
            : context.BranchSlug;

        return branch == context.BranchSlug
            ? context
            : new FictionPhaseExecutionContext(
                context.PlanId,
                context.AgentId,
                context.ConversationId,
                branch,
                context.ChapterBlueprintId,
                context.ChapterScrollId,
                context.ChapterSceneId,
                context.IterationIndex,
                context.InvokedByJobId,
                context.Metadata);
    }

    private FictionPhaseExecutionContext AttachScopeContext(FictionPhaseExecutionContext context, FictionPlan plan)
    {
        var token = context.ScopeToken ?? new ScopeToken(
            TenantId: null,
            AppId: null,
            PersonaId: null,
            AgentId: context.AgentId,
            ConversationId: context.ConversationId,
            PlanId: plan.Id,
            ProjectId: plan.FictionProjectId,
            WorldId: null);

        ScopePath? scopePath = context.ScopePath;
        if (scopePath is null && _scopePaths.TryBuild(token, out var builtPath))
        {
            scopePath = builtPath;
        }

        var metadata = context.Metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(context.Metadata, StringComparer.OrdinalIgnoreCase);

        if (scopePath is not null)
        {
            metadata["scopePath"] = scopePath.Canonical;
            if (!scopePath.Principal.IsEmpty)
            {
                metadata["scopePrincipalType"] = scopePath.Principal.PrincipalType;
                metadata["scopePrincipalId"] = scopePath.Principal.RootId.ToString("D");
            }
        }

        return context with
        {
            ScopeToken = token,
            ScopePath = scopePath,
            Metadata = metadata
        };
    }

    private static bool IsMetadataFlagSet(FictionPhaseExecutionContext context, string key)
    {
        if (context.Metadata is null)
        {
            return false;
        }

        if (!context.Metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return raw.Equals("true", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("1", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetBacklogItemId(FictionPhaseExecutionContext context)
    {
        if (context.Metadata is null)
        {
            return null;
        }

        return context.Metadata.TryGetValue("backlogItemId", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private void EnsureExecutionMetadata(
        FictionPhase phase,
        FictionPhaseExecutionContext context,
        string? backlogItemId)
    {
        var metadata = context.Metadata;
        if (metadata is null)
        {
            ThrowMetadataException(phase, context.PlanId, "metadata payload");
            return;
        }

        if (!metadata.ContainsKey("conversationPlanId"))
        {
            ThrowMetadataException(phase, context.PlanId, "conversationPlanId");
        }

        if (!metadata.ContainsKey("providerId"))
        {
            ThrowMetadataException(phase, context.PlanId, "providerId");
        }

        if (!metadata.ContainsKey("modelId"))
        {
            ThrowMetadataException(phase, context.PlanId, "modelId");
        }

        if (!string.IsNullOrWhiteSpace(backlogItemId) && !metadata.ContainsKey("taskId"))
        {
            ThrowMetadataException(phase, context.PlanId, $"taskId for backlog item {backlogItemId}");
        }
    }

    private void ThrowMetadataException(FictionPhase phase, Guid planId, string missingField)
    {
        var message = $"Cannot execute phase {phase} for plan {planId}: required metadata '{missingField}' is missing.";
        _logger.LogError(message);
        throw new InvalidOperationException(message);
    }

    private static Guid? GetWorldBibleId(FictionPhaseExecutionContext context)
    {
        if (context.Metadata is null)
        {
            return null;
        }

        if (context.Metadata.TryGetValue("worldBibleId", out var raw) && Guid.TryParse(raw, out var id))
        {
            return id;
        }

        return null;
    }

    private async Task ApplyVisionPlannerBacklogAsync(Guid planId, FictionPhaseResult result, FictionPhaseExecutionContext context, CancellationToken cancellationToken)
    {
        var backlog = ExtractPlannerBacklog(result);
        await UpsertBacklogAsync(planId, FictionPhase.VisionPlanner, context, backlog, cancellationToken).ConfigureAwait(false);
    }

    private async Task UpsertBacklogAsync(Guid planId, FictionPhase phase, FictionPhaseExecutionContext context, IReadOnlyList<PlannerBacklogItem> backlog, CancellationToken cancellationToken)
    {
        var existing = await _db.FictionPlanBacklogItems
            .Where(x => x.FictionPlanId == planId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var incomingIds = new HashSet<string>(backlog.Select(b => b.Id), StringComparer.OrdinalIgnoreCase);
        var conversationPlanId = await GetConversationPlanIdAsync(planId, cancellationToken).ConfigureAwait(false);

        foreach (var entity in existing.ToList())
        {
            if (!incomingIds.Contains(entity.BacklogId))
            {
                _db.FictionPlanBacklogItems.Remove(entity);
            }
        }

        foreach (var item in backlog)
        {
            var existingItem = existing.FirstOrDefault(x => string.Equals(x.BacklogId, item.Id, StringComparison.OrdinalIgnoreCase));
            var previousStatus = existingItem?.Status;
            if (existingItem is null)
            {
                existingItem = new FictionPlanBacklogItem
                {
                    Id = Guid.NewGuid(),
                    FictionPlanId = planId,
                    BacklogId = item.Id,
                    CreatedAtUtc = now
                };
                _db.FictionPlanBacklogItems.Add(existingItem);
                existing.Add(existingItem);
            }

            existingItem.Description = item.Description;
            existingItem.Status = MapPlannerStatus(item.Status);
            existingItem.Inputs = item.Inputs.Count == 0
                ? null
                : item.Inputs.Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            existingItem.Outputs = item.Outputs.Count == 0
                ? null
                : item.Outputs.Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            existingItem.UpdatedAtUtc = now;

            switch (existingItem.Status)
            {
                case FictionPlanBacklogStatus.InProgress:
                    existingItem.InProgressAtUtc ??= now;
                    existingItem.CompletedAtUtc = null;
                    break;
                case FictionPlanBacklogStatus.Complete:
                    existingItem.InProgressAtUtc ??= now;
                    existingItem.CompletedAtUtc ??= now;
                    break;
                default:
                    existingItem.InProgressAtUtc = null;
                    existingItem.CompletedAtUtc = null;
                    break;
            }

            if (previousStatus is null || previousStatus != existingItem.Status)
            {
                await RecordBacklogTelemetry(planId, existingItem.BacklogId, phase, existingItem.Status, context, "vision-sync", previousStatus, cancellationToken).ConfigureAwait(false);
            }

            if (conversationPlanId.HasValue)
            {
                await EnsureConversationTaskForBacklogAsync(conversationPlanId, planId, existingItem, phase, cancellationToken).ConfigureAwait(false);
            }
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SetBacklogStatusAsync(
        Guid planId,
        string backlogId,
        FictionPlanBacklogStatus status,
        FictionPhase phase,
        FictionPhaseExecutionContext context,
        string reason,
        CancellationToken cancellationToken)
    {
        var entity = (await _db.FictionPlanBacklogItems
            .Where(x => x.FictionPlanId == planId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .FirstOrDefault(x => string.Equals(x.BacklogId, backlogId, StringComparison.OrdinalIgnoreCase));

        if (entity is null)
        {
            _logger.LogWarning("Backlog item {BacklogId} was not found for plan {PlanId}.", backlogId, planId);
            return;
        }

        var previousStatus = entity.Status;
        var now = DateTime.UtcNow;
        if (entity.Status != status || status == FictionPlanBacklogStatus.InProgress)
        {
            entity.Status = status;
        }

        switch (status)
        {
            case FictionPlanBacklogStatus.InProgress:
                entity.InProgressAtUtc = now;
                entity.CompletedAtUtc = null;
                break;
            case FictionPlanBacklogStatus.Complete:
                entity.InProgressAtUtc ??= now;
                entity.CompletedAtUtc = now;
                break;
            default:
                entity.InProgressAtUtc = null;
                entity.CompletedAtUtc = null;
                break;
        }

        entity.UpdatedAtUtc = now;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var conversationPlanId = await GetConversationPlanIdAsync(planId, cancellationToken).ConfigureAwait(false);

        await RecordBacklogTelemetry(planId, backlogId, phase, status, context, reason, previousStatus, cancellationToken).ConfigureAwait(false);
        await UpdateConversationTaskStatusAsync(conversationPlanId, planId, backlogId, status, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Guid?> GetConversationPlanIdAsync(Guid planId, CancellationToken cancellationToken)
    {
        return await _db.FictionPlans
            .Where(p => p.Id == planId)
            .Select(p => p.CurrentConversationPlanId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Guid?> EnsureConversationTaskForBacklogAsync(
        Guid? conversationPlanId,
        Guid planId,
        FictionPlanBacklogItem backlogItem,
        FictionPhase producingPhase,
        CancellationToken cancellationToken)
    {
        if (!conversationPlanId.HasValue || string.IsNullOrWhiteSpace(backlogItem.BacklogId))
        {
            return null;
        }

        var backlogId = backlogItem.BacklogId;
        var task = await _db.ConversationTasks
            .FirstOrDefaultAsync(t => t.ConversationPlanId == conversationPlanId.Value && t.BacklogItemId == backlogId, cancellationToken)
            .ConfigureAwait(false);

        if (task is not null)
        {
            task.Status = MapBacklogStatusToTaskStatus(backlogItem.Status);
            return task.Id;
        }

        var goal = string.IsNullOrWhiteSpace(backlogItem.Description) ? backlogId : backlogItem.Description!;
        var phase = FictionBacklogPhaseResolver.ResolvePhase(backlogItem) ?? producingPhase;
        var toolName = MapPhaseToTool(phase);
        var argsJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            planId,
            backlogItemId = backlogId
        }, MetadataSerializerOptions);

        var nextStep = await _db.ConversationTasks
            .Where(t => t.ConversationPlanId == conversationPlanId.Value)
            .Select(t => (int?)t.StepNumber)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);

        var conversationTask = new ConversationTask
        {
            Id = Guid.NewGuid(),
            ConversationPlanId = conversationPlanId.Value,
            StepNumber = (nextStep ?? 0) + 1,
            Thought = goal,
            Goal = goal,
            ToolName = toolName,
            ArgsJson = argsJson,
            Status = MapBacklogStatusToTaskStatus(backlogItem.Status),
            CreatedAt = DateTime.UtcNow,
            BacklogItemId = backlogId
        };

        _db.ConversationTasks.Add(conversationTask);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return conversationTask.Id;
    }

    private async Task UpdateConversationTaskStatusAsync(
        Guid? conversationPlanId,
        Guid planId,
        string backlogId,
        FictionPlanBacklogStatus status,
        CancellationToken cancellationToken)
    {
        if (!conversationPlanId.HasValue || string.IsNullOrWhiteSpace(backlogId))
        {
            return;
        }

        var task = await _db.ConversationTasks
            .FirstOrDefaultAsync(t => t.ConversationPlanId == conversationPlanId.Value && t.BacklogItemId == backlogId, cancellationToken)
            .ConfigureAwait(false);

        if (task is null)
        {
            return;
        }

        task.Status = MapBacklogStatusToTaskStatus(status);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static FictionPlanBacklogStatus MapPlannerStatus(PlannerBacklogStatus status) => status switch
    {
        PlannerBacklogStatus.InProgress => FictionPlanBacklogStatus.InProgress,
        PlannerBacklogStatus.Complete => FictionPlanBacklogStatus.Complete,
        _ => FictionPlanBacklogStatus.Pending
    };

    private static FictionPlanBacklogStatus MapPhaseStatusToBacklog(FictionPhaseStatus status) => status switch
    {
        FictionPhaseStatus.Completed => FictionPlanBacklogStatus.Complete,
        _ => FictionPlanBacklogStatus.Pending
    };

    private static string MapBacklogStatusToTaskStatus(FictionPlanBacklogStatus status) => status switch
    {
        FictionPlanBacklogStatus.InProgress => "Running",
        FictionPlanBacklogStatus.Complete => "Completed",
        _ => "Pending"
    };

    private static string MapPhaseToTool(FictionPhase phase) => phase switch
    {
        FictionPhase.VisionPlanner => "fiction.weaver.visionPlanner",
        FictionPhase.WorldBibleManager => "fiction.weaver.worldBibleManager",
        FictionPhase.IterativePlanner => "fiction.weaver.iterativePlanner",
        FictionPhase.ChapterArchitect => "fiction.weaver.chapterArchitect",
        FictionPhase.ScrollRefiner => "fiction.weaver.scrollRefiner",
        FictionPhase.SceneWeaver => "fiction.weaver.sceneWeaver",
        _ => string.Empty
    };

    private static IReadOnlyList<PlannerBacklogItem> ExtractPlannerBacklog(FictionPhaseResult result)
    {
        if (result.Data is null || !result.Data.TryGetValue("backlog", out var backlogObj) || backlogObj is null)
        {
            return Array.Empty<PlannerBacklogItem>();
        }

        try
        {
            var json = JsonConvert.SerializeObject(backlogObj);
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<PlannerBacklogItem>();
            }

            var contracts = JsonConvert.DeserializeObject<List<PlannerBacklogContract>>(json);
            if (contracts is null)
            {
                return Array.Empty<PlannerBacklogItem>();
            }

            var list = new List<PlannerBacklogItem>(contracts.Count);
            foreach (var contract in contracts)
            {
                if (string.IsNullOrWhiteSpace(contract.Id))
                {
                    continue;
                }

                var description = string.IsNullOrWhiteSpace(contract.Description) ? contract.Id : contract.Description!;
                var status = PlannerBacklogStatusExtensions.Parse(contract.Status);
                var inputs = contract.Inputs?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()).ToArray() ?? Array.Empty<string>();
                var outputs = contract.Outputs?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim()).ToArray() ?? Array.Empty<string>();
                list.Add(new PlannerBacklogItem(contract.Id!, description, status, inputs, outputs));
            }

            return list;
        }
        catch (Newtonsoft.Json.JsonException)
        {
            return Array.Empty<PlannerBacklogItem>();
        }
    }

    private sealed record PlannerBacklogContract
    {
        public string? Id { get; init; }
        public string? Description { get; init; }
        public string? Status { get; init; }
        public string[]? Inputs { get; init; }
        public string[]? Outputs { get; init; }
    }

    private async Task RecordBacklogTelemetry(
        Guid planId,
        string backlogId,
        FictionPhase phase,
        FictionPlanBacklogStatus status,
        FictionPhaseExecutionContext context,
        string reason,
        FictionPlanBacklogStatus? previousStatus,
        CancellationToken cancellationToken)
    {
        var branch = context.BranchSlug ?? "main";
        _logger.LogInformation(
            "BacklogTelemetry plan={PlanId} backlog={BacklogId} phase={Phase} status={Status} previousStatus={PreviousStatus} reason={Reason} branch={Branch} iteration={Iteration} blueprint={BlueprintId} scroll={ScrollId} scene={SceneId}",
            planId,
            backlogId,
            phase.ToString(),
            status.ToString(),
            previousStatus?.ToString() ?? "unknown",
            reason,
            branch,
            context.IterationIndex,
            context.ChapterBlueprintId,
            context.ChapterScrollId,
            context.ChapterSceneId);

        var hasCharacterEntity = _db.Model.FindEntityType(typeof(FictionCharacter)) is not null;
        var hasLoreEntity = _db.Model.FindEntityType(typeof(FictionLoreRequirement)) is not null;

        if (!hasCharacterEntity && !hasLoreEntity)
        {
            return;
        }

        var characterMetrics = new CharacterMetrics(0, 0, 0);
        List<CharacterSnapshot> recentCharacters = new();
        if (hasCharacterEntity)
        {
            characterMetrics = await _db.FictionCharacters
                .AsNoTracking()
                .Where(c => c.FictionPlanId == planId)
                .GroupBy(_ => 1)
                .Select(g => new CharacterMetrics(
                    g.Count(),
                    g.Count(c => c.PersonaId != null),
                    g.Count(c => c.WorldBibleEntryId != null)))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false) ?? new CharacterMetrics(0, 0, 0);

            recentCharacters = await _db.FictionCharacters
                .AsNoTracking()
                .Where(c => c.FictionPlanId == planId)
                .OrderByDescending(c => c.UpdatedAtUtc ?? c.CreatedAtUtc)
                .Take(5)
                .Select(c => new CharacterSnapshot(
                    c.Id,
                    c.Slug,
                    c.DisplayName,
                    c.PersonaId,
                    c.WorldBibleEntryId,
                    c.Role,
                    c.Importance,
                    c.UpdatedAtUtc ?? c.CreatedAtUtc,
                    c.ProvenanceJson))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var loreMetrics = new LoreMetrics(0, 0, 0);
        List<LoreSnapshot> pendingLore = new();
        if (hasLoreEntity)
        {
            loreMetrics = await _db.FictionLoreRequirements
                .AsNoTracking()
                .Where(r => r.FictionPlanId == planId)
                .GroupBy(_ => 1)
                .Select(g => new LoreMetrics(
                    g.Count(),
                    g.Count(r => r.Status == FictionLoreRequirementStatus.Ready),
                    g.Count(r => r.Status == FictionLoreRequirementStatus.Blocked)))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false) ?? new LoreMetrics(0, 0, 0);

            pendingLore = await _db.FictionLoreRequirements
                .AsNoTracking()
                .Where(r => r.FictionPlanId == planId && r.Status != FictionLoreRequirementStatus.Ready)
                .OrderBy(r => r.UpdatedAtUtc ?? r.CreatedAtUtc)
                .Take(5)
                .Select(r => new LoreSnapshot(
                    r.Id,
                    r.RequirementSlug,
                    r.Title,
                    r.Status.ToString(),
                    r.WorldBibleEntryId,
                    r.MetadataJson,
                    r.UpdatedAtUtc ?? r.CreatedAtUtc))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var payload = new JObject
        {
            ["planId"] = planId,
            ["backlogId"] = backlogId,
            ["phase"] = phase.ToString(),
            ["status"] = status.ToString(),
            ["previousStatus"] = previousStatus?.ToString(),
            ["reason"] = reason,
            ["branch"] = branch,
            ["iteration"] = context.IterationIndex,
            ["chapterBlueprintId"] = context.ChapterBlueprintId,
            ["chapterScrollId"] = context.ChapterScrollId,
            ["chapterSceneId"] = context.ChapterSceneId,
            ["characterMetrics"] = new JObject
            {
                ["total"] = characterMetrics.Total,
                ["personaLinked"] = characterMetrics.PersonaLinked,
                ["worldBibleLinked"] = characterMetrics.WorldBibleLinked
            },
            ["loreMetrics"] = new JObject
            {
                ["total"] = loreMetrics.Total,
                ["ready"] = loreMetrics.Ready,
                ["blocked"] = loreMetrics.Blocked
            }
        };

        if (context.Metadata is not null && context.Metadata.Count > 0)
        {
            payload["metadata"] = JObject.FromObject(context.Metadata);
        }

        if (recentCharacters.Count > 0)
        {
            var characterArray = new JArray();
            foreach (var snapshot in recentCharacters)
            {
                var item = new JObject
                {
                    ["id"] = snapshot.Id,
                    ["slug"] = snapshot.Slug,
                    ["displayName"] = snapshot.DisplayName,
                    ["personaId"] = snapshot.PersonaId,
                    ["worldBibleEntryId"] = snapshot.WorldBibleEntryId,
                    ["role"] = snapshot.Role ?? string.Empty,
                    ["importance"] = snapshot.Importance ?? string.Empty,
                    ["updatedAtUtc"] = snapshot.UpdatedAtUtc
                };
                var provenance = TryParseJToken(snapshot.ProvenanceJson);
                if (provenance is not null)
                {
                    item["provenance"] = provenance;
                }

                characterArray.Add(item);
            }

            payload["recentCharacters"] = characterArray;
        }

        if (pendingLore.Count > 0)
        {
            var loreArray = new JArray();
            foreach (var snapshot in pendingLore)
            {
                var item = new JObject
                {
                    ["id"] = snapshot.Id,
                    ["slug"] = snapshot.RequirementSlug,
                    ["title"] = snapshot.Title,
                    ["status"] = snapshot.Status,
                    ["worldBibleEntryId"] = snapshot.WorldBibleEntryId,
                    ["updatedAtUtc"] = snapshot.UpdatedAtUtc
                };
                var metadataToken = TryParseJToken(snapshot.MetadataJson);
                if (metadataToken is not null)
                {
                    item["metadata"] = metadataToken;
                }

                loreArray.Add(item);
            }

            payload["pendingLore"] = loreArray;
        }

        await _workflowLogger.LogAsync(
            context.ConversationId,
            "fiction.backlog.telemetry",
            payload).ConfigureAwait(false);
    }

    private static JToken? TryParseJToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return JToken.Parse(raw);
        }
        catch (JsonReaderException)
        {
            return null;
        }
    }

    private sealed record CharacterMetrics(int Total, int PersonaLinked, int WorldBibleLinked);

    private sealed record LoreMetrics(int Total, int Ready, int Blocked);

    private sealed record CharacterSnapshot(Guid Id, string Slug, string DisplayName, Guid? PersonaId, Guid? WorldBibleEntryId, string? Role, string? Importance, DateTime UpdatedAtUtc, string? ProvenanceJson);

    private sealed record LoreSnapshot(Guid Id, string RequirementSlug, string Title, string Status, Guid? WorldBibleEntryId, string? MetadataJson, DateTime UpdatedAtUtc);

    private static FictionPhaseResult AnnotateBacklogResult(FictionPhaseResult result, string backlogItemId)
    {
        if (string.IsNullOrEmpty(backlogItemId))
        {
            return result;
        }

        var data = result.Data is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(result.Data, StringComparer.OrdinalIgnoreCase);

        if (!data.ContainsKey("backlogItemId"))
        {
            data["backlogItemId"] = backlogItemId;
            result = result with { Data = data };
        }

        if (result.Transcripts is not null && result.Transcripts.Count > 0)
        {
            var transcripts = new List<FictionPhaseTranscript>(result.Transcripts.Count);
            foreach (var transcript in result.Transcripts)
            {
                var metadata = transcript.Metadata is null
                    ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, object?>(transcript.Metadata, StringComparer.OrdinalIgnoreCase);
                metadata["backlogItemId"] = backlogItemId;
                transcripts.Add(transcript with { Metadata = metadata });
            }

            result = result with { Transcripts = transcripts };
        }

        return result;
    }

    private async Task PublishProgressAsync(
        FictionPhase phase,
        FictionPlan plan,
        FictionPlanCheckpoint checkpoint,
        FictionPhaseExecutionContext context,
        string status,
        string? summary,
        FictionPhaseResult? result,
        Exception? exception,
        DateTime? startedAtUtc,
        CancellationToken cancellationToken)
    {
        var snapshot = BuildProgressSnapshot(phase, context, status, summary, result, exception, startedAtUtc);
        snapshot["planId"] = plan.Id;
        snapshot["checkpointId"] = checkpoint.Id;
        snapshot["checkpointStatus"] = checkpoint.Status.ToString();
        snapshot["jobId"] = context.InvokedByJobId;
        var backlogItemId = snapshot.TryGetValue("backlogItemId", out var backlogObj) ? backlogObj?.ToString() : null;

        var payload = snapshot.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var evt = new FictionPhaseProgressed(plan.Id, context.ConversationId, context.AgentId, context.BranchSlug ?? "main", phase.ToString(), status, summary, payload, backlogItemId);
        await _bus.Publish(evt).ConfigureAwait(false);

        await _workflowLogger.LogAsync(context.ConversationId, nameof(FictionPhaseProgressed), JObject.FromObject(new
        {
            evt.PlanId,
            evt.ConversationId,
            evt.AgentId,
            evt.BranchSlug,
            evt.Phase,
            evt.Status,
            evt.Summary,
            evt.Payload,
            evt.BacklogItemId
        })).ConfigureAwait(false);

        var signalPayload = new
        {
            planId = plan.Id,
            conversationId = context.ConversationId,
            agentId = context.AgentId,
            branchSlug = context.BranchSlug,
            phase = phase.ToString(),
            status,
            summary,
            checkpointId = checkpoint.Id,
            checkpointStatus = checkpoint.Status.ToString(),
            backlogItemId,
            payload,
            timestampUtc = DateTime.UtcNow
        };
        await _planNotifier.NotifyPlanProgressAsync(context.ConversationId, signalPayload).ConfigureAwait(false);
    }

    private static void CancelCheckpoint(FictionPhase phase, FictionPlanCheckpoint checkpoint, FictionPhaseExecutionContext context)
    {
        checkpoint.Status = FictionPlanCheckpointStatus.Cancelled;
        checkpoint.LockedByAgentId = null;
        checkpoint.LockedByConversationId = null;
        checkpoint.LockedAtUtc = null;
        checkpoint.Progress = BuildProgressSnapshot(phase, context, "cancelled", "Branch cancelled before execution.");
        checkpoint.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task PersistTranscriptsAsync(
        FictionPlan plan,
        FictionPlanCheckpoint checkpoint,
        FictionPhaseExecutionContext context,
        FictionPhaseResult result,
        CancellationToken cancellationToken)
    {
        if (result.Transcripts is null || result.Transcripts.Count == 0)
        {
            return;
        }

        foreach (var entry in result.Transcripts)
        {
            var metadata = entry.Metadata is null
                ? new Dictionary<string, object?>()
                : entry.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            metadata["checkpointId"] = checkpoint.Id;
            metadata["branchSlug"] = context.BranchSlug;
            metadata["phaseKey"] = checkpoint.Phase;
            if (entry.ChapterScrollId.HasValue)
            {
                metadata["chapterScrollId"] = entry.ChapterScrollId.Value;
            }
            if (context.Metadata is not null)
            {
                if (context.Metadata.TryGetValue("scopePath", out var metadataScopePath) && !string.IsNullOrWhiteSpace(metadataScopePath))
                {
                    metadata["scopePath"] = metadataScopePath;
                }

                if (context.Metadata.TryGetValue("scopePrincipalType", out var principalType) && !string.IsNullOrWhiteSpace(principalType))
                {
                    metadata["scopePrincipalType"] = principalType;
                }

                if (context.Metadata.TryGetValue("scopePrincipalId", out var principalId) && !string.IsNullOrWhiteSpace(principalId))
                {
                    metadata["scopePrincipalId"] = principalId;
                }
            }

            var transcript = new FictionPlanTranscript
            {
                Id = Guid.NewGuid(),
                FictionPlanId = plan.Id,
                Phase = checkpoint.Phase,
                FictionChapterBlueprintId = entry.ChapterBlueprintId ?? context.ChapterBlueprintId,
                FictionChapterSceneId = entry.ChapterSceneId ?? context.ChapterSceneId,
                AgentId = entry.AgentId ?? context.AgentId,
                ConversationId = entry.ConversationId ?? context.ConversationId,
                ConversationMessageId = entry.ConversationMessageId,
                Attempt = entry.Attempt <= 0 ? 1 : entry.Attempt,
                RequestPayload = entry.RequestPayload,
                ResponsePayload = entry.ResponsePayload,
                PromptTokens = entry.PromptTokens,
                CompletionTokens = entry.CompletionTokens,
                LatencyMs = entry.LatencyMs,
                ValidationStatus = entry.ValidationStatus,
                ValidationDetails = entry.ValidationDetails,
                IsRetry = entry.IsRetry,
                Metadata = metadata,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _db.Set<FictionPlanTranscript>().Add(transcript);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<FictionPlanCheckpoint> GetOrCreateCheckpointAsync(Guid planId, string phaseKey, CancellationToken cancellationToken)
    {
        var checkpoint = await _db.Set<FictionPlanCheckpoint>()
            .FirstOrDefaultAsync(cp => cp.FictionPlanId == planId && cp.Phase == phaseKey, cancellationToken)
            .ConfigureAwait(false);

        if (checkpoint is not null)
        {
            return checkpoint;
        }

        checkpoint = new FictionPlanCheckpoint
        {
            Id = Guid.NewGuid(),
            FictionPlanId = planId,
            Phase = phaseKey,
            Status = FictionPlanCheckpointStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Set<FictionPlanCheckpoint>().Add(checkpoint);
        return checkpoint;
    }

    private static string BuildPhaseKey(FictionPhase phase, FictionPhaseExecutionContext context)
    {
        var parts = new List<string>
        {
            phase.ToString(),
            context.BranchSlug ?? "main"
        };

        if (context.IterationIndex is int iteration)
        {
            parts.Add($"pass:{iteration}");
        }

        if (context.ChapterBlueprintId is Guid blueprintId)
        {
            parts.Add($"blueprint:{blueprintId:N}");
        }

        if (context.ChapterScrollId is Guid scrollId)
        {
            parts.Add($"scroll:{scrollId:N}");
        }

        if (context.ChapterSceneId is Guid sceneId)
        {
            parts.Add($"scene:{sceneId:N}");
        }

        return string.Join("|", parts);
    }

    private static Dictionary<string, object?> BuildProgressSnapshot(
        FictionPhase phase,
        FictionPhaseExecutionContext context,
        string status,
        string? summary = null,
        FictionPhaseResult? result = null,
        Exception? exception = null,
        DateTime? startedAtUtc = null)
    {
        var snapshot = new Dictionary<string, object?>
        {
            ["phase"] = phase.ToString(),
            ["branch"] = context.BranchSlug,
            ["status"] = status,
            ["summary"] = summary,
            ["timestampUtc"] = DateTime.UtcNow,
            ["agentId"] = context.AgentId,
            ["conversationId"] = context.ConversationId,
        };

        if (context.Metadata is not null)
        {
            if (context.Metadata.TryGetValue("scopePath", out var metadataScopePath) && !string.IsNullOrWhiteSpace(metadataScopePath))
            {
                snapshot["scopePath"] = metadataScopePath;
            }

            if (context.Metadata.TryGetValue("scopePrincipalType", out var principalType) && !string.IsNullOrWhiteSpace(principalType))
            {
                snapshot["scopePrincipalType"] = principalType;
            }

            if (context.Metadata.TryGetValue("scopePrincipalId", out var principalId) && !string.IsNullOrWhiteSpace(principalId))
            {
                snapshot["scopePrincipalId"] = principalId;
            }
        }

        var backlogItemId = GetBacklogItemId(context);
        if (!string.IsNullOrEmpty(backlogItemId))
        {
            snapshot["backlogItemId"] = backlogItemId;
        }

        var worldBibleId = GetWorldBibleId(context);
        if (worldBibleId.HasValue)
        {
            snapshot["worldBibleId"] = worldBibleId.Value;
        }

        if (startedAtUtc.HasValue)
        {
            snapshot["startedAtUtc"] = startedAtUtc.Value;
        }

        if (context.IterationIndex is int iteration)
        {
            snapshot["iterationIndex"] = iteration;
        }

        if (context.ChapterBlueprintId is Guid blueprintId)
        {
            snapshot["chapterBlueprintId"] = blueprintId;
        }

        if (context.ChapterScrollId is Guid scrollId)
        {
            snapshot["chapterScrollId"] = scrollId;
        }

        if (context.ChapterSceneId is Guid sceneId)
        {
            snapshot["chapterSceneId"] = sceneId;
        }

        if (result is not null)
        {
            snapshot["phaseStatus"] = result.Status.ToString();
            if (result.Data is not null)
            {
                snapshot["resultData"] = result.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            if (result.Exception is not null)
            {
                snapshot["resultException"] = result.Exception.Message;
            }
        }

        if (exception is not null)
        {
            snapshot["exception"] = exception.Message;
        }

        return snapshot;
    }

    private static string MapPhaseStatusToProgress(FictionPhaseStatus status) => status switch
    {
        FictionPhaseStatus.Completed => "completed",
        FictionPhaseStatus.Skipped => "skipped",
        FictionPhaseStatus.Pending => "pending",
        FictionPhaseStatus.NotImplemented => "not-implemented",
        FictionPhaseStatus.Blocked => "blocked",
        FictionPhaseStatus.Cancelled => "cancelled",
        FictionPhaseStatus.Failed => "failed",
        _ => status.ToString().ToLowerInvariant()
    };
}
