using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionChapterScroll : BaseEntity
{
    public Guid FictionChapterBlueprintId { get; set; }
    public FictionChapterBlueprint FictionChapterBlueprint { get; set; } = null!;

    public int VersionIndex { get; set; }
    public string ScrollSlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Synopsis { get; set; } = string.Empty;
    public Dictionary<string, object?>? Metadata { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? DerivedFromScrollId { get; set; }
    public FictionChapterScroll? DerivedFromScroll { get; set; }

    public List<FictionChapterSection> Sections { get; set; } = [];
    public List<FictionWorldBibleEntry> WorldBibleEntries { get; set; } = [];
}
