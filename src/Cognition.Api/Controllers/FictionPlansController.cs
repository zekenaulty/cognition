using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Security;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
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

    public FictionPlansController(CognitionDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
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
            characters.Select(MapCharacter).ToArray(),
            loreRequirements.Select(MapLoreRequirement).ToArray());

        return Ok(response);
    }

    private static CharacterRosterItem MapCharacter(FictionCharacter entity)
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
            TryParseJson(entity.ProvenanceJson),
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
    }

    private static LoreRequirementRosterItem MapLoreRequirement(FictionLoreRequirement entity)
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

        return new LoreRequirementRosterItem(
            entity.Id,
            entity.Title,
            entity.RequirementSlug,
            entity.Status,
            entity.Description,
            entity.Notes,
            entity.WorldBibleEntryId,
            worldBible,
            TryParseJson(entity.MetadataJson),
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.CreatedByPlanPassId,
            entity.ChapterScrollId,
            entity.ChapterSceneId);
    }

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

    public sealed record FictionPlanRosterResponse(
        Guid PlanId,
        string PlanName,
        string? ProjectTitle,
        string? BranchSlug,
        IReadOnlyList<CharacterRosterItem> Characters,
        IReadOnlyList<LoreRequirementRosterItem> LoreRequirements);

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
        Guid? ChapterSceneId);
}
