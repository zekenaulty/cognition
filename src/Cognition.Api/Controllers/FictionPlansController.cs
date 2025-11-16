using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Security;
using Cognition.Clients.Tools.Fiction.Authoring;
using Cognition.Clients.Tools.Fiction.Lifecycle;
using Cognition.Clients.Tools.Fiction.Weaver;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Conversations;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
using Cognition.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.ViewerOrHigher)]
[ApiController]
[Route("api/fiction/plans")]
public sealed class FictionPlansController : ControllerBase
{
    private readonly CognitionDbContext _db;
    private readonly ICharacterLifecycleService _lifecycleService;
    private readonly IAuthorPersonaRegistry _authorPersonaRegistry;
    private readonly IFictionBacklogScheduler _backlogScheduler;
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public FictionPlansController(
        CognitionDbContext db,
        ICharacterLifecycleService lifecycleService,
        IAuthorPersonaRegistry authorPersonaRegistry,
        IFictionBacklogScheduler backlogScheduler)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _lifecycleService = lifecycleService ?? throw new ArgumentNullException(nameof(lifecycleService));
        _authorPersonaRegistry = authorPersonaRegistry ?? throw new ArgumentNullException(nameof(authorPersonaRegistry));
        _backlogScheduler = backlogScheduler ?? throw new ArgumentNullException(nameof(backlogScheduler));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FictionPlanSummary>>> GetPlans(CancellationToken cancellationToken)
    {
        var plans = await _db.FictionPlans
            .AsNoTracking()
            .Include(p => p.FictionProject)
            .OrderBy(p => p.FictionProject!.Title ?? p.Name)
            .ThenBy(p => p.Name)
            .Select(p => new FictionPlanSummary(
                p.Id,
                p.Name,
                p.FictionProject!.Title,
                p.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(plans);
    }

    [HttpGet("{planId:guid}/roster")]
    public async Task<ActionResult<FictionPlanRosterResponse>> GetRoster(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _db.FictionPlans
            .AsNoTracking()
            .Include(p => p.FictionProject)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
        {
            return NotFound();
        }

        var characters = await _db.FictionCharacters
            .AsNoTracking()
            .Where(c => c.FictionPlanId == plan.Id)
            .Include(c => c.Persona)
            .Include(c => c.Agent)
            .Include(c => c.WorldBibleEntry)
                .ThenInclude(entry => entry!.FictionWorldBible)
            .OrderBy(c => c.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var loreRequirements = await _db.FictionLoreRequirements
            .AsNoTracking()
            .Where(r => r.FictionPlanId == plan.Id)
            .Include(r => r.WorldBibleEntry)
            .OrderBy(r => r.Title)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var response = new FictionPlanRosterResponse(
            plan.Id,
            plan.Name,
            plan.FictionProject?.Title,
            plan.PrimaryBranchSlug,
            characters.Select(c => MapCharacter(c, plan.PrimaryBranchSlug)).ToArray(),
            loreRequirements.Select(r => MapLoreRequirement(r, plan.PrimaryBranchSlug)).ToArray());

        return Ok(response);
    }

    [HttpGet("{planId:guid}/backlog")]
    public async Task<ActionResult<IReadOnlyList<BacklogItemResponse>>> GetBacklog(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _db.FictionPlans
            .AsNoTracking()
            .Include(p => p.Backlog)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
        {
            return NotFound();
        }

        var tasks = plan.CurrentConversationPlanId.HasValue
            ? await _db.ConversationTasks
                .AsNoTracking()
                .Where(t => t.ConversationPlanId == plan.CurrentConversationPlanId.Value)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : new List<ConversationTask>();

        var response = plan.Backlog
            .OrderBy(item => item.CreatedAtUtc)
            .Select(item => MapBacklogItem(item, tasks))
            .ToList();

        return Ok(response);
    }

    [HttpPost("{planId:guid}/backlog/{backlogId}/resume")]
    public async Task<ActionResult<BacklogItemResponse>> ResumeBacklog(
        Guid planId,
        string backlogId,
        [FromBody] ResumeBacklogRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(backlogId))
        {
            return BadRequest("Backlog ID is required.");
        }

        var plan = await _db.FictionPlans
            .Include(p => p.Backlog)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
        {
            return NotFound();
        }

        var backlog = plan.Backlog.FirstOrDefault(item => item.BacklogId.Equals(backlogId, StringComparison.OrdinalIgnoreCase));
        if (backlog is null)
        {
            return NotFound();
        }

        if (backlog.Status == FictionPlanBacklogStatus.Complete)
        {
            return Conflict($"Backlog item '{backlog.BacklogId}' is already complete.");
        }

        var conversationPlan = await _db.ConversationPlans
            .Include(cp => cp.Tasks)
            .FirstOrDefaultAsync(cp => cp.Id == request.ConversationPlanId, cancellationToken)
            .ConfigureAwait(false);

        if (conversationPlan is null || conversationPlan.ConversationId != request.ConversationId)
        {
            return BadRequest("Conversation context mismatch for the supplied conversation plan.");
        }

        if (plan.CurrentConversationPlanId.HasValue && plan.CurrentConversationPlanId.Value != conversationPlan.Id)
        {
            plan.CurrentConversationPlanId = conversationPlan.Id;
        }

        var conversationTask = conversationPlan.Tasks.FirstOrDefault(t => t.Id == request.TaskId);
        if (conversationTask is null)
        {
            return NotFound("Conversation task not found for the supplied backlog resume request.");
        }

        if (!string.Equals(conversationTask.BacklogItemId, backlog.BacklogId, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Conversation task does not map to the supplied backlog item.");
        }

        var branchSlug = string.IsNullOrWhiteSpace(request.BranchSlug)
            ? (plan.PrimaryBranchSlug ?? "main")
            : request.BranchSlug.Trim();

        var args = DeserializeArgs(conversationTask.ArgsJson);
        SetArg(args, "planId", plan.Id);
        SetArg(args, "agentId", request.AgentId);
        SetArg(args, "conversationId", request.ConversationId);
        SetArg(args, "providerId", request.ProviderId);
        SetArg(args, "modelId", request.ModelId);
        SetArg(args, "branchSlug", branchSlug);
        SetArg(args, "backlogItemId", backlog.BacklogId);

        conversationTask.ArgsJson = JsonSerializer.Serialize(args, MetadataSerializerOptions);
        conversationTask.Status = "Pending";
        conversationTask.Error = null;
        conversationTask.Observation = null;
        conversationTask.UpdatedAtUtc = DateTime.UtcNow;

        backlog.Status = FictionPlanBacklogStatus.Pending;
        backlog.InProgressAtUtc = null;
        backlog.CompletedAtUtc = null;
        backlog.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["conversationPlanId"] = conversationPlan.Id.ToString(),
            ["providerId"] = request.ProviderId.ToString(),
            ["taskId"] = conversationTask.Id.ToString(),
            ["backlogItemId"] = backlog.BacklogId
        };
        if (request.ModelId.HasValue)
        {
            metadata["modelId"] = request.ModelId.Value.ToString();
        }

        var executionContext = new FictionPhaseExecutionContext(
            plan.Id,
            request.AgentId,
            request.ConversationId,
            branchSlug,
            Metadata: metadata);

        var resumeResult = FictionPhaseResult.Pending(
            FictionPhase.VisionPlanner,
            "Backlog resume initiated via API.",
            new Dictionary<string, object?>());

        await _backlogScheduler.ScheduleAsync(plan, FictionPhase.VisionPlanner, resumeResult, executionContext, cancellationToken).ConfigureAwait(false);

        return Ok(MapBacklogItem(backlog, new[] { conversationTask }));
    }

    [HttpGet("{planId:guid}/lore/summary")]
    public async Task<ActionResult<IReadOnlyList<LoreBranchSummary>>> GetLoreSummary(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _db.FictionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
        {
            return NotFound();
        }

        var requirements = await _db.FictionLoreRequirements
            .AsNoTracking()
            .Where(r => r.FictionPlanId == plan.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var summaries = requirements
            .GroupBy(r =>
            {
                var metadata = TryParseJson(r.MetadataJson);
                var context = ResolveBranchContext(metadata, plan.PrimaryBranchSlug);
                return context.Slug.ToLowerInvariant();
            })
            .Select(group =>
            {
                var sample = group.First();
                var metadata = TryParseJson(sample.MetadataJson);
                var context = ResolveBranchContext(metadata, plan.PrimaryBranchSlug);
                return new LoreBranchSummary(
                    context.Slug,
                    context.Lineage,
                    group.Count(r => r.Status == FictionLoreRequirementStatus.Ready),
                    group.Count(r => r.Status == FictionLoreRequirementStatus.Blocked),
                    group.Count(r => r.Status == FictionLoreRequirementStatus.Planned));
            })
            .OrderBy(summary => summary.BranchSlug, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Ok(summaries);
    }

    [HttpPost("{planId:guid}/lore/{requirementId:guid}/fulfill")]
    public async Task<ActionResult<LoreRequirementRosterItem>> FulfillLoreRequirement(
        Guid planId,
        Guid requirementId,
        [FromBody] FulfillLoreRequirementRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await _db.FictionPlans
            .Include(p => p.FictionProject)
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
        {
            return NotFound();
        }

        var requirement = await _db.FictionLoreRequirements
            .Include(r => r.WorldBibleEntry)
            .FirstOrDefaultAsync(r => r.FictionPlanId == plan.Id && r.Id == requirementId, cancellationToken)
            .ConfigureAwait(false);

        if (requirement is null)
        {
            return NotFound();
        }

        var branchContext = ResolveBranchContext(request.BranchSlug, request.BranchLineage, plan.PrimaryBranchSlug);
        requirement.Status = FictionLoreRequirementStatus.Ready;
        requirement.WorldBibleEntryId = request.WorldBibleEntryId ?? requirement.WorldBibleEntryId;
        requirement.Notes = request.Notes ?? requirement.Notes;
        requirement.UpdatedAtUtc = DateTime.UtcNow;
        requirement.MetadataJson = UpdateLoreMetadata(requirement.MetadataJson, branchContext, request);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await EmitLoreFulfillmentTelemetry(plan, requirement, branchContext, request, cancellationToken).ConfigureAwait(false);

        return Ok(MapLoreRequirement(requirement, plan.PrimaryBranchSlug));
    }

    [HttpGet("{planId:guid}/author-persona")]
    public async Task<ActionResult<AuthorPersonaContextResponse>> GetAuthorPersona(Guid planId, CancellationToken cancellationToken)
    {
        var context = await _authorPersonaRegistry.GetForPlanAsync(planId, cancellationToken).ConfigureAwait(false);
        if (context is null)
        {
            return NotFound();
        }

        var response = new AuthorPersonaContextResponse(
            context.PersonaId,
            context.PersonaName,
            context.Summary,
            context.Memories,
            context.WorldNotes);

        return Ok(response);
    }

    private static CharacterRosterItem MapCharacter(FictionCharacter entity, string? defaultBranch)
    {
        var persona = entity.Persona is null
            ? null
            : new PersonaSummary(
                entity.Persona.Id,
                entity.Persona.Name,
                entity.Persona.Role,
                entity.Persona.Voice,
                entity.Persona.Essence,
                entity.Persona.Background,
                entity.Persona.CommunicationStyle);

        var agent = entity.Agent is null
            ? null
            : new AgentSummary(
                entity.Agent.Id,
                entity.Agent.PersonaId,
                entity.Agent.RolePlay);

        var worldBible = entity.WorldBibleEntry is null
            ? null
            : new WorldBibleEntrySummary(
                entity.WorldBibleEntry.Id,
                entity.WorldBibleEntry.FictionWorldBibleId,
                entity.WorldBibleEntry.FictionWorldBible?.Domain ?? string.Empty,
                entity.WorldBibleEntry.EntrySlug,
                entity.WorldBibleEntry.EntryName,
                entity.WorldBibleEntry.Content.Category,
                entity.WorldBibleEntry.Content.Summary,
                entity.WorldBibleEntry.Content.Status,
                entity.WorldBibleEntry.Content.ContinuityNotes ?? Array.Empty<string>(),
                entity.WorldBibleEntry.UpdatedAtUtc ?? entity.WorldBibleEntry.CreatedAtUtc);

        var provenance = TryParseJson(entity.ProvenanceJson);
        var branch = ResolveBranchContext(provenance, defaultBranch);

        return new CharacterRosterItem(
            entity.Id,
            entity.Slug,
            entity.DisplayName,
            entity.Role,
            entity.Importance,
            entity.Summary,
            entity.Notes,
            entity.PersonaId,
            persona,
            entity.AgentId,
            agent,
            entity.WorldBibleEntryId,
            worldBible,
            entity.FirstSceneId,
            entity.CreatedByPlanPassId,
            branch.Slug,
            branch.Lineage,
            provenance,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
    }

    private static LoreRequirementRosterItem MapLoreRequirement(FictionLoreRequirement entity, string? defaultBranch)
    {
        var worldBible = entity.WorldBibleEntry is null
            ? null
            : new WorldBibleEntrySummary(
                entity.WorldBibleEntry.Id,
                entity.WorldBibleEntry.FictionWorldBibleId,
                entity.WorldBibleEntry.FictionWorldBible?.Domain ?? string.Empty,
                entity.WorldBibleEntry.EntrySlug,
                entity.WorldBibleEntry.EntryName,
                entity.WorldBibleEntry.Content.Category,
                entity.WorldBibleEntry.Content.Summary,
                entity.WorldBibleEntry.Content.Status,
                entity.WorldBibleEntry.Content.ContinuityNotes ?? Array.Empty<string>(),
                entity.WorldBibleEntry.UpdatedAtUtc ?? entity.WorldBibleEntry.CreatedAtUtc);

        var metadata = TryParseJson(entity.MetadataJson);
        var branch = ResolveBranchContext(metadata, defaultBranch);

        return new LoreRequirementRosterItem(
            entity.Id,
            entity.Title,
            entity.RequirementSlug,
            entity.Status,
            entity.Description,
            entity.Notes,
            entity.WorldBibleEntryId,
            worldBible,
            metadata,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.CreatedByPlanPassId,
            entity.ChapterScrollId,
            entity.ChapterSceneId,
            branch.Slug,
            branch.Lineage);
    }

    private static BacklogItemResponse MapBacklogItem(FictionPlanBacklogItem entity, IEnumerable<ConversationTask> tasks)
    {
        var task = FindTaskForBacklog(tasks, entity);
        return new BacklogItemResponse(
            entity.Id,
            entity.BacklogId,
            entity.Description,
            entity.Status,
            entity.Inputs,
            entity.Outputs,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            task?.ConversationPlanId,
            task?.Id,
            task?.StepNumber,
            task?.ToolName,
            task?.Thought,
            task?.Status);
    }

    private static ConversationTask? FindTaskForBacklog(IEnumerable<ConversationTask> tasks, FictionPlanBacklogItem backlog)
        => tasks.FirstOrDefault(t => string.Equals(t.BacklogItemId, backlog.BacklogId, StringComparison.OrdinalIgnoreCase));

    private static JsonElement? TryParseJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractString(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var obj = element.Value;
        if (!obj.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static IReadOnlyList<string>? ExtractStringArray(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var obj = element.Value;
        if (!obj.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var str = item.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    values.Add(str);
                }
            }
        }

        return values.Count == 0 ? null : values.AsReadOnly();
    }

    private static BranchContext ResolveBranchContext(JsonElement? metadata, string? defaultBranch)
        => BuildBranchContext(ExtractString(metadata, "branchSlug"), ExtractStringArray(metadata, "branchLineage"), defaultBranch);

    private static BranchContext ResolveBranchContext(string? branchSlug, IReadOnlyList<string>? branchLineage, string? defaultBranch)
        => BuildBranchContext(branchSlug, branchLineage, defaultBranch);

    private static BranchContext BuildBranchContext(string? slug, IReadOnlyList<string>? lineage, string? defaultBranch)
    {
        var fallback = string.IsNullOrWhiteSpace(defaultBranch) ? "main" : defaultBranch.Trim();
        var resolvedSlug = string.IsNullOrWhiteSpace(slug) ? fallback : slug.Trim();
        IReadOnlyList<string>? resolvedLineage = null;

        if (lineage is { Count: > 0 })
        {
            resolvedLineage = lineage.ToArray();
        }
        else if (!string.IsNullOrWhiteSpace(fallback) &&
                 !string.Equals(fallback, resolvedSlug, StringComparison.OrdinalIgnoreCase))
        {
            resolvedLineage = new[] { fallback, resolvedSlug };
        }

        return new BranchContext(resolvedSlug, resolvedLineage);
    }

    private static string UpdateLoreMetadata(string? existingMetadata, BranchContext branchContext, FulfillLoreRequirementRequest request)
    {
        var metadata = DeserializeMetadata(existingMetadata);
        metadata["branchSlug"] = branchContext.Slug;
        if (branchContext.Lineage is { Count: > 0 })
        {
            metadata["branchLineage"] = branchContext.Lineage.ToArray();
        }
        metadata["fulfilledAtUtc"] = DateTime.UtcNow;
        metadata["fulfilledSource"] = string.IsNullOrWhiteSpace(request.Source) ? "api" : request.Source;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            metadata["fulfillmentNotes"] = request.Notes;
        }

        return JsonSerializer.Serialize(metadata, MetadataSerializerOptions);
    }

    private static Dictionary<string, object?> DeserializeArgs(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, MetadataSerializerOptions);
            return parsed is null
                ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object?>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void SetArg(Dictionary<string, object?> args, string key, object? value)
    {
        if (value is null)
        {
            args.Remove(key);
            return;
        }

        args[key] = value;
    }

    private async Task EmitLoreFulfillmentTelemetry(
        FictionPlan plan,
        FictionLoreRequirement requirement,
        BranchContext branchContext,
        FulfillLoreRequirementRequest request,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["fulfilledAtUtc"] = DateTime.UtcNow,
            ["fulfilledSource"] = string.IsNullOrWhiteSpace(request.Source) ? "api" : request.Source
        };

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            metadata["fulfillmentNotes"] = request.Notes;
        }

        var descriptor = new LoreRequirementDescriptor(
            requirement.Title,
            requirement.RequirementSlug,
            requirement.Status,
            requirement.ChapterScrollId,
            requirement.ChapterSceneId,
            requirement.WorldBibleEntryId,
            requirement.CreatedByPlanPassId,
            requirement.Description,
            requirement.Notes,
            metadata);

        var lifecycleRequest = new CharacterLifecycleRequest(
            plan.Id,
            request.ConversationId,
            request.PlanPassId,
            Array.Empty<CharacterLifecycleDescriptor>(),
            new[] { descriptor },
            Source: request.Source ?? "api:fulfillment",
            BranchSlug: branchContext.Slug,
            BranchLineage: branchContext.Lineage);

        await _lifecycleService.ProcessAsync(lifecycleRequest, cancellationToken).ConfigureAwait(false);
    }

    private static Dictionary<string, object?> DeserializeMetadata(string? existingMetadata)
    {
        if (string.IsNullOrWhiteSpace(existingMetadata))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(existingMetadata, MetadataSerializerOptions);
            return parsed is null
                ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object?>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private sealed record BranchContext(string Slug, IReadOnlyList<string>? Lineage);

    public sealed record FictionPlanRosterResponse(
        Guid PlanId,
        string PlanName,
        string? ProjectTitle,
        string? BranchSlug,
        IReadOnlyList<CharacterRosterItem> Characters,
        IReadOnlyList<LoreRequirementRosterItem> LoreRequirements);

    public sealed record LoreBranchSummary(
        string BranchSlug,
        IReadOnlyList<string>? BranchLineage,
        int Ready,
        int Blocked,
        int Planned);

    public sealed record AuthorPersonaContextResponse(
        Guid PersonaId,
        string PersonaName,
        string Summary,
        IReadOnlyList<string> Memories,
        IReadOnlyList<string> WorldNotes);

    public sealed record BacklogItemResponse(
        Guid Id,
        string BacklogId,
        string Description,
        FictionPlanBacklogStatus Status,
        IReadOnlyList<string>? Inputs,
        IReadOnlyList<string>? Outputs,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        Guid? ConversationPlanId,
        Guid? TaskId,
        int? StepNumber,
        string? ToolName,
        string? Thought,
        string? TaskStatus);

    public sealed record FictionPlanSummary(
        Guid Id,
        string Name,
        string? ProjectTitle,
        FictionPlanStatus Status);

    public sealed record CharacterRosterItem(
        Guid Id,
        string Slug,
        string DisplayName,
        string Role,
        string Importance,
        string? Summary,
        string? Notes,
        Guid? PersonaId,
        PersonaSummary? Persona,
        Guid? AgentId,
        AgentSummary? Agent,
        Guid? WorldBibleEntryId,
        WorldBibleEntrySummary? WorldBible,
        Guid? FirstSceneId,
        Guid? CreatedByPlanPassId,
        string? BranchSlug,
        IReadOnlyList<string>? BranchLineage,
        JsonElement? Provenance,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);

    public sealed record PersonaSummary(
        Guid Id,
        string Name,
        string Role,
        string Voice,
        string Essence,
        string Background,
        string CommunicationStyle);

    public sealed record AgentSummary(Guid Id, Guid PersonaId, bool RolePlay);

    public sealed record WorldBibleEntrySummary(
        Guid EntryId,
        Guid WorldBibleId,
        string Domain,
        string EntrySlug,
        string EntryName,
        string Category,
        string Summary,
        string Status,
        IReadOnlyList<string> ContinuityNotes,
        DateTime UpdatedAtUtc);

    public sealed record LoreRequirementRosterItem(
        Guid Id,
        string Title,
        string RequirementSlug,
        FictionLoreRequirementStatus Status,
        string? Description,
        string? Notes,
        Guid? WorldBibleEntryId,
        WorldBibleEntrySummary? WorldBible,
        JsonElement? Metadata,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        Guid? CreatedByPlanPassId,
        Guid? ChapterScrollId,
        Guid? ChapterSceneId,
        string? BranchSlug,
        IReadOnlyList<string>? BranchLineage);

    public sealed record FulfillLoreRequirementRequest(
        Guid? WorldBibleEntryId,
        string? Notes,
        Guid? ConversationId,
        Guid? PlanPassId,
        string? BranchSlug,
        IReadOnlyList<string>? BranchLineage,
        string? Source);

    public sealed record ResumeBacklogRequest(
        [property: Required] Guid ConversationId,
        [property: Required] Guid ConversationPlanId,
        [property: Required] Guid AgentId,
        [property: Required] Guid ProviderId,
        Guid? ModelId,
        [property: Required] Guid TaskId,
        string? BranchSlug);
}
