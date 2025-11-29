using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Agents;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Planning;
using Cognition.Clients.Tools.Fiction.Lifecycle;
using Cognition.Clients.Tools.Planning.Fiction;
using Cognition.Clients.Tools.Fiction.Authoring;
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
    private readonly ICharacterLifecycleService _lifecycleService;
    private readonly IAuthorPersonaRegistry _authorRegistry;

    public SceneWeaverRunner(
        CognitionDbContext db,
        IAgentService agentService,
        IServiceProvider serviceProvider,
        ICharacterLifecycleService lifecycleService,
        IAuthorPersonaRegistry authorRegistry,
        SceneWeaverPlannerTool planner,
        ILogger<SceneWeaverRunner> logger,
        IScopePathBuilder scopePathBuilder)
        : base(db, agentService, logger, FictionPhase.SceneWeaver, scopePathBuilder)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _lifecycleService = lifecycleService ?? throw new ArgumentNullException(nameof(lifecycleService));
        _authorRegistry = authorRegistry ?? throw new ArgumentNullException(nameof(authorRegistry));
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

        var section = scene.FictionChapterSection;
        var scrollEntity = section?.FictionChapterScroll;
        var resolvedScrollId = context.ChapterScrollId ?? scrollEntity?.Id ?? section?.FictionChapterScrollId;
        var blocking = await GetBlockingLoreRequirementsAsync(plan.Id, resolvedScrollId, context.ChapterSceneId.Value, cancellationToken).ConfigureAwait(false);
        if (blocking.Count > 0)
        {
            return BuildBlockedResult(context, blocking);
        }

        var authorContext = await _authorRegistry.GetForPlanAsync(plan.Id, cancellationToken).ConfigureAwait(false);

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

        var scopePath = ResolveScopePath(context);
        var conversationState = BuildConversationState(plan, context, resolvedScrollId, scrollEntity, authorContext);

        var plannerContext = PlannerContext.FromToolContext(
            toolContext,
            scopePath,
            primaryAgentId: context.AgentId,
            conversationState: conversationState);

        PlannerResult plannerResult;
        try
        {
            plannerResult = await _planner.PlanAsync(plannerContext, parameters, cancellationToken).ConfigureAwait(false);
        }
        catch (FictionResponseValidationException ex)
        {
            return BuildValidationBlockedResult(context, ex.Result);
        }

        await ProcessLifecycleAsync(plan, context, resolvedScrollId, plannerResult, cancellationToken).ConfigureAwait(false);
        await AppendAuthorPersonaMemoryAsync(plan, scene, context, resolvedScrollId, authorContext, plannerResult, cancellationToken).ConfigureAwait(false);
        return ToPhaseResult(plan, scene, plannerResult, context);
    }

    private async Task<IReadOnlyList<FictionLoreRequirement>> GetBlockingLoreRequirementsAsync(
        Guid planId,
        Guid? scrollId,
        Guid sceneId,
        CancellationToken cancellationToken)
    {
        var query = DbContext.Set<FictionLoreRequirement>()
            .AsNoTracking()
            .Where(r => r.FictionPlanId == planId && r.Status != FictionLoreRequirementStatus.Ready);

        query = query.Where(r =>
            r.ChapterSceneId == sceneId ||
            (r.ChapterSceneId == null && scrollId.HasValue && r.ChapterScrollId == scrollId));

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private FictionPhaseResult BuildBlockedResult(
        FictionPhaseExecutionContext context,
        IReadOnlyList<FictionLoreRequirement> blocking)
    {
        foreach (var requirement in blocking)
        {
            Logger.LogWarning(
                "FictionLoreRequirementBlocked plan={PlanId} requirement={Slug} status={Status}",
                requirement.FictionPlanId,
                requirement.RequirementSlug,
                requirement.Status);
        }

        var summary = $"Scene blocked by {blocking.Count} lore requirement(s).";
        var data = new Dictionary<string, object?>
        {
            ["blockedLoreRequirements"] = blocking.Select(r => new Dictionary<string, object?>
            {
                ["slug"] = r.RequirementSlug,
                ["status"] = r.Status.ToString(),
                ["title"] = r.Title,
                ["notes"] = r.Notes,
                ["description"] = r.Description
            }).ToList()
        };

        return BuildResult(
            FictionPhaseStatus.Blocked,
            summary,
            context,
            requestPayload: string.Empty,
            responsePayload: string.Empty,
            conversationMessageId: null,
            data);
    }

    private static Dictionary<string, object?> BuildConversationState(
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        Guid? scrollId,
        FictionChapterScroll? scroll,
        AuthorPersonaContext? authorContext)
    {
        var state = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["planId"] = plan.Id,
            ["branch"] = context.BranchSlug,
            ["chapterSceneId"] = context.ChapterSceneId,
            ["chapterScrollId"] = scrollId,
            ["chapterBlueprintId"] = context.ChapterBlueprintId ?? scroll?.FictionChapterBlueprintId
        };

        AuthorPersonaPromptContext.ApplyToConversationState(state, authorContext);
        return state;
    }

    private async Task ProcessLifecycleAsync(
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        Guid? scrollId,
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
        var loreRequirements = LifecyclePayloadParser.ExtractLoreRequirements(token, scrollId, context.ChapterSceneId);

        if (characters.Count == 0 && loreRequirements.Count == 0)
        {
            return;
        }

        var (branchSlug, branchLineage) = ResolveBranchContext(plan, context);
        var request = new CharacterLifecycleRequest(
            plan.Id,
            context.ConversationId,
            PlanPassId: null,
            characters,
            loreRequirements,
            Source: "scene",
            BranchSlug: branchSlug,
            BranchLineage: branchLineage);

        await _lifecycleService.ProcessAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task AppendAuthorPersonaMemoryAsync(
        FictionPlan plan,
        FictionChapterScene scene,
        FictionPhaseExecutionContext context,
        Guid? scrollId,
        AuthorPersonaContext? authorContext,
        PlannerResult result,
        CancellationToken cancellationToken)
    {
        if (authorContext is null)
        {
            return;
        }

        if (result.Outcome != PlannerOutcome.Success)
        {
            return;
        }

        var section = scene.FictionChapterSection;
        var scroll = section?.FictionChapterScroll;
        var branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug;
        var title = $"Scene draft: {scene.Title}";
        var sectionLabel = section is null
            ? "(section not captured)"
            : $"{section.SectionSlug} â€” {section.Title}";
        var scrollLabel = scroll is null
            ? "(scroll not captured)"
            : $"{scroll.ScrollSlug} â€” {scroll.Title}";
        var description = string.IsNullOrWhiteSpace(scene.Description)
            ? "(no scene description recorded)"
            : scene.Description!;
        var content = $"Drafted scene \"{scene.Title}\" for branch \"{branch}\" ({sectionLabel}, scroll {scrollLabel}). Track obligations: {description}";
        var entry = new AuthorPersonaMemoryEntry(
            title,
            content,
            plan.Id,
            scrollId,
            scene.Id,
            SourcePhase: "scene");

        await _authorRegistry.AppendMemoryAsync(authorContext.PersonaId, entry, cancellationToken).ConfigureAwait(false);
    }

    private static FictionPhaseResult ToPhaseResult(FictionPlan plan, FictionChapterScene scene, PlannerResult result, FictionPhaseExecutionContext context)
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
        var response = TryGetString(result.Steps.LastOrDefault()?.Output, "response") ?? string.Empty;
        var validation = FictionResponseValidator.ValidateScenePayload(response, plan, context, scene);
        if (validation.Status == FictionTranscriptValidationStatus.Failed)
        {
            status = FictionPhaseStatus.Blocked;
            summary = validation.Details ?? summary;
        }

        var transcripts = BuildTranscripts(result, context, validation);

        return new FictionPhaseResult(
            FictionPhase.SceneWeaver,
            status,
            summary,
            data,
            Exception: null,
            Transcripts: transcripts);
    }

    private static FictionPhaseResult BuildValidationBlockedResult(FictionPhaseExecutionContext context, FictionResponseValidationResult validation)
    {
        var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["validationStatus"] = validation.Status.ToString(),
            ["validationDetails"] = validation.Details,
            ["validationErrors"] = validation.Errors,
            ["salientTerms"] = validation.SalientTerms
        };

        return new FictionPhaseResult(
            FictionPhase.SceneWeaver,
            FictionPhaseStatus.Blocked,
            validation.Details ?? "Scene validation failed.",
            data,
            Exception: null,
            Transcripts: Array.Empty<FictionPhaseTranscript>());
    }

    private static IReadOnlyList<FictionPhaseTranscript> BuildTranscripts(
        PlannerResult result,
        FictionPhaseExecutionContext context,
        FictionResponseValidationResult validation)
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

        transcriptMetadata["validationStatus"] = validation.Status.ToString();
        transcriptMetadata["validationSummary"] = validation.Details;

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
        var promptTokens = result.Metrics.TryGetValue("promptTokens", out var promptTokenValue) ? (int?)promptTokenValue : null;
        var completionTokens = result.Metrics.TryGetValue("completionTokens", out var completionTokenValue) ? (int?)completionTokenValue : null;

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
                PromptTokens: promptTokens,
                CompletionTokens: completionTokens,
                LatencyMs: latency,
                ValidationStatus: validation.Status,
                ValidationDetails: validation.Details,
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
