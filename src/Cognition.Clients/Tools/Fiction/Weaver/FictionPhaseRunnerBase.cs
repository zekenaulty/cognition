using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Agents;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools.Fiction.Weaver;

public interface IFictionPhaseRunner
{
    FictionPhase Phase { get; }
    Task<FictionPhaseResult> RunAsync(FictionPhaseExecutionContext context, CancellationToken cancellationToken = default);
}

public abstract class FictionPhaseRunnerBase : IFictionPhaseRunner
{
    private readonly CognitionDbContext _db;
    private readonly IAgentService _agentService;
    private readonly ILogger _logger;

    protected FictionPhaseRunnerBase(CognitionDbContext db, IAgentService agentService, ILogger logger, FictionPhase phase)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Phase = phase;
    }

    protected CognitionDbContext DbContext => _db;
    protected IAgentService AgentService => _agentService;
    protected ILogger Logger => _logger;
    protected virtual DateTime UtcNow => DateTime.UtcNow;

    public FictionPhase Phase { get; }

    public async Task<FictionPhaseResult> RunAsync(FictionPhaseExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        var plan = await _db.Set<FictionPlan>()
            .Include(p => p.FictionProject)
            .FirstOrDefaultAsync(p => p.Id == context.PlanId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
        {
            var exception = new InvalidOperationException($"Fiction plan {context.PlanId} was not found.");
            _logger.LogWarning(exception, "Unable to run phase {Phase} because the plan {PlanId} is missing.", Phase, context.PlanId);
            return BuildResult(FictionPhaseStatus.Failed, $"Plan {context.PlanId} not found.", context, string.Empty, string.Empty, null, null, exception: exception);
        }

        var conversation = await LoadConversationAsync(context.ConversationId, cancellationToken).ConfigureAwait(false);
        if (conversation is null)
        {
            var exception = new InvalidOperationException($"Conversation {context.ConversationId} was not found.");
            _logger.LogWarning(exception, "Unable to run phase {Phase} because the conversation {ConversationId} is missing.", Phase, context.ConversationId);
            return BuildResult(FictionPhaseStatus.Failed, $"Conversation {context.ConversationId} not found.", context, string.Empty, string.Empty, null, null, exception: exception);
        }

        return await ExecuteCoreAsync(plan, conversation, context, cancellationToken).ConfigureAwait(false);
    }

    protected virtual async Task<Conversation?> LoadConversationAsync(Guid conversationId, CancellationToken cancellationToken)
        => await _db.Set<Conversation>().AsNoTracking().FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken).ConfigureAwait(false);

    protected (Guid ProviderId, Guid? ModelId) ResolveProviderAndModel(FictionPhaseExecutionContext context)
    {
        if (context.Metadata is null || !context.Metadata.TryGetValue("providerId", out var providerRaw) || !Guid.TryParse(providerRaw, out var providerId))
        {
            throw new InvalidOperationException("Fiction phase metadata must include a valid 'providerId' (Guid).");
        }

        Guid? modelId = null;
        if (context.Metadata.TryGetValue("modelId", out var modelRaw) && Guid.TryParse(modelRaw, out var parsedModel))
        {
            modelId = parsedModel;
        }

        return (providerId, modelId);
    }

    protected Dictionary<string, object?> BuildResponseData(string rawResponse, string? artifact = null)
    {
        var data = new Dictionary<string, object?>
        {
            ["rawResponse"] = rawResponse
        };

        if (!string.IsNullOrWhiteSpace(artifact))
        {
            data["artifact"] = artifact;
        }

        try
        {
            using var doc = JsonDocument.Parse(rawResponse);
            data["parsed"] = doc.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            data["parseError"] = ex.Message;
        }

        return data;
    }

    protected FictionPhaseResult BuildResult(
        FictionPhaseStatus status,
        string summary,
        FictionPhaseExecutionContext context,
        string requestPayload,
        string responsePayload,
        Guid? conversationMessageId,
        IReadOnlyDictionary<string, object?>? data,
        double? latencyMs = null,
        int? promptTokens = null,
        int? completionTokens = null,
        FictionTranscriptValidationStatus validationStatus = FictionTranscriptValidationStatus.Unknown,
        string? validationDetails = null,
        IReadOnlyDictionary<string, object?>? transcriptMetadata = null,
        bool isRetry = false,
        int attempt = 1,
        Exception? exception = null)
    {
        var transcriptMeta = new Dictionary<string, object?>
        {
            ["phase"] = Phase.ToString(),
            ["branch"] = context.BranchSlug
        };

        if (transcriptMetadata is not null)
        {
            foreach (var kv in transcriptMetadata)
            {
                transcriptMeta[kv.Key] = kv.Value;
            }
        }

        var transcript = new FictionPhaseTranscript(
            AgentId: context.AgentId,
            ConversationId: context.ConversationId,
            ConversationMessageId: conversationMessageId,
            ChapterBlueprintId: context.ChapterBlueprintId,
            ChapterScrollId: context.ChapterScrollId,
            ChapterSceneId: context.ChapterSceneId,
            Attempt: attempt,
            IsRetry: isRetry,
            RequestPayload: requestPayload,
            ResponsePayload: responsePayload,
            PromptTokens: promptTokens,
            CompletionTokens: completionTokens,
            LatencyMs: latencyMs,
            ValidationStatus: validationStatus,
            ValidationDetails: validationDetails,
            Metadata: transcriptMeta);

        var resultData = new Dictionary<string, object?>
        {
            ["rawResponse"] = responsePayload
        };

        if (data is not null)
        {
            foreach (var kv in data)
            {
                resultData[kv.Key] = kv.Value;
            }
        }

        return new FictionPhaseResult(Phase, status, summary, resultData, exception, new[] { transcript });
    }

    protected abstract Task<FictionPhaseResult> ExecuteCoreAsync(FictionPlan plan, Conversation conversation, FictionPhaseExecutionContext context, CancellationToken cancellationToken);
}
