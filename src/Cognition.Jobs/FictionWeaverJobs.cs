using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Contracts.Events;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Rebus.Bus;

namespace Cognition.Jobs;

public class FictionWeaverJobs
{
    private readonly CognitionDbContext _db;
    private readonly ILogger<FictionWeaverJobs> _logger;
    private readonly IReadOnlyDictionary<FictionPhase, IFictionPhaseRunner> _runnerLookup;
    private readonly IBus _bus;
    private readonly SignalRNotifier _notifier;
    private readonly WorkflowEventLogger _workflowLogger;

    public FictionWeaverJobs(
        CognitionDbContext db,
        IEnumerable<IFictionPhaseRunner> runners,
        IBus bus,
        SignalRNotifier notifier,
        WorkflowEventLogger workflowLogger,
        ILogger<FictionWeaverJobs> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runnerLookup = runners?.ToDictionary(r => r.Phase) ?? throw new ArgumentNullException(nameof(runners));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _workflowLogger = workflowLogger ?? throw new ArgumentNullException(nameof(workflowLogger));
    }

    public Task<FictionPhaseResult> RunVisionPlannerAsync(Guid planId, Guid agentId, Guid conversationId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => ExecutePhaseAsync(FictionPhase.VisionPlanner, CreateContext(planId, agentId, conversationId, branchSlug, providerId, modelId, metadata), cancellationToken);

    public Task<FictionPhaseResult> RunWorldBibleManagerAsync(Guid planId, Guid agentId, Guid conversationId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => ExecutePhaseAsync(FictionPhase.WorldBibleManager, CreateContext(planId, agentId, conversationId, branchSlug, providerId, modelId, metadata), cancellationToken);

    public Task<FictionPhaseResult> RunIterativePlannerAsync(Guid planId, Guid agentId, Guid conversationId, int iterationIndex, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => ExecutePhaseAsync(FictionPhase.IterativePlanner, CreateContext(planId, agentId, conversationId, branchSlug, providerId, modelId, metadata, iterationIndex: iterationIndex), cancellationToken);

    public Task<FictionPhaseResult> RunChapterArchitectAsync(Guid planId, Guid agentId, Guid conversationId, Guid chapterBlueprintId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => ExecutePhaseAsync(FictionPhase.ChapterArchitect, CreateContext(planId, agentId, conversationId, branchSlug, providerId, modelId, metadata, chapterBlueprintId: chapterBlueprintId), cancellationToken);

    public Task<FictionPhaseResult> RunScrollRefinerAsync(Guid planId, Guid agentId, Guid conversationId, Guid chapterScrollId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => ExecutePhaseAsync(FictionPhase.ScrollRefiner, CreateContext(planId, agentId, conversationId, branchSlug, providerId, modelId, metadata, chapterScrollId: chapterScrollId), cancellationToken);

    public Task<FictionPhaseResult> RunSceneWeaverAsync(Guid planId, Guid agentId, Guid conversationId, Guid chapterSceneId, Guid providerId, Guid? modelId = null, string branchSlug = "main", IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => ExecutePhaseAsync(FictionPhase.SceneWeaver, CreateContext(planId, agentId, conversationId, branchSlug, providerId, modelId, metadata, chapterSceneId: chapterSceneId), cancellationToken);

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

        var plan = await _db.Set<FictionPlan>().FirstOrDefaultAsync(p => p.Id == context.PlanId, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            throw new InvalidOperationException($"Fiction plan {context.PlanId} was not found.");
        }

        var effectiveContext = NormalizeContext(context, plan.PrimaryBranchSlug);

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
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublishProgressAsync(phase, plan, checkpoint, effectiveContext, "cancelled", "Cancellation requested prior to execution.", null, null, null, cancellationToken).ConfigureAwait(false);
            return FictionPhaseResult.Cancelled(phase, "Cancellation requested.");
        }

        if (checkpoint.Status == FictionPlanCheckpointStatus.Cancelled && !IsMetadataFlagSet(effectiveContext, "resume"))
        {
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

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var startedAtUtc = DateTime.UtcNow;
        await PublishProgressAsync(phase, plan, checkpoint, effectiveContext, "started", "Phase execution started.", null, null, startedAtUtc, cancellationToken).ConfigureAwait(false);

        try
        {
            _logger.LogInformation("Running fiction phase {Phase} for plan {PlanId} (branch {Branch}).", phase, plan.Id, effectiveContext.BranchSlug);

            var result = await runner.RunAsync(effectiveContext, cancellationToken).ConfigureAwait(false);

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
                case FictionPhaseStatus.Skipped:
                case FictionPhaseStatus.Pending:
                case FictionPhaseStatus.NotImplemented:
                    checkpoint.Status = FictionPlanCheckpointStatus.Pending;
                    break;
                case FictionPhaseStatus.Cancelled:
                    checkpoint.Status = FictionPlanCheckpointStatus.Cancelled;
                    break;
                case FictionPhaseStatus.Blocked:
                case FictionPhaseStatus.Failed:
                    checkpoint.Status = FictionPlanCheckpointStatus.Pending;
                    break;
            }

            plan.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await PublishProgressAsync(phase, plan, checkpoint, effectiveContext, progressStatus, result.Summary, result, null, startedAtUtc, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Finished fiction phase {Phase} for plan {PlanId} with status {Status}.", phase, plan.Id, result.Status);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Phase {Phase} failed for plan {PlanId}.", phase, plan.Id);

            checkpoint.Status = FictionPlanCheckpointStatus.Pending;
            checkpoint.LockedByAgentId = null;
            checkpoint.LockedByConversationId = null;
            checkpoint.LockedAtUtc = null;
            checkpoint.Progress = BuildProgressSnapshot(phase, effectiveContext, "failed", ex.Message, null, ex, startedAtUtc);
            checkpoint.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublishProgressAsync(phase, plan, checkpoint, effectiveContext, "failed", ex.Message, null, ex, startedAtUtc, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private static FictionPhaseExecutionContext NormalizeContext(FictionPhaseExecutionContext context, string? defaultBranch)
    {
        var branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? (string.IsNullOrWhiteSpace(defaultBranch) ? "main" : defaultBranch) : context.BranchSlug;
        if (branch == context.BranchSlug)
        {
            return context;
        }

        return new FictionPhaseExecutionContext(
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

    private static bool IsMetadataFlagSet(FictionPhaseExecutionContext context, string key)
    {
        if (context.Metadata is null) return false;
        if (!context.Metadata.TryGetValue(key, out var raw) || raw is null) return false;
        return raw.Equals("true", StringComparison.OrdinalIgnoreCase) || raw.Equals("1") || raw.Equals("yes", StringComparison.OrdinalIgnoreCase);
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

        var payload = snapshot.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var evt = new FictionPhaseProgressed(plan.Id, context.ConversationId, context.AgentId, context.BranchSlug ?? "main", phase.ToString(), status, summary, payload);
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
            evt.Payload
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
            payload,
            timestampUtc = DateTime.UtcNow
        };
        await _notifier.NotifyPlanProgressAsync(context.ConversationId, signalPayload).ConfigureAwait(false);
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









