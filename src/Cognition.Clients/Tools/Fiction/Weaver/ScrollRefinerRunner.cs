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

public class ScrollRefinerRunner : FictionPhaseRunnerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ScrollRefinerPlannerTool _planner;
    private readonly ICharacterLifecycleService _lifecycleService;
    private readonly IAuthorPersonaRegistry _authorRegistry;

    public ScrollRefinerRunner(
        CognitionDbContext db,
        IAgentService agentService,
        IServiceProvider serviceProvider,
        ICharacterLifecycleService lifecycleService,
        IAuthorPersonaRegistry authorRegistry,
        ScrollRefinerPlannerTool planner,
        ILogger<ScrollRefinerRunner> logger,
        IScopePathBuilder scopePathBuilder)
        : base(db, agentService, logger, FictionPhase.ScrollRefiner, scopePathBuilder)
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

        var scopePath = ResolveScopePath(context);
        var resolvedScrollId = context.ChapterScrollId ?? existingScroll?.Id;
        var blocking = await GetBlockingLoreRequirementsAsync(plan.Id, resolvedScrollId, cancellationToken).ConfigureAwait(false);
        if (blocking.Count > 0)
        {
            return BuildBlockedResult(context, blocking);
        }

        var authorContext = await _authorRegistry.GetForPlanAsync(plan.Id, cancellationToken).ConfigureAwait(false);
        var conversationState = BuildConversationState(plan, context, resolvedScrollId, blueprint, authorContext);

        var plannerContext = PlannerContext.FromToolContext(
            toolContext,
            scopePath,
            primaryAgentId: context.AgentId,
            conversationState: conversationState);

        var plannerResult = await _planner.PlanAsync(plannerContext, parameters, cancellationToken).ConfigureAwait(false);
        await ProcessLifecycleAsync(plan, context, resolvedScrollId, plannerResult, cancellationToken).ConfigureAwait(false);
        await AppendAuthorPersonaMemoryAsync(plan, context, blueprint, existingScroll, resolvedScrollId, authorContext, plannerResult, cancellationToken).ConfigureAwait(false);
        return ToPhaseResult(plannerResult, context);
    }

    private async Task<IReadOnlyList<FictionLoreRequirement>> GetBlockingLoreRequirementsAsync(
        Guid planId,
        Guid? scrollId,
        CancellationToken cancellationToken)
    {
        var query = DbContext.Set<FictionLoreRequirement>()
            .AsNoTracking()
            .Where(r => r.FictionPlanId == planId && r.Status != FictionLoreRequirementStatus.Ready && r.ChapterSceneId == null);

        if (scrollId.HasValue)
        {
            query = query.Where(r => r.ChapterScrollId == scrollId);
        }
        else
        {
            query = query.Where(r => r.ChapterScrollId == null);
        }

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

        var summary = $"Scroll blocked by {blocking.Count} lore requirement(s).";
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
        FictionChapterBlueprint? blueprint,
        AuthorPersonaContext? authorContext)
    {
        var state = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["planId"] = plan.Id,
            ["branch"] = context.BranchSlug,
            ["chapterScrollId"] = scrollId,
            ["chapterBlueprintId"] = context.ChapterBlueprintId ?? blueprint?.Id
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

        var request = new CharacterLifecycleRequest(
            plan.Id,
            context.ConversationId,
            PlanPassId: null,
            characters,
            loreRequirements,
            Source: "scroll");

        await _lifecycleService.ProcessAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task AppendAuthorPersonaMemoryAsync(
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        FictionChapterBlueprint? blueprint,
        FictionChapterScroll? existingScroll,
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

        var title = blueprint is null
            ? $"Scroll revision for {plan.Name}"
            : $"Scroll revision: {blueprint.Title}";
        var content = BuildScrollMemoryContent(plan, context, blueprint, existingScroll);
        var entry = new AuthorPersonaMemoryEntry(
            title,
            content,
            plan.Id,
            scrollId,
            SceneId: null,
            SourcePhase: "scroll");

        await _authorRegistry.AppendMemoryAsync(authorContext.PersonaId, entry, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildScrollMemoryContent(
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        FictionChapterBlueprint? blueprint,
        FictionChapterScroll? existingScroll)
    {
        var branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug;
        var synopsis = string.IsNullOrWhiteSpace(blueprint?.Synopsis)
            ? "(no synopsis captured)"
            : blueprint!.Synopsis!;
        var sectionCount = existingScroll?.Sections?.Count ?? 0;
        return $"Refined scroll for branch \"{branch}\" on plan \"{plan.Name}\". Blueprint summary: {synopsis}. Prior sections tracked: {sectionCount}.";
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
