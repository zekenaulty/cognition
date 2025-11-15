using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Fiction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools.Fiction.Lifecycle;

public sealed class CharacterLifecycleService : ICharacterLifecycleService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly CognitionDbContext _db;
    private readonly ILogger<CharacterLifecycleService> _logger;
    private readonly IReadOnlyList<IFictionLifecycleTelemetry> _telemetrySinks;

    public CharacterLifecycleService(
        CognitionDbContext db,
        ILogger<CharacterLifecycleService> logger,
        IEnumerable<IFictionLifecycleTelemetry>? telemetrySinks = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetrySinks = telemetrySinks?.ToArray() ?? Array.Empty<IFictionLifecycleTelemetry>();
    }

    public async Task<CharacterLifecycleResult> ProcessAsync(CharacterLifecycleRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Characters.Count == 0 && request.LoreRequirements.Count == 0)
        {
            return CharacterLifecycleResult.Empty;
        }

        var planExists = await _db.FictionPlans
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.PlanId, cancellationToken)
            .ConfigureAwait(false);

        if (!planExists)
        {
            throw new InvalidOperationException($"Fiction plan {request.PlanId} does not exist.");
        }

        var createdCharacters = new List<FictionCharacter>();
        var updatedCharacters = new List<FictionCharacter>();
        var upsertedRequirements = new List<FictionLoreRequirement>();

        await UpsertCharactersAsync(request, createdCharacters, updatedCharacters, cancellationToken).ConfigureAwait(false);
        await UpsertLoreRequirementsAsync(request, upsertedRequirements, cancellationToken).ConfigureAwait(false);

        if (createdCharacters.Count == 0 && updatedCharacters.Count == 0 && upsertedRequirements.Count == 0)
        {
            return CharacterLifecycleResult.Empty;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var result = new CharacterLifecycleResult(
            createdCharacters.AsReadOnly(),
            updatedCharacters.AsReadOnly(),
            upsertedRequirements.AsReadOnly());

        foreach (var sink in _telemetrySinks)
        {
            await sink.LifecycleProcessedAsync(request, result, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    private async Task UpsertCharactersAsync(
        CharacterLifecycleRequest request,
        List<FictionCharacter> created,
        List<FictionCharacter> updated,
        CancellationToken cancellationToken)
    {
        var tracked = request.Characters.Where(c => c.Track).ToList();
        if (tracked.Count == 0)
        {
            return;
        }

        var normalized = tracked
            .Select(descriptor =>
            {
                if (string.IsNullOrWhiteSpace(descriptor.Name))
                {
                    _logger.LogWarning("Skipping character with empty name for plan {PlanId}.", request.PlanId);
                    return (Slug: (string?)null, Descriptor: descriptor);
                }

                var slug = NormalizeSlug(descriptor.Slug ?? descriptor.Name);
                return (Slug: slug, Descriptor: descriptor);
            })
            .Where(tuple => !string.IsNullOrWhiteSpace(tuple.Slug))
            .ToList();

        if (normalized.Count == 0)
        {
            return;
        }

        var slugSet = normalized.Select(tuple => tuple.Slug!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var existing = await _db.FictionCharacters
            .Where(c => c.FictionPlanId == request.PlanId && slugSet.Contains(c.Slug))
            .ToDictionaryAsync(c => c.Slug, StringComparer.OrdinalIgnoreCase, cancellationToken)
            .ConfigureAwait(false);

        foreach (var (slug, descriptor) in normalized)
        {
            if (slug is null)
            {
                continue;
            }

            var isNew = false;
            if (!existing.TryGetValue(slug, out var entity))
            {
                entity = new FictionCharacter
                {
                    Id = Guid.NewGuid(),
                    FictionPlanId = request.PlanId,
                    Slug = slug,
                    DisplayName = descriptor.Name,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _db.FictionCharacters.Add(entity);
                existing[slug] = entity;
                created.Add(entity);
                isNew = true;
            }
            else
            {
                updated.Add(entity);
            }

            entity.DisplayName = descriptor.Name;
            entity.Role = descriptor.Role ?? entity.Role;
            entity.Importance = descriptor.Importance ?? entity.Importance;
            entity.Summary = descriptor.Summary ?? entity.Summary;
            entity.Notes = descriptor.Notes ?? entity.Notes;
            entity.PersonaId = descriptor.PersonaId ?? entity.PersonaId;
            entity.AgentId = descriptor.AgentId ?? entity.AgentId;
            entity.WorldBibleEntryId = descriptor.WorldBibleEntryId ?? entity.WorldBibleEntryId;
            entity.FirstSceneId = descriptor.FirstSceneId ?? entity.FirstSceneId;
            entity.CreatedByPlanPassId ??= descriptor.CreatedByPlanPassId ?? request.PlanPassId;

            var provenance = SerializeMetadata(descriptor.Metadata);
            if (!string.IsNullOrWhiteSpace(provenance))
            {
                entity.ProvenanceJson = provenance;
            }
            entity.UpdatedAtUtc = DateTime.UtcNow;

            _logger.LogInformation(
                isNew ? "FictionCharacterPromoted plan={PlanId} slug={Slug}" : "FictionCharacterUpdated plan={PlanId} slug={Slug}",
                request.PlanId,
                slug);
        }
    }

    private async Task UpsertLoreRequirementsAsync(
        CharacterLifecycleRequest request,
        List<FictionLoreRequirement> results,
        CancellationToken cancellationToken)
    {
        if (request.LoreRequirements.Count == 0)
        {
            return;
        }

        var normalized = request.LoreRequirements
            .Select(descriptor =>
            {
                if (string.IsNullOrWhiteSpace(descriptor.Title) && string.IsNullOrWhiteSpace(descriptor.RequirementSlug))
                {
                    _logger.LogWarning("Skipping lore requirement with empty title and slug for plan {PlanId}.", request.PlanId);
                    return (Slug: (string?)null, Descriptor: descriptor);
                }
                var baseValue = descriptor.RequirementSlug ?? descriptor.Title;
                var slug = NormalizeSlug(baseValue);
                return (Slug: slug, Descriptor: descriptor);
            })
            .Where(tuple => !string.IsNullOrWhiteSpace(tuple.Slug))
            .ToList();

        if (normalized.Count == 0)
        {
            return;
        }

        var slugSet = normalized.Select(tuple => tuple.Slug!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var existing = await _db.FictionLoreRequirements
            .Where(r => r.FictionPlanId == request.PlanId && slugSet.Contains(r.RequirementSlug))
            .ToDictionaryAsync(r => r.RequirementSlug, StringComparer.OrdinalIgnoreCase, cancellationToken)
            .ConfigureAwait(false);

        foreach (var (slug, descriptor) in normalized)
        {
            if (slug is null)
            {
                continue;
            }

            var isNew = false;
            if (!existing.TryGetValue(slug, out var entity))
            {
                entity = new FictionLoreRequirement
                {
                    Id = Guid.NewGuid(),
                    FictionPlanId = request.PlanId,
                    RequirementSlug = slug,
                    Title = descriptor.Title,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _db.FictionLoreRequirements.Add(entity);
                existing[slug] = entity;
                isNew = true;
            }

            entity.Title = descriptor.Title;
            entity.Status = descriptor.Status;
            entity.Description = descriptor.Description ?? entity.Description;
            entity.Notes = descriptor.Notes ?? entity.Notes;
            entity.ChapterScrollId = descriptor.ChapterScrollId ?? entity.ChapterScrollId;
            entity.ChapterSceneId = descriptor.ChapterSceneId ?? entity.ChapterSceneId;
            entity.WorldBibleEntryId = descriptor.WorldBibleEntryId ?? entity.WorldBibleEntryId;
            entity.CreatedByPlanPassId ??= descriptor.CreatedByPlanPassId ?? request.PlanPassId;
            var metadata = SerializeMetadata(descriptor.Metadata);
            if (!string.IsNullOrWhiteSpace(metadata))
            {
                entity.MetadataJson = metadata;
            }
            entity.UpdatedAtUtc = DateTime.UtcNow;

            results.Add(entity);
            _logger.LogInformation(
                isNew ? "FictionLoreRequirementCreated plan={PlanId} slug={Slug} status={Status}" : "FictionLoreRequirementUpdated plan={PlanId} slug={Slug} status={Status}",
                request.PlanId,
                slug,
                entity.Status);
        }
    }

    private static string NormalizeSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            builder.Append(char.IsLetterOrDigit(ch) ? ch : '-');
        }

        var slug = builder.ToString();
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? $"item-{Guid.NewGuid():N}" : slug;
    }

    private static string? SerializeMetadata(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(metadata, JsonOptions);
    }
}
