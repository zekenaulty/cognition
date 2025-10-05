using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
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

public class ScrollRefinerRunner : FictionPhaseRunnerBase
{
    public ScrollRefinerRunner(CognitionDbContext db, IAgentService agentService, ILogger<ScrollRefinerRunner> logger)
        : base(db, agentService, logger, FictionPhase.ScrollRefiner)
    {
    }

    protected override async Task<FictionPhaseResult> ExecuteCoreAsync(
        FictionPlan plan,
        Conversation conversation,
        FictionPhaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("ScrollRefiner runner invoked for plan {PlanId} on branch {Branch}.", plan.Id, context.BranchSlug);

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

        var prompt = BuildScrollPrompt(plan, context, blueprint, existingScroll);

        var stopwatch = Stopwatch.StartNew();
        var (reply, messageId) = await AgentService.ChatAsync(
            context.ConversationId,
            context.AgentId,
            providerId,
            modelId,
            prompt,
            cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        var validation = FictionResponseValidator.ValidateScrollPayload(reply, plan, context);
        if (!validation.IsValid)
        {
            throw new FictionResponseValidationException(validation);
        }

        var data = BuildResponseData(reply, "chapterScroll");
        var transcriptMetadata = new Dictionary<string, object?>
        {
            ["promptType"] = "scroll-refiner",
            ["chapterScrollId"] = context.ChapterScrollId,
            ["chapterBlueprintId"] = context.ChapterBlueprintId ?? blueprint?.Id
        };

        return BuildResult(
            FictionPhaseStatus.Completed,
            "Chapter scroll refinement response recorded.",
            context,
            prompt,
            reply,
            messageId,
            data,
            latencyMs: stopwatch.Elapsed.TotalMilliseconds,
            validationStatus: validation.Status,
            validationDetails: validation.Details,
            transcriptMetadata: transcriptMetadata);
    }

    private static string BuildScrollPrompt(
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        FictionChapterBlueprint? blueprint,
        FictionChapterScroll? existingScroll)
    {
        var branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug;
        var blueprintSynopsis = blueprint is null
            ? "No chapter blueprint exists yet."
            : $"Blueprint {blueprint.ChapterIndex} ({blueprint.ChapterSlug}): {blueprint.Title} — {blueprint.Synopsis}";

        var structureJson = blueprint?.Structure is null
            ? "(structure not captured yet)"
            : JsonSerializer.Serialize(blueprint.Structure);

        string scrollSummary;
        if (existingScroll is null)
        {
            scrollSummary = "No prior scroll revisions exist for this chapter.";
        }
        else
        {
            var sectionSummaries = existingScroll.Sections
                .OrderBy(s => s.SectionIndex)
                .Select(section =>
                {
                    var sceneSummary = section.Scenes
                        .OrderBy(scene => scene.SceneIndex)
                        .Select(scene => $"    Scene {scene.SceneIndex}: {scene.Title} — {scene.Description ?? "(no description)"}")
                        .DefaultIfEmpty("    (no scenes)");
                    return $"  Section {section.SectionIndex}: {section.Title} — {section.Description ?? "(no description)"}\n{string.Join("\n", sceneSummary)}";
                });

            scrollSummary = string.Join("\n", sectionSummaries);
        }

        return $@"You are refining the chapter scroll for project ""{plan.Name}"" on branch ""{branch}"".

Blueprint synopsis:
{blueprintSynopsis}

Blueprint structure (JSON):
{structureJson}

Existing scroll snapshot:
{scrollSummary}

Produce minified JSON with the following structure:
{{
  ""scrollSlug"": ""string"",
  ""title"": ""string"",
  ""synopsis"": ""string"",
  ""sections"": [
    {{
      ""sectionSlug"": ""string"",
      ""title"": ""string"",
      ""summary"": ""string"",
      ""transitions"": [""string""],
      ""scenes"": [
        {{
          ""sceneSlug"": ""string"",
          ""title"": ""string"",
          ""goal"": ""string"",
          ""conflict"": ""string"",
          ""turn"": ""string"",
          ""fallout"": ""string"",
          ""carryForward"": [""string""]
        }}
      ]
    }}
  ]
}}

Ensure the scroll aligns with the blueprint beats and that continuity notes flag canonical obligations. Respond with JSON only.";
    }
}





