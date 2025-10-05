using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class FictionChapterSection : BaseEntity
{
    public Guid FictionChapterScrollId { get; set; }
    public FictionChapterScroll FictionChapterScroll { get; set; } = null!;

    public Guid? ParentSectionId { get; set; }
    public FictionChapterSection? ParentSection { get; set; }
    public List<FictionChapterSection> ChildSections { get; set; } = [];

    public int SectionIndex { get; set; }
    public string SectionSlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }

    public Guid? BranchId { get; set; }

    public List<FictionChapterScene> Scenes { get; set; } = [];
}
