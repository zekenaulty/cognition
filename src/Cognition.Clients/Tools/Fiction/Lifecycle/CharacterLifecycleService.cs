using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Fiction;
using Cognition.Data.Relational.Modules.Personas;
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
    private const string CharacterWorldBibleCategory = "characters";

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
        var personaCache = new Dictionary<Guid, Persona>();
        var agentCache = new Dictionary<Guid, Agent>();
        var worldBibleLookup = await LoadCharacterWorldBibleEntriesAsync(request.PlanId, slugSet, cancellationToken).ConfigureAwait(false);

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
            entity.FirstSceneId = descriptor.FirstSceneId ?? entity.FirstSceneId;
            entity.CreatedByPlanPassId ??= descriptor.CreatedByPlanPassId ?? request.PlanPassId;

            var (persona, personaCreated) = await EnsurePersonaAsync(descriptor, entity, personaCache, cancellationToken).ConfigureAwait(false);
            var agent = await EnsureAgentAsync(descriptor, entity, persona, agentCache, cancellationToken).ConfigureAwait(false);

            var resolvedWorldBibleEntryId = descriptor.WorldBibleEntryId
                ?? entity.WorldBibleEntryId
                ?? TryResolveWorldBibleEntryId(slug, worldBibleLookup);
            if (resolvedWorldBibleEntryId.HasValue)
            {
                entity.WorldBibleEntryId = resolvedWorldBibleEntryId;
            }

            var metadata = BuildCharacterMetadata(request, descriptor, persona, agent, slug);
            var provenance = SerializeMetadata(metadata);
            if (!string.IsNullOrWhiteSpace(provenance))
            {
                entity.ProvenanceJson = provenance;
            }
            entity.UpdatedAtUtc = DateTime.UtcNow;

            if (personaCreated)
            {
                await CreatePersonaMemoryAsync(persona, descriptor, request, slug, entity.WorldBibleEntryId, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                isNew ? "FictionCharacterPromoted plan={PlanId} slug={Slug} persona={PersonaId}" : "FictionCharacterUpdated plan={PlanId} slug={Slug} persona={PersonaId}",
                request.PlanId,
                slug,
                entity.PersonaId);
        }
    }

    private async Task<IReadOnlyDictionary<string, FictionWorldBibleEntry>> LoadCharacterWorldBibleEntriesAsync(
        Guid planId,
        IReadOnlyCollection<string> slugs,
        CancellationToken cancellationToken)
    {
        if (slugs.Count == 0)
        {
            return new Dictionary<string, FictionWorldBibleEntry>(StringComparer.OrdinalIgnoreCase);
        }

        var bibleIds = await _db.FictionWorldBibles
            .Where(b => b.FictionPlanId == planId)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (bibleIds.Count == 0)
        {
            return new Dictionary<string, FictionWorldBibleEntry>(StringComparer.OrdinalIgnoreCase);
        }

        var prefixedSlugs = slugs.Select(BuildWorldBibleCharacterSlug).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var entries = await _db.FictionWorldBibleEntries
            .Where(e => bibleIds.Contains(e.FictionWorldBibleId) && prefixedSlugs.Contains(e.EntrySlug))
            .OrderByDescending(e => e.Sequence)
            .ThenByDescending(e => e.UpdatedAtUtc ?? e.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var lookup = new Dictionary<string, FictionWorldBibleEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var slug = ExtractCharacterSlug(entry.EntrySlug);
            if (string.IsNullOrEmpty(slug))
            {
                continue;
            }

            if (!lookup.ContainsKey(slug))
            {
                lookup[slug] = entry;
            }
        }

        return lookup;
    }

    private async Task<(Persona Persona, bool Created)> EnsurePersonaAsync(
        CharacterLifecycleDescriptor descriptor,
        FictionCharacter entity,
        Dictionary<Guid, Persona> cache,
        CancellationToken cancellationToken)
    {
        var personaId = descriptor.PersonaId ?? entity.PersonaId;
        Persona? persona = null;
        if (personaId.HasValue)
        {
            persona = await LoadPersonaAsync(personaId.Value, cache, cancellationToken).ConfigureAwait(false);
        }

        var created = false;
        if (persona is null)
        {
            var id = personaId ?? Guid.NewGuid();
            persona = new Persona
            {
                Id = id,
                Name = descriptor.Name,
                Nickname = descriptor.Name,
                Role = descriptor.Role ?? "Fiction Character",
                Type = PersonaType.RolePlayCharacter,
                OwnedBy = OwnedBy.System,
                IsPublic = false,
                Background = descriptor.Summary ?? string.Empty,
                Essence = descriptor.Importance ?? string.Empty,
                Voice = descriptor.Notes ?? string.Empty,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.Personas.Add(persona);
            cache[id] = persona;
            created = true;
        }
        else
        {
            var updated = false;
            if (!string.Equals(persona.Name, descriptor.Name, StringComparison.Ordinal))
            {
                persona.Name = descriptor.Name;
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(persona.Nickname))
            {
                persona.Nickname = descriptor.Name;
                updated = true;
            }
            if (!string.IsNullOrWhiteSpace(descriptor.Role) && !string.Equals(persona.Role, descriptor.Role, StringComparison.Ordinal))
            {
                persona.Role = descriptor.Role!;
                updated = true;
            }
            if (persona.Type != PersonaType.RolePlayCharacter)
            {
                persona.Type = PersonaType.RolePlayCharacter;
                updated = true;
            }
            if (persona.OwnedBy != OwnedBy.System)
            {
                persona.OwnedBy = OwnedBy.System;
                updated = true;
            }
            if (!string.IsNullOrWhiteSpace(descriptor.Summary) && !string.Equals(persona.Background, descriptor.Summary, StringComparison.Ordinal))
            {
                persona.Background = descriptor.Summary;
                updated = true;
            }
            if (!string.IsNullOrWhiteSpace(descriptor.Notes) && !string.Equals(persona.CommunicationStyle, descriptor.Notes, StringComparison.Ordinal))
            {
                persona.CommunicationStyle = descriptor.Notes!;
                updated = true;
            }
            if (!string.IsNullOrWhiteSpace(descriptor.Importance) && !string.Equals(persona.Essence, descriptor.Importance, StringComparison.Ordinal))
            {
                persona.Essence = descriptor.Importance!;
                updated = true;
            }

            if (updated)
            {
                persona.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        entity.PersonaId = persona.Id;
        return (persona, created);
    }

    private async Task<Persona?> LoadPersonaAsync(Guid personaId, IDictionary<Guid, Persona> cache, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(personaId, out var cached))
        {
            return cached;
        }

        var persona = await _db.Personas.FirstOrDefaultAsync(p => p.Id == personaId, cancellationToken).ConfigureAwait(false);
        if (persona is not null)
        {
            cache[personaId] = persona;
        }

        return persona;
    }

    private async Task<Agent> EnsureAgentAsync(
        CharacterLifecycleDescriptor descriptor,
        FictionCharacter entity,
        Persona persona,
        Dictionary<Guid, Agent> cache,
        CancellationToken cancellationToken)
    {
        var agentId = descriptor.AgentId ?? entity.AgentId;
        Agent? agent = null;
        if (agentId.HasValue)
        {
            agent = await LoadAgentAsync(agentId.Value, cache, cancellationToken).ConfigureAwait(false);
        }

        if (agent is null)
        {
            var id = agentId ?? Guid.NewGuid();
            agent = new Agent
            {
                Id = id,
                PersonaId = persona.Id,
                Persona = persona,
                RolePlay = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.Agents.Add(agent);
            cache[id] = agent;
        }
        else if (agent.PersonaId != persona.Id)
        {
            agent.PersonaId = persona.Id;
            agent.Persona = persona;
            agent.UpdatedAtUtc = DateTime.UtcNow;
        }

        entity.AgentId = agent.Id;
        return agent;
    }

    private async Task<Agent?> LoadAgentAsync(Guid agentId, IDictionary<Guid, Agent> cache, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(agentId, out var cached))
        {
            return cached;
        }

        var agent = await _db.Agents.FirstOrDefaultAsync(a => a.Id == agentId, cancellationToken).ConfigureAwait(false);
        if (agent is not null)
        {
            cache[agentId] = agent;
        }

        return agent;
    }

    private Task CreatePersonaMemoryAsync(
        Persona persona,
        CharacterLifecycleDescriptor descriptor,
        CharacterLifecycleRequest request,
        string slug,
        Guid? worldBibleEntryId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var content = BuildPersonaMemoryContent(descriptor);
        if (string.IsNullOrWhiteSpace(content))
        {
            content = $"Character dossier for {descriptor.Name}.";
        }

        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["planId"] = request.PlanId,
            ["characterSlug"] = slug,
            ["source"] = request.Source ?? "lifecycle",
            ["worldBibleEntryId"] = worldBibleEntryId,
            ["createdByPlanPassId"] = descriptor.CreatedByPlanPassId ?? request.PlanPassId
        };

        if (descriptor.Metadata is not null && descriptor.Metadata.Count > 0)
        {
            properties["descriptor"] = descriptor.Metadata;
        }

        var memory = new PersonaMemory
        {
            Id = Guid.NewGuid(),
            PersonaId = persona.Id,
            Title = $"{descriptor.Name} dossier",
            Content = content,
            Source = request.Source ?? "lifecycle",
            OccurredAtUtc = DateTime.UtcNow,
            Properties = properties
        };

        _db.PersonaMemories.Add(memory);
        return Task.CompletedTask;
    }

    private static string BuildPersonaMemoryContent(CharacterLifecycleDescriptor descriptor)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(descriptor.Summary))
        {
            builder.AppendLine($"Summary: {descriptor.Summary.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(descriptor.Role))
        {
            builder.AppendLine($"Role: {descriptor.Role.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(descriptor.Importance))
        {
            builder.AppendLine($"Importance: {descriptor.Importance.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(descriptor.Notes))
        {
            builder.AppendLine($"Notes: {descriptor.Notes.Trim()}");
        }

        return builder.ToString().Trim();
    }

    private static IReadOnlyDictionary<string, object?>? BuildCharacterMetadata(
        CharacterLifecycleRequest request,
        CharacterLifecycleDescriptor descriptor,
        Persona persona,
        Agent agent,
        string slug)
    {
        Dictionary<string, object?>? metadata = null;
        if (descriptor.Metadata is not null && descriptor.Metadata.Count > 0)
        {
            metadata = new Dictionary<string, object?>(descriptor.Metadata, StringComparer.OrdinalIgnoreCase);
        }

        void AddValue(string key, object? value)
        {
            if (value is null)
            {
                return;
            }

            metadata ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            metadata[key] = value;
        }

        AddValue("planId", request.PlanId);
        AddValue("planPassId", descriptor.CreatedByPlanPassId ?? request.PlanPassId);
        AddValue("characterSlug", slug);
        AddValue("source", request.Source);
        AddValue("personaId", persona.Id);
        AddValue("agentId", agent.Id);
        AddValue("importance", descriptor.Importance);
        AddValue("role", descriptor.Role);
        AddValue("track", descriptor.Track);

        return metadata;
    }

    private static Guid? TryResolveWorldBibleEntryId(string slug, IReadOnlyDictionary<string, FictionWorldBibleEntry> lookup)
        => lookup.TryGetValue(slug, out var entry) ? entry.Id : null;

    private static string BuildWorldBibleCharacterSlug(string slug)
        => $"{CharacterWorldBibleCategory}:{slug}";

    private static string ExtractCharacterSlug(string? entrySlug)
    {
        if (string.IsNullOrWhiteSpace(entrySlug))
        {
            return string.Empty;
        }

        var parts = entrySlug.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return string.Empty;
        }

        return parts[0].Equals(CharacterWorldBibleCategory, StringComparison.OrdinalIgnoreCase) ? parts[1] : string.Empty;
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
