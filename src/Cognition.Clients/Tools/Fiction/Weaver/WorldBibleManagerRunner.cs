using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Clients.Agents;
using Cognition.Clients.Scope;
using Cognition.Clients.Tools.Fiction.Lifecycle;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Cognition.Clients.Tools.Fiction.Weaver;

public class WorldBibleManagerRunner : FictionPhaseRunnerBase
{

    private sealed record PlanPassSummary(int PassIndex, string Title, string? Summary);
    private readonly ICharacterLifecycleService _lifecycleService;

    public WorldBibleManagerRunner(
        CognitionDbContext db,
        IAgentService agentService,
        ICharacterLifecycleService lifecycleService,
        ILogger<WorldBibleManagerRunner> logger,
        IScopePathBuilder scopePathBuilder)
        : base(db, agentService, logger, FictionPhase.WorldBibleManager, scopePathBuilder)
    {
        _lifecycleService = lifecycleService ?? throw new ArgumentNullException(nameof(lifecycleService));
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

        var worldBibleId = TryGetMetadataGuid(context, MetadataKeys.WorldBibleId);
        if (worldBibleId.HasValue)
        {
            transcriptMetadata["worldBibleId"] = worldBibleId.Value;
            await UpsertWorldBibleEntriesAsync(worldBibleId.Value, reply, context, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            Logger.LogWarning("WorldBibleManager invoked for plan {PlanId} without a worldBibleId in metadata.", plan.Id);
        }

        await ProcessLifecycleAsync(plan, context, validation, cancellationToken).ConfigureAwait(false);

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

    private async Task ProcessLifecycleAsync(
        FictionPlan plan,
        FictionPhaseExecutionContext context,
        FictionResponseValidationResult validation,
        CancellationToken cancellationToken)
    {
        if (validation.ParsedPayload is not JToken token)
        {
            return;
        }

        var characters = LifecyclePayloadParser.ExtractCharacters(token);
        if (characters.Count == 0)
        {
            return;
        }

        var (branchSlug, branchLineage) = ResolveBranchContext(plan, context);
        var request = new CharacterLifecycleRequest(
            plan.Id,
            context.ConversationId,
            PlanPassId: null,
            characters,
            Array.Empty<LoreRequirementDescriptor>(),
            Source: "world-bible",
            BranchSlug: branchSlug,
            BranchLineage: branchLineage);

        var result = await _lifecycleService.ProcessAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.CreatedCharacters.Count > 0)
        {
            Logger.LogInformation(
                "World bible lifecycle promoted {Count} characters for plan {PlanId}.",
                result.CreatedCharacters.Count,
                plan.Id);
        }
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

    private async Task UpsertWorldBibleEntriesAsync(Guid worldBibleId, string payload, FictionPhaseExecutionContext context, CancellationToken cancellationToken)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(payload);
        }
        catch (JsonException ex)
        {
            Logger.LogWarning(ex, "WorldBible payload for worldBibleId {WorldBibleId} could not be parsed; skipping persistence.", worldBibleId);
            return;
        }

        using var doc = document;
        var root = doc.RootElement;
        var categories = new[] { "characters", "locations", "systems" };

        var entriesSet = DbContext.Set<FictionWorldBibleEntry>();
        var existingEntries = await entriesSet
            .Where(e => e.FictionWorldBibleId == worldBibleId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var latestBySlug = existingEntries
            .GroupBy(e => e.EntrySlug, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.Version).First(), StringComparer.OrdinalIgnoreCase);

        var sequence = existingEntries.Count == 0 ? 0 : existingEntries.Max(e => e.Sequence);
        var touchedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in categories)
        {
            if (!root.TryGetProperty(category, out var array) || array.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in array.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var entryName = nameElement.GetString();
                if (string.IsNullOrWhiteSpace(entryName))
                {
                    continue;
                }

                var slug = BuildEntrySlug(category, entryName);
                touchedSlugs.Add(slug);

                var summary = item.TryGetProperty("summary", out var summaryElement) && summaryElement.ValueKind == JsonValueKind.String
                    ? summaryElement.GetString() ?? string.Empty
                    : string.Empty;
                var status = item.TryGetProperty("status", out var statusElement) && statusElement.ValueKind == JsonValueKind.String
                    ? statusElement.GetString() ?? string.Empty
                    : string.Empty;

                var continuityNotes = new List<string>();
                if (item.TryGetProperty("continuityNotes", out var notesElement) && notesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var note in notesElement.EnumerateArray())
                    {
                        if (note.ValueKind == JsonValueKind.String)
                        {
                            var value = note.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                continuityNotes.Add(value);
                            }
                        }
                    }
                }

                latestBySlug.TryGetValue(slug, out var previous);
                if (previous is not null && previous.IsActive)
                {
                    previous.IsActive = false;
                    previous.UpdatedAtUtc = UtcNow;
                    previous.Content.UpdatedAtUtc = previous.UpdatedAtUtc ?? previous.CreatedAtUtc;
                }

                var updatedAt = UtcNow;
                var entry = new FictionWorldBibleEntry
                {
                    Id = Guid.NewGuid(),
                    FictionWorldBibleId = worldBibleId,
                    EntrySlug = slug,
                    EntryName = entryName.Trim(),
                    Content = BuildEntryContent(category, summary, status, continuityNotes, context, updatedAt),
                    Version = (previous?.Version ?? 0) + 1,
                    ChangeType = previous is null ? FictionWorldBibleChangeType.Seed : FictionWorldBibleChangeType.Update,
                    Sequence = ++sequence,
                    DerivedFromEntryId = previous?.Id,
                    IsActive = true,
                    CreatedAtUtc = updatedAt,
                    UpdatedAtUtc = updatedAt
                };

                entriesSet.Add(entry);
                latestBySlug[slug] = entry;
            }
        }

        // Mark entries not touched in this run as inactive so dashboards can filter on active state.
        foreach (var entry in existingEntries.Where(e => e.IsActive && !touchedSlugs.Contains(e.EntrySlug)))
        {
            entry.IsActive = false;
            entry.UpdatedAtUtc = UtcNow;
            entry.Content.UpdatedAtUtc = entry.UpdatedAtUtc ?? entry.CreatedAtUtc;
        }

        await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private FictionWorldBibleEntryContent BuildEntryContent(string category, string summary, string status, IReadOnlyCollection<string> continuityNotes, FictionPhaseExecutionContext context, DateTime updatedAt)
    {
        var iteration = context.IterationIndex ?? TryGetMetadataInt(context, MetadataKeys.IterationIndex);
        var backlogId = TryGetMetadataValue(context, MetadataKeys.BacklogItemId);

        return new FictionWorldBibleEntryContent
        {
            Category = category,
            Summary = summary,
            Status = status,
            ContinuityNotes = continuityNotes.ToArray(),
            Branch = string.IsNullOrWhiteSpace(context.BranchSlug) ? "main" : context.BranchSlug,
            IterationIndex = iteration,
            BacklogItemId = backlogId,
            UpdatedAtUtc = updatedAt
        };
    }

    private static string BuildEntrySlug(string category, string name)
    {
        var builder = new StringBuilder();
        foreach (var ch in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var normalized = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = Guid.NewGuid().ToString("N");
        }

        return $"{category}:{normalized}";
    }
}

