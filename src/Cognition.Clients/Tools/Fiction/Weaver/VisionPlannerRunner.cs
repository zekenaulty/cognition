using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Agents;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools;
using Cognition.Clients.Tools.Fiction.Lifecycle;
using Cognition.Clients.Tools.Planning;
using Cognition.Clients.Tools.Planning.Fiction;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools.Fiction.Weaver;

public class VisionPlannerRunner : FictionPhaseRunnerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly VisionPlannerTool _planner;
    private readonly ICharacterLifecycleService _lifecycleService;

    public VisionPlannerRunner(
        CognitionDbContext db,
        IAgentService agentService,
        IServiceProvider serviceProvider,
        ICharacterLifecycleService lifecycleService,
        VisionPlannerTool planner,
        ILogger<VisionPlannerRunner> logger,
        IScopePathBuilder scopePathBuilder)
        : base(db, agentService, logger, FictionPhase.VisionPlanner, scopePathBuilder)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _lifecycleService = lifecycleService ?? throw new ArgumentNullException(nameof(lifecycleService));
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
    }

    protected override async Task<FictionPhaseResult> ExecuteCoreAsync(
        FictionPlan plan,
        Conversation conversation,
        FictionPhaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("VisionPlanner runner invoked for plan {PlanId} on branch {Branch}.", plan.Id, context.BranchSlug);

        var (providerId, modelId) = ResolveProviderAndModel(context);
        var parameters = VisionPlannerParameters.Create(plan, conversation, context, providerId, modelId);

        var toolContext = new ToolContext(
            context.AgentId,
            context.ConversationId,
            PersonaId: null,
            _serviceProvider,
            cancellationToken);

        var scopePath = ResolveScopePath(context);

        var plannerContext = PlannerContext.FromToolContext(
            toolContext,
            scopePath,
            primaryAgentId: context.AgentId,
            conversationState: new Dictionary<string, object?>
            {
                ["planId"] = plan.Id,
                ["branch"] = context.BranchSlug
            });

        var plannerResult = await _planner.PlanAsync(plannerContext, parameters, cancellationToken).ConfigureAwait(false);
        await ProcessLifecycleAsync(plan, context, plannerResult, cancellationToken).ConfigureAwait(false);
        return ToPhaseResult(plannerResult, context);
    }

    private async Task ProcessLifecycleAsync(
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        PlannerResult result,
        CancellationToken cancellationToken)
    {
        if (!result.Artifacts.TryGetValue("parsed", out var parsedArtifact))
        {
            return;
        }

        var token = LifecyclePayloadParser.TryConvertToToken(parsedArtifact);
        if (token is null)
        {
            return;
        }

        var characters = LifecyclePayloadParser.ExtractCharacters(token);
        var loreRequirements = LifecyclePayloadParser.ExtractLoreRequirements(token);

        if (characters.Count == 0 && loreRequirements.Count == 0)
        {
            return;
        }

        var request = new CharacterLifecycleRequest(
            plan.Id,
            context.ConversationId,
            PlanPassId: null,
            characters,
            loreRequirements,
            Source: "vision");

        var lifecycleResult = await _lifecycleService.ProcessAsync(request, cancellationToken).ConfigureAwait(false);
        if (lifecycleResult.CreatedCharacters.Count > 0 || lifecycleResult.UpsertedLoreRequirements.Count > 0)
        {
            Logger.LogInformation(
                "Vision lifecycle processed for plan {PlanId}: {Characters} characters, {Lore} lore requirements.",
                plan.Id,
                lifecycleResult.CreatedCharacters.Count,
                lifecycleResult.UpsertedLoreRequirements.Count);
        }
    }

    private FictionPhaseResult ToPhaseResult(PlannerResult result, FictionPhaseExecutionContext context)
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
            FictionPhase.VisionPlanner,
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
            ["plannerOutcome"] = result.Outcome.ToString()
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
        var validationStatus = ParseValidationStatus(transcriptMetadata.TryGetValue("validationStatus", out var statusObj) ? statusObj?.ToString() : null);
        var validationDetails = transcriptMetadata.TryGetValue("validationSummary", out var summaryObj) ? summaryObj?.ToString() : null;

        var transcripts = new List<FictionPhaseTranscript>(1)
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
                ValidationDetails: validationDetails,
                Metadata: transcriptMetadata)
        };

        return new ReadOnlyCollection<FictionPhaseTranscript>(transcripts);
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
