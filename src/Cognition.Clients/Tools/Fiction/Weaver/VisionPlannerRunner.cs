using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Agents;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools.Fiction.Weaver;

public class VisionPlannerRunner : FictionPhaseRunnerBase
{
    public VisionPlannerRunner(CognitionDbContext db, IAgentService agentService, ILogger<VisionPlannerRunner> logger)
        : base(db, agentService, logger, FictionPhase.VisionPlanner)
    {
    }

    protected override async Task<FictionPhaseResult> ExecuteCoreAsync(
        FictionPlan plan,
        Conversation conversation,
        FictionPhaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("VisionPlanner runner invoked for plan {PlanId} on branch {Branch}.", plan.Id, context.BranchSlug);

        var (providerId, modelId) = ResolveProviderAndModel(context);
        var prompt = BuildVisionPrompt(plan, context);

        var stopwatch = Stopwatch.StartNew();
        var (reply, messageId) = await AgentService.ChatAsync(
            context.ConversationId,
            context.AgentId,
            providerId,
            modelId,
            prompt,
            cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        var validation = FictionResponseValidator.ValidateVisionPayload(reply, plan, context);
        if (!validation.IsValid)
        {
            throw new FictionResponseValidationException(validation);
        }

        var data = BuildResponseData(reply, "vision");
        var transcriptMetadata = new Dictionary<string, object?>
        {
            ["promptType"] = "vision-planner"
        };

        return BuildResult(
            FictionPhaseStatus.Completed,
            "Vision planning response recorded.",
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

    private static string BuildVisionPrompt(FictionPlan plan, FictionPhaseExecutionContext context)
    {
        var projectTitle = string.IsNullOrWhiteSpace(plan.FictionProject?.Title) ? plan.Name : plan.FictionProject!.Title;
        var branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug;
        var description = string.IsNullOrWhiteSpace(plan.Description) ? "(no long-form description captured yet)" : plan.Description!;
        var logline = string.IsNullOrWhiteSpace(plan.FictionProject?.Logline) ? "(no logline recorded yet)" : plan.FictionProject!.Logline!;

        return $@"You are the lead creative planner for the fiction project ""{projectTitle}"" on branch ""{branch}"".

Project description:
{description}

Project logline:
{logline}

Produce minified JSON with the following shape:
{{
  ""authorSummary"": ""string describing the author persona voice, tone, pacing, and stylistic edges"",
  ""bookGoals"": [""goal 1"", ""goal 2"", ""goal 3""],
  ""storyPlan"": ""markdown outlining acts, protagonists, antagonists, core conflicts, and thematic threads""
}}

Respond with JSON only.";
    }
}






