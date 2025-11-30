using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionWorldBibleEntry : BaseEntity
{
    public Guid FictionWorldBibleId { get; set; }
    public FictionWorldBible FictionWorldBible { get; set; } = null!;

    public string EntrySlug { get; set; } = string.Empty;
    public string EntryName { get; set; } = string.Empty;
    public FictionWorldBibleEntryContent Content { get; set; } = new();

    public int Version { get; set; } = 1;
    public FictionWorldBibleChangeType ChangeType { get; set; } = FictionWorldBibleChangeType.Unknown;
    public int Sequence { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? DerivedFromEntryId { get; set; }
    public FictionWorldBibleEntry? DerivedFromEntry { get; set; }

    public Guid? AgentId { get; set; }
    public Modules.Agents.Agent? Agent { get; set; }

    public Guid? PersonaId { get; set; }
    public Modules.Personas.Persona? Persona { get; set; }

    public Guid? SourcePlanPassId { get; set; }
    public Guid? SourceConversationId { get; set; }
    public string? SourceBacklogId { get; set; }
    public string? BranchSlug { get; set; }

    public Guid? FictionChapterScrollId { get; set; }
    public FictionChapterScroll? FictionChapterScroll { get; set; }
    public Guid? FictionChapterSceneId { get; set; }
    public FictionChapterScene? FictionChapterScene { get; set; }
}

public enum FictionWorldBibleChangeType
{
    Unknown,
    Seed,
    Update
}
