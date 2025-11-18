using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cognition.Data.Relational.Modules.Fiction;

namespace Cognition.Clients.Tools.Fiction.Lifecycle;

public sealed record CharacterLifecycleRequest(
    Guid PlanId,
    Guid? ConversationId,
    Guid? PlanPassId,
    IReadOnlyList<CharacterLifecycleDescriptor> Characters,
    IReadOnlyList<LoreRequirementDescriptor> LoreRequirements,
    string? Source = null,
    string? BranchSlug = null,
    IReadOnlyList<string>? BranchLineage = null)
{
    public static CharacterLifecycleRequest Empty(Guid planId)
        => new(planId, null, null, Array.Empty<CharacterLifecycleDescriptor>(), Array.Empty<LoreRequirementDescriptor>());
}

public sealed record CharacterLifecycleDescriptor(
    string Name,
    bool Track,
    string? Slug = null,
    Guid? PersonaId = null,
    Guid? AgentId = null,
    Guid? WorldBibleEntryId = null,
    Guid? FirstSceneId = null,
    Guid? CreatedByPlanPassId = null,
    string? Role = null,
    string? Importance = null,
    string? Summary = null,
    string? Notes = null,
    IReadOnlyDictionary<string, object?>? Metadata = null,
    IReadOnlyList<string>? ContinuityHooks = null);

public sealed record LoreRequirementDescriptor(
    string Title,
    string? RequirementSlug = null,
    FictionLoreRequirementStatus Status = FictionLoreRequirementStatus.Planned,
    Guid? ChapterScrollId = null,
    Guid? ChapterSceneId = null,
    Guid? WorldBibleEntryId = null,
    Guid? CreatedByPlanPassId = null,
    string? Description = null,
    string? Notes = null,
    IReadOnlyDictionary<string, object?>? Metadata = null);

public sealed record CharacterLifecycleResult(
    IReadOnlyList<FictionCharacter> CreatedCharacters,
    IReadOnlyList<FictionCharacter> UpdatedCharacters,
    IReadOnlyList<FictionLoreRequirement> UpsertedLoreRequirements)
{
    public static CharacterLifecycleResult Empty { get; } =
        new CharacterLifecycleResult(Array.Empty<FictionCharacter>(), Array.Empty<FictionCharacter>(), Array.Empty<FictionLoreRequirement>());
}

public interface ICharacterLifecycleService
{
    Task<CharacterLifecycleResult> ProcessAsync(CharacterLifecycleRequest request, CancellationToken cancellationToken = default);
}
