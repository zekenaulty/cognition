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

public class IterativePlannerRunner : FictionPhaseRunnerBase
{
    public IterativePlannerRunner(CognitionDbContext db, IAgentService agentService, ILogger<IterativePlannerRunner> logger)
        : base(db, agentService, logger, FictionPhase.IterativePlanner)
    {
    }

    protected override async Task<FictionPhaseResult> ExecuteCoreAsync(
        FictionPlan plan,
        Conversation conversation,
        FictionPhaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("IterativePlanner runner invoked for plan {PlanId} on branch {Branch} iteration {Iteration}.", plan.Id, context.BranchSlug, context.IterationIndex);

        var (providerId, modelId) = ResolveProviderAndModel(context);

        var existingPasses = await DbContext.Set<FictionPlanPass>()
            .AsNoTracking()
            .Where(p => p.FictionPlanId == plan.Id)
            .OrderBy(p => p.PassIndex)
            .Select(p => new PlanPassSummary(p.PassIndex, p.Title ?? $"Pass {p.PassIndex}", p.Summary))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var prompt = BuildIterativePrompt(plan, context, existingPasses);

        var stopwatch = Stopwatch.StartNew();
        var (reply, messageId) = await AgentService.ChatAsync(
            context.ConversationId,
            context.AgentId,
            providerId,
            modelId,
            prompt,
            cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        var validation = FictionResponseValidator.ValidateIterativePayload(reply, plan, context);
        if (!validation.IsValid)
        {
            throw new FictionResponseValidationException(validation);
        }

        var data = BuildResponseData(reply, "iterativePlan");
        var transcriptMetadata = new Dictionary<string, object?>
        {
            ["promptType"] = "iterative-planner",
            ["iterationIndex"] = context.IterationIndex
        };

        return BuildResult(
            FictionPhaseStatus.Completed,
            "Iterative planning pass completed.",
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

    private static string BuildIterativePrompt(
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        IReadOnlyCollection<PlanPassSummary> existingPasses)
    {
        var branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug;
        var description = string.IsNullOrWhiteSpace(plan.Description) ? "(no long-form description captured yet)" : plan.Description!;
        var iterationLabel = context.IterationIndex.HasValue ? context.IterationIndex.Value.ToString() : "(unspecified)";
        var passesSummary = existingPasses.Count == 0
            ? "No previous planning passes recorded yet."
            : string.Join("\n", existingPasses.Select(p => $"Pass {p.PassIndex}: {p.Title} — {p.Summary ?? "(no summary)"}"));

        return $@"You are running an iterative planning pass (iteration {iterationLabel}) for the fiction project ""{plan.Name}"" on branch ""{branch}"".

Project description:
{description}

Existing planning passes:
{passesSummary}

Produce minified JSON with keys:
{{
  ""storyAdjustments"": [""string""],
  ""characterPriorities"": [""string""],
  ""locationNotes"": [""string""],
  ""systemsConsiderations"": [""string""],
  ""risks"": [""string""]
}}

Each array should contain concrete, actionable items. Respond with JSON only.";
    }

    private sealed record PlanPassSummary(int PassIndex, string Title, string? Summary);
}





