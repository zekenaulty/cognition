using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Personas;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionPersonaObligation : BaseEntity
{
    public Guid FictionPlanId { get; set; }
    public FictionPlan FictionPlan { get; set; } = null!;

    public Guid PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;

    public Guid? FictionCharacterId { get; set; }
    public FictionCharacter? FictionCharacter { get; set; }

    public string ObligationSlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FictionPersonaObligationStatus Status { get; set; } = FictionPersonaObligationStatus.Open;
    public string? SourcePhase { get; set; }
    public string? SourceBacklogId { get; set; }
    public Guid? SourcePlanPassId { get; set; }
    public Guid? SourceConversationId { get; set; }
    public string? BranchSlug { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? ResolvedByActor { get; set; }
}

public enum FictionPersonaObligationStatus
{
    Open,
    Resolved,
    Dismissed
}
