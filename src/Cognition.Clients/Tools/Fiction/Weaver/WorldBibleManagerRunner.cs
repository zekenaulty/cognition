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

public class WorldBibleManagerRunner : FictionPhaseRunnerBase
{

    private sealed record PlanPassSummary(int PassIndex, string Title, string? Summary);

    public WorldBibleManagerRunner(CognitionDbContext db, IAgentService agentService, ILogger<WorldBibleManagerRunner> logger)
        : base(db, agentService, logger, FictionPhase.WorldBibleManager)
    {
    }

    protected override async Task<FictionPhaseResult> ExecuteCoreAsync(
        FictionPlan plan,
        Conversation conversation,
        FictionPhaseExecutionContext context,
        CancellationToken cancellationToken)
    {
        Logger.LogDebug("WorldBibleManager runner invoked for plan {PlanId} on branch {Branch}.", plan.Id, context.BranchSlug);

        var (providerId, modelId) = ResolveProviderAndModel(context);
        var passes = await DbContext.Set<FictionPlanPass>()
            .AsNoTracking()
            .Where(p => p.FictionPlanId == plan.Id)
            .OrderBy(p => p.PassIndex)
            .Select(p => new PlanPassSummary(p.PassIndex, p.Title ?? $"Pass {p.PassIndex}", p.Summary))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var prompt = BuildWorldBiblePrompt(plan, context, passes);

        var stopwatch = Stopwatch.StartNew();
        var (reply, messageId) = await AgentService.ChatAsync(
            context.ConversationId,
            context.AgentId,
            providerId,
            modelId,
            prompt,
            cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        var validation = FictionResponseValidator.ValidateWorldBiblePayload(reply, plan, context);
        if (!validation.IsValid)
        {
            throw new FictionResponseValidationException(validation);
        }

        var data = BuildResponseData(reply, "worldBible");
        var transcriptMetadata = new Dictionary<string, object?>
        {
            ["promptType"] = "world-bible-manager"
        };

        return BuildResult(
            FictionPhaseStatus.Completed,
            "World bible update recorded.",
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

    private static string BuildWorldBiblePrompt(
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        IReadOnlyCollection<PlanPassSummary> passes)
    {
        var branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug;
        var description = string.IsNullOrWhiteSpace(plan.Description) ? "(no long-form description captured yet)" : plan.Description!;
        var passesSummary = passes.Count == 0
            ? "No iterative planning passes have been recorded yet."
            : string.Join("\n", passes.Select(p => $"Pass {p.PassIndex}: {p.Title} — {p.Summary ?? "(no summary)"}"));

        return $@"You are maintaining the world bible for the fiction project ""{plan.Name}"" on branch ""{branch}"".

Project description:
{description}

Recent planning notes:
{passesSummary}

Produce minified JSON with this structure:
{{
  ""characters"": [{{ ""name"": ""string"", ""summary"": ""string"", ""status"": ""string"", ""continuityNotes"": [""string""] }}],
  ""locations"": [{{ ""name"": ""string"", ""summary"": ""string"", ""status"": ""string"", ""continuityNotes"": [""string""] }}],
  ""systems"": [{{ ""name"": ""string"", ""summary"": ""string"", ""status"": ""string"", ""continuityNotes"": [""string""] }}]
}}

Ensure every entry references concrete story information and call out continuity obligations in ""continuityNotes"". Respond with JSON only.";
    }
}







