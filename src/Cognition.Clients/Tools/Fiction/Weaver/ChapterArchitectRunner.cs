using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Agents;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools.Fiction.Weaver;

public class ChapterArchitectRunner : FictionPhaseRunnerBase
{
    public ChapterArchitectRunner(CognitionDbContext db, IAgentService agentService, ILogger<ChapterArchitectRunner> logger)
        : base(db, agentService, logger, FictionPhase.ChapterArchitect)
    {
    }

    protected override async Task<FictionPhaseResult> ExecuteCoreAsync(
        FictionPlan plan,
        Conversation conversation,
        FictionPhaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("ChapterArchitect runner invoked for plan {PlanId} on branch {Branch}.", plan.Id, context.BranchSlug);

        var (providerId, modelId) = ResolveProviderAndModel(context);

        FictionChapterBlueprint? existingBlueprint = null;
        if (context.ChapterBlueprintId.HasValue)
        {
            existingBlueprint = await DbContext.Set<FictionChapterBlueprint>()
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == context.ChapterBlueprintId.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        var passes = await DbContext.Set<FictionPlanPass>()
            .AsNoTracking()
            .Where(p => p.FictionPlanId == plan.Id)
            .OrderBy(p => p.PassIndex)
            .Select(p => new PlanPassSummary(p.PassIndex, p.Title ?? $"Pass {p.PassIndex}", p.Summary))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var prompt = BuildChapterBlueprintPrompt(plan, context, passes, existingBlueprint);

        var stopwatch = Stopwatch.StartNew();
        var (reply, messageId) = await AgentService.ChatAsync(
            context.ConversationId,
            context.AgentId,
            providerId,
            modelId,
            prompt,
            cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        var validation = FictionResponseValidator.ValidateBlueprintPayload(reply, plan, context);
        if (!validation.IsValid)
        {
            throw new FictionResponseValidationException(validation);
        }

        var data = BuildResponseData(reply, "chapterBlueprint");
        var transcriptMetadata = new Dictionary<string, object?>
        {
            ["promptType"] = "chapter-architect",
            ["chapterBlueprintId"] = context.ChapterBlueprintId
        };

        return BuildResult(
            FictionPhaseStatus.Completed,
            "Chapter blueprint response recorded.",
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

    private static string BuildChapterBlueprintPrompt(
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        IReadOnlyCollection<PlanPassSummary> passes,
        FictionChapterBlueprint? existing)
    {
        var branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug;
        var description = string.IsNullOrWhiteSpace(plan.Description) ? "(no long-form description captured yet)" : plan.Description!;
        var passesSummary = passes.Count == 0
            ? "No iterative planning passes recorded yet."
            : string.Join("\n", passes.Select(p => $"Pass {p.PassIndex}: {p.Title} - {p.Summary ?? "(no summary)"}"));

        var existingBlueprintSummary = existing is null
            ? "No existing blueprint for this chapter."
            : $"Existing blueprint (index {existing.ChapterIndex}, slug {existing.ChapterSlug}): {existing.Title} - {existing.Synopsis}";

        var builder = new StringBuilder();
        builder.AppendLine($"You are the chapter architect for the fiction project \"{plan.Name}\" on branch \"{branch}\".");
        builder.AppendLine();
        builder.AppendLine("Project description:");
        builder.AppendLine(description);
        builder.AppendLine();
        builder.AppendLine("Planning context:");
        builder.AppendLine(passesSummary);
        builder.AppendLine();
        builder.AppendLine("Existing blueprint context:");
        builder.AppendLine(existingBlueprintSummary);
        builder.AppendLine();
        builder.AppendLine("Produce minified JSON with the following structure:");
        builder.AppendLine("{");
        builder.AppendLine("  \"title\": \"string\",");
        builder.AppendLine("  \"synopsis\": \"string\",");
        builder.AppendLine("  \"structure\": [");
        builder.AppendLine("    {");
        builder.AppendLine("      \"slug\": \"string\",");
        builder.AppendLine("      \"summary\": \"string\",");
        builder.AppendLine("      \"goal\": \"string\",");
        builder.AppendLine("      \"obstacle\": \"string\",");
        builder.AppendLine("      \"turn\": \"string\",");
        builder.AppendLine("      \"fallout\": \"string\",");
        builder.AppendLine("      \"carryForward\": [\"string\"]");
        builder.AppendLine("    }");
        builder.AppendLine("  ]");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("Each structure entry should capture a major beat for the chapter. Respond with JSON only.");
        return builder.ToString();
    }

    private sealed record PlanPassSummary(int PassIndex, string Title, string? Summary);
}








