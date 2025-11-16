using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Tools.Fiction.Lifecycle;
using Newtonsoft.Json.Linq;

namespace Cognition.Jobs;

public sealed class FictionLifecycleWorkflowTelemetry : IFictionLifecycleTelemetry
{
    private readonly WorkflowEventLogger _workflowLogger;

    public FictionLifecycleWorkflowTelemetry(WorkflowEventLogger workflowLogger)
    {
        _workflowLogger = workflowLogger ?? throw new ArgumentNullException(nameof(workflowLogger));
    }

    public async Task LifecycleProcessedAsync(
        CharacterLifecycleRequest request,
        CharacterLifecycleResult result,
        CancellationToken cancellationToken)
    {
        if (!HasConversation(request))
        {
            return;
        }

        var payload = new JObject
        {
            ["planId"] = request.PlanId,
            ["source"] = request.Source ?? string.Empty,
            ["branchSlug"] = request.BranchSlug ?? string.Empty,
            ["charactersCreated"] = result.CreatedCharacters.Count,
            ["charactersUpdated"] = result.UpdatedCharacters.Count,
            ["loreRequirementsUpserted"] = result.UpsertedLoreRequirements.Count
        };

        if (request.BranchLineage is { Count: > 0 })
        {
            payload["branchLineage"] = new JArray(request.BranchLineage);
        }

        if (result.CreatedCharacters.Count > 0)
        {
            payload["created"] = new JArray(result.CreatedCharacters.Select(c => new JObject
            {
                ["slug"] = c.Slug,
                ["displayName"] = c.DisplayName,
                ["worldBibleEntryId"] = c.WorldBibleEntryId,
                ["personaId"] = c.PersonaId
            }));
        }

        if (result.UpsertedLoreRequirements.Count > 0)
        {
            payload["loreRequirements"] = new JArray(result.UpsertedLoreRequirements.Select(r => new JObject
            {
                ["slug"] = r.RequirementSlug,
                ["status"] = r.Status.ToString(),
                ["chapterScrollId"] = r.ChapterScrollId,
                ["chapterSceneId"] = r.ChapterSceneId
            }));
        }

        await _workflowLogger.LogAsync(request.ConversationId!.Value, "fiction.lifecycle", payload).ConfigureAwait(false);
    }

    private static bool HasConversation(CharacterLifecycleRequest request)
        => request.ConversationId.HasValue;
}
