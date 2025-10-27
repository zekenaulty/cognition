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

public class ScrollRefinerRunner : FictionPhaseRunnerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ScrollRefinerPlannerTool _planner;

    public ScrollRefinerRunner(
        CognitionDbContext db,
        IAgentService agentService,
        IServiceProvider serviceProvider,
        ScrollRefinerPlannerTool planner,
        ILogger<ScrollRefinerRunner> logger,
        IScopePathBuilder scopePathBuilder)
        : base(db, agentService, logger, FictionPhase.ScrollRefiner, scopePathBuilder)
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
        Logger.LogInformation("ScrollRefiner runner invoked for plan {PlanId} on branch {Branch}.", plan.Id, context.BranchSlug);

        var (providerId, modelId) = ResolveProviderAndModel(context);

        FictionChapterScroll? existingScroll = null;
        FictionChapterBlueprint? blueprint = null;

        if (context.ChapterScrollId.HasValue)
        {
            existingScroll = await DbContext.Set<FictionChapterScroll>()
                .AsNoTracking()
                .Include(s => s.Sections)
                    .ThenInclude(section => section.Scenes)
                .FirstOrDefaultAsync(s => s.Id == context.ChapterScrollId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (existingScroll is not null)
            {
                blueprint = await DbContext.Set<FictionChapterBlueprint>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == existingScroll.FictionChapterBlueprintId, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (blueprint is null && context.ChapterBlueprintId.HasValue)
        {
            blueprint = await DbContext.Set<FictionChapterBlueprint>()
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == context.ChapterBlueprintId.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        var parameters = ScrollRefinerPlannerParameters.Create(
            plan,
            conversation,
            context,
            providerId,
            modelId,
            blueprint,
            existingScroll);

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

        var plannerContext = PlannerContext.FromToolContext(
            toolContext,
            scopePath,
            primaryAgentId: context.AgentId,
            conversationState: new Dictionary<string, object?>
            {
                ["planId"] = plan.Id,
                ["branch"] = context.BranchSlug,
                ["chapterScrollId"] = context.ChapterScrollId,
                ["chapterBlueprintId"] = context.ChapterBlueprintId ?? blueprint?.Id
            });

        var plannerResult = await _planner.PlanAsync(plannerContext, parameters, cancellationToken).ConfigureAwait(false);
        return ToPhaseResult(plannerResult, context);
    }

    private static FictionPhaseResult ToPhaseResult(PlannerResult result, FictionPhaseExecutionContext context)
    {
        var summary = result.Diagnostics.TryGetValue("validationSummary", out var validationSummary)
            ? validationSummary
            : $"Planner completed with outcome {result.Outcome}.";

        var data = result.ToDictionary();
        var transcripts = BuildTranscripts(result, context);

        var status = result.Outcome switch
        {
            PlannerOutcome.Success => FictionPhaseStatus.Completed,
            PlannerOutcome.Partial => FictionPhaseStatus.Blocked,
            PlannerOutcome.Cancelled => FictionPhaseStatus.Cancelled,
            _ => FictionPhaseStatus.Failed
        };

        return new FictionPhaseResult(
            FictionPhase.ScrollRefiner,
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
            ["chapterScrollId"] = context.ChapterScrollId,
            ["chapterBlueprintId"] = context.ChapterBlueprintId
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
