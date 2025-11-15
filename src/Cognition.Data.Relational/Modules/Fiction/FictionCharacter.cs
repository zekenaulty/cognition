using System;
using Cognition.Data.Relational.Modules.Agents;
using Cognition.Data.Relational.Modules.Common;
using Cognition.Data.Relational.Modules.Personas;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionCharacter : BaseEntity
{
    public Guid FictionPlanId { get; set; }
    public FictionPlan FictionPlan { get; set; } = null!;

    public Guid? PersonaId { get; set; }
    public Persona? Persona { get; set; }

    public Guid? AgentId { get; set; }
    public Agent? Agent { get; set; }

    public Guid? WorldBibleEntryId { get; set; }
    public FictionWorldBibleEntry? WorldBibleEntry { get; set; }

    public Guid? FirstSceneId { get; set; }
    public FictionChapterScene? FirstScene { get; set; }

    public Guid? CreatedByPlanPassId { get; set; }
    public FictionPlanPass? CreatedByPlanPass { get; set; }

    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Importance { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Notes { get; set; }
    public string? ProvenanceJson { get; set; }
}
