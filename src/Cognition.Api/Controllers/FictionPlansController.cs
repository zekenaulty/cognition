using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Obligations;
using Cognition.Api.Infrastructure.Planning;
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
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

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
    private readonly IFictionPlanCreator _planCreator;
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public FictionPlansController(
        CognitionDbContext db,
        ICharacterLifecycleService lifecycleService,
        IAuthorPersonaRegistry authorPersonaRegistry,
        IFictionBacklogScheduler backlogScheduler,
        IFictionPlanCreator planCreator)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _lifecycleService = lifecycleService ?? throw new ArgumentNullException(nameof(lifecycleService));
        _authorPersonaRegistry = authorPersonaRegistry ?? throw new ArgumentNullException(nameof(authorPersonaRegistry));
        _backlogScheduler = backlogScheduler ?? throw new ArgumentNullException(nameof(backlogScheduler));
        _planCreator = planCreator ?? throw new ArgumentNullException(nameof(planCreator));
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

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.UserOrHigher)]
    public async Task<ActionResult<FictionPlanSummary>> CreatePlan(
        [FromBody] CreateFictionPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        try
        {
            var options = new FictionPlanCreationOptions(
                request.ProjectId,
                request.ProjectTitle,
                request.ProjectLogline,
                request.Name,
                request.Description,
                request.BranchSlug,
                request.PersonaId,
                request.AgentId);

            var plan = await _planCreator.CreatePlanAsync(options, cancellationToken).ConfigureAwait(false);
            var summary = new FictionPlanSummary(plan.Id, plan.Name, plan.FictionProject?.Title, plan.Status);
            return CreatedAtAction(nameof(GetBacklog), new { planId = plan.Id }, summary);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
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

    [HttpGet("{planId:guid}/backlog/actions")]
    public async Task<ActionResult<IReadOnlyList<BacklogActionLogResponse>>> GetBacklogActions(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _db.FictionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
        {
            return NotFound();
        }

        var events = await _db.WorkflowEvents
            .AsNoTracking()
            .Where(e => e.Kind == "fiction.backlog.action")
            .OrderByDescending(e => e.Timestamp)
            .Take(200)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (events.Count == 0)
        {
            return Ok(Array.Empty<BacklogActionLogResponse>());
        }

        var logs = new List<BacklogActionLogResponse>();
        foreach (var evt in events)
        {
            if (evt.Payload is not JObject payload)
            {
                continue;
            }

            var payloadPlanId = TryReadGuid(payload, "planId");
            if (!payloadPlanId.HasValue || payloadPlanId.Value != plan.Id)
            {
                continue;
            }

            logs.Add(new BacklogActionLogResponse(
                Action: payload.Value<string>("action") ?? "unknown",
                BacklogId: payload.Value<string>("backlogId") ?? "(unknown)",
                Description: payload.Value<string>("description"),
                Branch: payload.Value<string>("branch") ?? plan.PrimaryBranchSlug ?? "main",
                Actor: payload.Value<string>("actor"),
                ActorId: payload.Value<string>("actorId"),
                Source: payload.Value<string>("source") ?? "api",
                ProviderId: TryReadGuid(payload, "providerId"),
                ModelId: TryReadGuid(payload, "modelId"),
                AgentId: TryReadGuid(payload, "agentId"),
                Status: payload.Value<string>("status"),
                ConversationId: TryReadGuid(payload, "conversationId"),
                ConversationPlanId: TryReadGuid(payload, "conversationPlanId"),
                TaskId: TryReadGuid(payload, "taskId"),
                TimestampUtc: evt.Timestamp));

            if (logs.Count >= 50)
            {
                break;
            }
        }

        return Ok(logs);
    }

    [HttpGet("{planId:guid}/lore/history")]
    public async Task<ActionResult<IReadOnlyList<LoreFulfillmentLogResponse>>> GetLoreHistory(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _db.FictionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
        {
            return NotFound();
        }

        var events = await _db.WorkflowEvents
            .AsNoTracking()
            .Where(e => e.Kind == "fiction.lore.fulfillment")
            .OrderByDescending(e => e.Timestamp)
            .Take(200)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (events.Count == 0)
        {
            return Ok(Array.Empty<LoreFulfillmentLogResponse>());
        }

        var logs = new List<LoreFulfillmentLogResponse>();
        foreach (var evt in events)
        {
            if (evt.Payload is not JObject payload)
            {
                continue;
            }

            var payloadPlanId = TryReadGuid(payload, "planId");
            if (!payloadPlanId.HasValue || payloadPlanId.Value != plan.Id)
            {
                continue;
            }

            var requirementId = TryReadGuid(payload, "requirementId");
            if (!requirementId.HasValue)
            {
                continue;
            }

            logs.Add(new LoreFulfillmentLogResponse(
                requirementId.Value,
                payload.Value<string>("requirementSlug") ?? "(unknown)",
                payload.Value<string>("action") ?? "fulfilled",
                payload.Value<string>("branch") ?? plan.PrimaryBranchSlug ?? "main",
                payload.Value<string>("actor"),
                payload.Value<string>("actorId"),
                payload.Value<string>("source") ?? "api",
                TryReadGuid(payload, "worldBibleEntryId"),
                payload.Value<string>("notes"),
                payload.Value<string>("status"),
                payload.Value<string>("conversationId"),
                payload.Value<string>("planPassId"),
                evt.Timestamp));

            if (logs.Count >= 100)
            {
                break;
            }
        }

        return Ok(logs);
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

        if (request.AgentId == Guid.Empty || request.ProviderId == Guid.Empty)
        {
            return BadRequest("AgentId and ProviderId are required to resume a backlog item.");
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

        await LogBacklogActionAsync(plan, backlog, branchSlug, request, cancellationToken).ConfigureAwait(false);

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
        await LogLoreFulfillmentEventAsync(plan, requirement, branchContext, request, cancellationToken).ConfigureAwait(false);

        return Ok(MapLoreRequirement(requirement, plan.PrimaryBranchSlug));
    }

    [HttpGet("{planId:guid}/persona-obligations")]
    public async Task<ActionResult<PersonaObligationListResponse>> GetPersonaObligations(
        Guid planId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var planExists = await _db.FictionPlans
            .AsNoTracking()
            .AnyAsync(p => p.Id == planId, cancellationToken)
            .ConfigureAwait(false);

        if (!planExists)
        {
            return NotFound();
        }

        if (page <= 0)
        {
            return BadRequest("Page must be greater than zero.");
        }

        if (pageSize <= 0 || pageSize > 200)
        {
            return BadRequest("Page size must be between 1 and 200.");
        }

        var query = _db.FictionPersonaObligations
            .AsNoTracking()
            .Include(o => o.Persona)
            .Include(o => o.FictionCharacter)
            .Where(o => o.FictionPlanId == planId);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var obligations = await query
            .OrderBy(o => o.Status)
            .ThenBy(o => o.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = obligations.Select(MapPersonaObligation).ToList();
        return Ok(new PersonaObligationListResponse(items, totalCount, page, pageSize));
    }

    [HttpPost("{planId:guid}/persona-obligations/{obligationId:guid}/resolve")]
    public async Task<ActionResult<PersonaObligationResponse>> ResolvePersonaObligation(
        Guid planId,
        Guid obligationId,
        [FromBody] ResolvePersonaObligationRequest request,
        CancellationToken cancellationToken)
    {
        var obligation = await _db.FictionPersonaObligations
            .Include(o => o.Persona)
            .Include(o => o.FictionCharacter)
            .Include(o => o.FictionPlan)
            .FirstOrDefaultAsync(o => o.FictionPlanId == planId && o.Id == obligationId, cancellationToken)
            .ConfigureAwait(false);

        if (obligation is null)
        {
            return NotFound();
        }

        var requestedAction = (request.Action ?? "resolve").Trim().ToLowerInvariant();
        var targetStatus = requestedAction switch
        {
            "dismiss" or "dismissed" => FictionPersonaObligationStatus.Dismissed,
            _ => FictionPersonaObligationStatus.Resolved
        };
        var actionPayload = targetStatus == FictionPersonaObligationStatus.Dismissed ? "dismissed" : "resolved";

        if (obligation.Status == targetStatus)
        {
            return Ok(MapPersonaObligation(obligation));
        }

        var metadata = PersonaObligationMetadata.FromJson(obligation.MetadataJson);
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            metadata.AddResolutionNote(request.Notes!, DateTime.UtcNow, ResolveActorName(HttpContext));
        }
        metadata.SetResolvedSource(request.Source ?? "api");

        obligation.Status = targetStatus;
        obligation.ResolvedAtUtc = DateTime.UtcNow;
        obligation.ResolvedByActor = ResolveActorName(HttpContext);
        obligation.MetadataJson = metadata.Serialize();
        obligation.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await LogPersonaObligationEventAsync(obligation.FictionPlanId, obligation, actionPayload, request, cancellationToken).ConfigureAwait(false);

        return Ok(MapPersonaObligation(obligation));
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

    private PersonaObligationResponse MapPersonaObligation(FictionPersonaObligation entity)
    {
        var metadata = TryParseJson(entity.MetadataJson);
        var personaName = entity.Persona?.Name ?? $"Persona {entity.PersonaId:N}";
        var characterSlug = entity.FictionCharacter?.Slug;
        var branchSlug = string.IsNullOrWhiteSpace(entity.BranchSlug)
            ? TryReadMetadataString(metadata, "branchSlug")
            : entity.BranchSlug;
        var branchLineage = ExtractBranchLineage(metadata);
        var sourceBacklogId = entity.SourceBacklogId ?? TryReadMetadataString(metadata, "sourceBacklogId");

        return new PersonaObligationResponse(
            entity.Id,
            entity.Title,
            entity.Description,
            entity.Status,
            entity.SourcePhase,
            branchSlug,
            branchLineage,
            entity.PersonaId,
            personaName,
            entity.FictionCharacterId,
            characterSlug,
            sourceBacklogId,
            metadata,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.ResolvedAtUtc);
    }

    private static BacklogItemResponse MapBacklogItem(FictionPlanBacklogItem entity, IEnumerable<ConversationTask> tasks)
    {
        var task = FindTaskForBacklog(tasks, entity);
        var args = task is null ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) : DeserializeArgs(task.ArgsJson);

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
            TryGetGuidArg(args, "conversationId"),
            TryGetGuidArg(args, "agentId"),
            TryGetGuidArg(args, "providerId"),
            TryGetGuidArg(args, "modelId"),
            TryGetStringArg(args, "branchSlug"),
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

    private static string? TryReadMetadataString(JsonElement? metadata, string propertyName)
    {
        if (!TryGetMetadataProperty(metadata, propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static IReadOnlyList<string>? ExtractBranchLineage(JsonElement? metadata)
    {
        if (!TryGetMetadataProperty(metadata, "branchLineage", out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var lineage = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    lineage.Add(value);
                }
            }
        }

        return lineage.Count == 0 ? null : lineage;
    }

    private static bool TryGetMetadataProperty(JsonElement? metadata, string propertyName, out JsonElement value)
    {
        value = default;
        if (metadata is null || metadata.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return metadata.Value.TryGetProperty(propertyName, out value);
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

    private async Task LogBacklogActionAsync(
        FictionPlan plan,
        FictionPlanBacklogItem backlog,
        string branchSlug,
        ResumeBacklogRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new JObject
        {
            ["planId"] = plan.Id,
            ["planName"] = plan.Name,
            ["backlogId"] = backlog.BacklogId,
            ["description"] = backlog.Description,
            ["action"] = "resume",
            ["branch"] = string.IsNullOrWhiteSpace(branchSlug) ? "main" : branchSlug,
            ["status"] = backlog.Status.ToString(),
            ["conversationId"] = request.ConversationId,
            ["conversationPlanId"] = request.ConversationPlanId,
            ["taskId"] = request.TaskId,
            ["providerId"] = request.ProviderId,
            ["agentId"] = request.AgentId
        };

        if (request.ModelId.HasValue)
        {
            payload["modelId"] = request.ModelId.Value;
        }

        var httpContext = HttpContext ?? ControllerContext?.HttpContext;
        var actorId = ResolveActorId(httpContext);
        if (!string.IsNullOrWhiteSpace(actorId))
        {
            payload["actorId"] = actorId;
        }

        var actorName = ResolveActorName(httpContext);
        if (!string.IsNullOrWhiteSpace(actorName))
        {
            payload["actor"] = actorName;
        }

        payload["source"] = ResolveBacklogActionSource(httpContext);

        _db.WorkflowEvents.Add(new WorkflowEvent
        {
            ConversationId = request.ConversationId,
            Kind = "fiction.backlog.action",
            Payload = payload,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task LogPersonaObligationEventAsync(
        Guid planId,
        FictionPersonaObligation obligation,
        string action,
        ResolvePersonaObligationRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new JObject
        {
            ["planId"] = planId,
            ["planName"] = obligation.FictionPlan?.Name,
            ["obligationId"] = obligation.Id,
            ["personaId"] = obligation.PersonaId,
            ["personaName"] = obligation.Persona?.Name,
            ["characterId"] = obligation.FictionCharacterId,
            ["characterSlug"] = obligation.FictionCharacter?.Slug,
            ["action"] = action,
            ["notes"] = request.Notes,
            ["source"] = string.IsNullOrWhiteSpace(request.Source) ? "api" : request.Source,
            ["status"] = obligation.Status.ToString(),
            ["branch"] = obligation.BranchSlug,
            ["sourceBacklogId"] = obligation.SourceBacklogId,
            ["resolvedAtUtc"] = obligation.ResolvedAtUtc
        };

        var actorId = ResolveActorId(HttpContext);
        if (!string.IsNullOrWhiteSpace(actorId))
        {
            payload["actorId"] = actorId;
        }

        var actorName = ResolveActorName(HttpContext);
        if (!string.IsNullOrWhiteSpace(actorName))
        {
            payload["actor"] = actorName;
        }

        _db.WorkflowEvents.Add(new WorkflowEvent
        {
            ConversationId = obligation.SourceConversationId ?? Guid.Empty,
            Kind = "fiction.persona.obligation",
            Payload = payload,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string? ResolveActorId(HttpContext? context)
    {
        if (context?.User is null)
        {
            return null;
        }

        return context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
    }

    private static string? ResolveActorName(HttpContext? context)
    {
        if (context?.User is null)
        {
            return null;
        }

        return context.User.Identity?.Name
            ?? context.User.FindFirstValue("name")
            ?? context.User.FindFirstValue(ClaimTypes.Name);
    }

    private static string ResolveBacklogActionSource(HttpContext? context)
    {
        if (context is null)
        {
            return "api";
        }

        if (context.Request.Headers.TryGetValue("X-Console-Client", out var client) &&
            !StringValues.IsNullOrEmpty(client))
        {
            return client.ToString();
        }

        return "api";
    }

    private async Task LogLoreFulfillmentEventAsync(
        FictionPlan plan,
        FictionLoreRequirement requirement,
        BranchContext branchContext,
        FulfillLoreRequirementRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new JObject
        {
            ["planId"] = plan.Id,
            ["planName"] = plan.Name,
            ["requirementId"] = requirement.Id,
            ["requirementSlug"] = requirement.RequirementSlug,
            ["title"] = requirement.Title,
            ["action"] = "fulfilled",
            ["branch"] = branchContext.Slug,
            ["status"] = requirement.Status.ToString(),
            ["notes"] = request.Notes,
            ["worldBibleEntryId"] = requirement.WorldBibleEntryId ?? request.WorldBibleEntryId,
            ["source"] = string.IsNullOrWhiteSpace(request.Source) ? "api" : request.Source
        };

        if (!string.IsNullOrWhiteSpace(request.BranchSlug))
        {
            payload["requestedBranch"] = request.BranchSlug;
        }

        if (request.ConversationId.HasValue)
        {
            payload["conversationId"] = request.ConversationId.Value;
        }

        if (request.PlanPassId.HasValue)
        {
            payload["planPassId"] = request.PlanPassId.Value;
        }

        var actorId = ResolveActorId(HttpContext ?? ControllerContext?.HttpContext);
        if (!string.IsNullOrWhiteSpace(actorId))
        {
            payload["actorId"] = actorId;
        }

        var actorName = ResolveActorName(HttpContext ?? ControllerContext?.HttpContext);
        if (!string.IsNullOrWhiteSpace(actorName))
        {
            payload["actor"] = actorName;
        }

        _db.WorkflowEvents.Add(new WorkflowEvent
        {
            ConversationId = request.ConversationId ?? Guid.Empty,
            Kind = "fiction.lore.fulfillment",
            Payload = payload,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Guid? TryReadGuid(JObject payload, string propertyName)
    {
        if (!payload.TryGetValue(propertyName, out var token) || token is null || token.Type == JTokenType.Null)
        {
            return null;
        }

        if (token.Type == JTokenType.Guid)
        {
            return token.Value<Guid>();
        }

        var value = token.ToString();
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private static Guid? TryGetGuidArg(IReadOnlyDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            Guid guid => guid,
            string str when Guid.TryParse(str, out var parsed) => parsed,
            JsonElement element when element.ValueKind == JsonValueKind.String && Guid.TryParse(element.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static string? TryGetStringArg(IReadOnlyDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string str when !string.IsNullOrWhiteSpace(str) => str,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            _ => null
        };
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

    public sealed record CreateFictionPlanRequest(
        Guid? ProjectId,
        string? ProjectTitle,
        string? ProjectLogline,
        [property: Required] string Name,
        string? Description,
        string? BranchSlug,
        [property: Required] Guid PersonaId,
        Guid? AgentId);

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
        Guid? ConversationId,
        Guid? AgentId,
        Guid? ProviderId,
        Guid? ModelId,
        string? BranchSlug,
        Guid? TaskId,
        int? StepNumber,
        string? ToolName,
        string? Thought,
        string? TaskStatus);

    public sealed record BacklogActionLogResponse(
        string Action,
        string BacklogId,
        string? Description,
        string Branch,
        string? Actor,
        string? ActorId,
        string Source,
        Guid? ProviderId,
        Guid? ModelId,
        Guid? AgentId,
        string? Status,
        Guid? ConversationId,
        Guid? ConversationPlanId,
        Guid? TaskId,
        DateTime TimestampUtc);

    public sealed record LoreFulfillmentLogResponse(
        Guid RequirementId,
        string RequirementSlug,
        string Action,
        string Branch,
        string? Actor,
        string? ActorId,
        string Source,
        Guid? WorldBibleEntryId,
        string? Notes,
        string? Status,
        string? ConversationId,
        string? PlanPassId,
        DateTime TimestampUtc);

    public sealed record PersonaObligationListResponse(
        IReadOnlyList<PersonaObligationResponse> Items,
        int TotalCount,
        int Page,
        int PageSize);

    public sealed record PersonaObligationResponse(
        Guid Id,
        string Title,
        string? Description,
        FictionPersonaObligationStatus Status,
        string? SourcePhase,
        string? BranchSlug,
        IReadOnlyList<string>? BranchLineage,
        Guid PersonaId,
        string PersonaName,
        Guid? FictionCharacterId,
        string? CharacterSlug,
        string? SourceBacklogId,
        JsonElement? Metadata,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        DateTime? ResolvedAtUtc);

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

    public sealed record ResolvePersonaObligationRequest(string? Notes, string? Source, string? Action);
}
