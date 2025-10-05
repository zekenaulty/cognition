using System.Collections.Generic;
using System.Diagnostics;
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

public class SceneWeaverRunner : FictionPhaseRunnerBase
{
    public SceneWeaverRunner(CognitionDbContext db, IAgentService agentService, ILogger<SceneWeaverRunner> logger)
        : base(db, agentService, logger, FictionPhase.SceneWeaver)
    {
    }

    protected override async Task<FictionPhaseResult> ExecuteCoreAsync(
        FictionPlan plan,
        Conversation conversation,
        FictionPhaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("SceneWeaver runner invoked for plan {PlanId} on branch {Branch}.", plan.Id, context.BranchSlug);

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

        var branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug!;
        var prompt = BuildScenePrompt(scene, branch);

        var stopwatch = Stopwatch.StartNew();
        var (reply, messageId) = await AgentService.ChatAsync(
            context.ConversationId,
            context.AgentId,
            providerId,
            modelId,
            prompt,
            cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        var data = BuildResponseData(reply, "sceneDraft");
        var transcriptMetadata = new Dictionary<string, object?>
        {
            ["promptType"] = "scene-weaver",
            ["chapterSceneId"] = context.ChapterSceneId,
            ["sceneSlug"] = scene.SceneSlug,
            ["sectionSlug"] = scene.FictionChapterSection.SectionSlug
        };

        return BuildResult(
            FictionPhaseStatus.Completed,
            "Scene weaving response recorded.",
            context,
            prompt,
            reply,
            messageId,
            data,
            latencyMs: stopwatch.Elapsed.TotalMilliseconds,
            transcriptMetadata: transcriptMetadata);
    }

    private static string BuildScenePrompt(FictionChapterScene scene, string branch)
    {
        var section = scene.FictionChapterSection;
        var scroll = section.FictionChapterScroll;
        var blueprint = scroll?.FictionChapterBlueprint;

        var scrollSynopsis = scroll is null
            ? "(scroll synopsis unavailable)"
            : $"Scroll {scroll.ScrollSlug}: {scroll.Title} — {scroll.Synopsis}";

        var blueprintStructure = blueprint?.Structure is null
            ? "(blueprint structure unavailable)"
            : JsonSerializer.Serialize(blueprint.Structure);

        var sceneMetadataJson = scene.Metadata is null ? "(none)" : JsonSerializer.Serialize(scene.Metadata);

        return $@"You are writing the full narrative scene ""{scene.Title}"" (slug {scene.SceneSlug}) for branch ""{branch}"".

Section context:
Section {section.SectionIndex} ({section.SectionSlug}): {section.Title} — {section.Description ?? "(no description)"}

Scroll synopsis:
{scrollSynopsis}

Blueprint structure (JSON):
{blueprintStructure}

Scene metadata (JSON):
{sceneMetadataJson}

Write the complete scene in rich Markdown. Use immersive prose, consistent with the blueprint goals and the scroll synopsis. Include dialogue, action, and interiority. Target 900-1300 words. Return Markdown only; do not add JSON or commentary.";
    }
}



