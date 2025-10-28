using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Agents;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Planning;
using Cognition.Clients.Tools.Planning.Fiction;
using Cognition.Contracts;
using Cognition.Contracts.Scopes;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools.Fiction.Weaver;

public class SceneWeaverRunner : FictionPhaseRunnerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SceneWeaverPlannerTool _planner;

    public SceneWeaverRunner(
        CognitionDbContext db,
        IAgentService agentService,
        IServiceProvider serviceProvider,
        SceneWeaverPlannerTool planner,
        ILogger<SceneWeaverRunner> logger,
        IScopePathBuilder scopePathBuilder)
        : base(db, agentService, logger, FictionPhase.SceneWeaver, scopePathBuilder)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
    }

    protected override async Task<FictionPhaseResult> ExecuteCoreAsync(
        FictionPlan plan,
        Conversation conversation,
        FictionPhaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("SceneWeaver runner invoked for plan {PlanId} on branch {Branch}.", plan.Id, context.BranchSlug);

        if (!context.ChapterSceneId.HasValue)
        {
            throw new InvalidOperationException("SceneWeaver requires FictionPhaseExecutionContext.ChapterSceneId to be set.");
        }

        var (providerId, modelId) = ResolveProviderAndModel(context);

        var scene = await DbContext.Set<FictionChapterScene>()
            .AsNoTracking()
            .Include(s => s.FictionChapterSection)
                .ThenInclude(section => section.FictionChapterScroll)
                    .ThenInclude(scroll => scroll.FictionChapterBlueprint)
            .FirstOrDefaultAsync(s => s.Id == context.ChapterSceneId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (scene is null)
        {
            throw new InvalidOperationException($"Scene {context.ChapterSceneId} was not found.");
        }

        var parameters = SceneWeaverPlannerParameters.Create(
            plan,
            conversation,
            context,
            providerId,
            modelId,
            scene);

        var toolContext = new ToolContext(
            context.AgentId,
            context.ConversationId,
            PersonaId: null,
            _serviceProvider,
            cancellationToken);

        ScopePath? scopePath = null;
        if (ScopePathBuilder.TryBuild(new ScopeToken(null, null, null, context.AgentId, context.ConversationId, null, null), out var builtPath))
        {
            scopePath = builtPath;
        }

        var conversationState = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["planId"] = plan.Id,
            ["branch"] = context.BranchSlug,
            ["chapterSceneId"] = context.ChapterSceneId,
            ["chapterScrollId"] = context.ChapterScrollId ?? scene.FictionChapterSection.FictionChapterScrollId,
            ["chapterBlueprintId"] = context.ChapterBlueprintId ?? scene.FictionChapterSection.FictionChapterScroll?.FictionChapterBlueprintId
        };

        var plannerContext = PlannerContext.FromToolContext(
            toolContext,
            scopePath,
            primaryAgentId: context.AgentId,
            conversationState: conversationState);

        var plannerResult = await _planner.PlanAsync(plannerContext, parameters, cancellationToken).ConfigureAwait(false);
        return ToPhaseResult(plannerResult, context);
    }

    private static FictionPhaseResult ToPhaseResult(PlannerResult result, FictionPhaseExecutionContext context)
    {
        var summary = result.Diagnostics.TryGetValue("summary", out var summaryValue)
            ? summaryValue
            : $"Planner completed with outcome {result.Outcome}.";

        var status = result.Outcome switch
        {
            PlannerOutcome.Success => FictionPhaseStatus.Completed,
            PlannerOutcome.Partial => FictionPhaseStatus.Blocked,
            PlannerOutcome.Cancelled => FictionPhaseStatus.Cancelled,
            _ => FictionPhaseStatus.Failed
        };

        var data = result.ToDictionary();
        var transcripts = BuildTranscripts(result, context);

        return new FictionPhaseResult(
            FictionPhase.SceneWeaver,
            status,
            summary,
            data,
            Exception: null,
            Transcripts: transcripts);
    }

    private static IReadOnlyList<FictionPhaseTranscript> BuildTranscripts(PlannerResult result, FictionPhaseExecutionContext context)
    {
        var step = result.Steps.LastOrDefault();
        var prompt = TryGetString(step?.Output, "prompt");
        var response = TryGetString(step?.Output, "response");
        var messageId = TryGetGuid(step?.Output, "messageId");

        var assistantEntry = result.Transcript.LastOrDefault(t => string.Equals(t.Role, "assistant", StringComparison.OrdinalIgnoreCase));
        var transcriptMetadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["plannerOutcome"] = result.Outcome.ToString(),
            ["chapterBlueprintId"] = context.ChapterBlueprintId,
            ["chapterScrollId"] = context.ChapterScrollId,
            ["chapterSceneId"] = context.ChapterSceneId
        };

        if (assistantEntry?.Metadata is not null)
        {
            foreach (var kv in assistantEntry.Metadata)
            {
                transcriptMetadata[kv.Key] = kv.Value;
            }
        }

        foreach (var diagnostic in result.Diagnostics)
        {
            transcriptMetadata[$"diagnostic:{diagnostic.Key}"] = diagnostic.Value;
        }

        var latency = result.Metrics.TryGetValue("latencyMs", out var latencyMs) ? latencyMs : (double?)null;
        var validationStatus = transcriptMetadata.TryGetValue("validationStatus", out var statusObj)
            ? ParseValidationStatus(statusObj?.ToString())
            : FictionTranscriptValidationStatus.Unknown;
        var validationSummary = transcriptMetadata.TryGetValue("validationSummary", out var validationObj) ? validationObj?.ToString() : null;

        return new List<FictionPhaseTranscript>(1)
        {
            new FictionPhaseTranscript(
                AgentId: context.AgentId,
                ConversationId: context.ConversationId,
                ConversationMessageId: messageId,
                ChapterBlueprintId: context.ChapterBlueprintId,
                ChapterScrollId: context.ChapterScrollId,
                ChapterSceneId: context.ChapterSceneId,
                Attempt: 1,
                IsRetry: false,
                RequestPayload: prompt,
                ResponsePayload: response,
                PromptTokens: null,
                CompletionTokens: null,
                LatencyMs: latency,
                ValidationStatus: validationStatus,
                ValidationDetails: validationSummary,
                Metadata: transcriptMetadata)
        };
    }

    private static string? TryGetString(IReadOnlyDictionary<string, object?>? dictionary, string key)
    {
        if (dictionary is null) return null;
        if (!dictionary.TryGetValue(key, out var value) || value is null) return null;
        return value.ToString();
    }

    private static Guid? TryGetGuid(IReadOnlyDictionary<string, object?>? dictionary, string key)
    {
        if (dictionary is null) return null;
        if (!dictionary.TryGetValue(key, out var value) || value is null) return null;

        return value switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }

    private static FictionTranscriptValidationStatus ParseValidationStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return FictionTranscriptValidationStatus.Unknown;
        }

        return Enum.TryParse<FictionTranscriptValidationStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : FictionTranscriptValidationStatus.Unknown;
    }
}

