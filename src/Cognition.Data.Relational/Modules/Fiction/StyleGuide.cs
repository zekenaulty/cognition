using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class StyleGuide : BaseEntity
{
    public Guid FictionProjectId { get; set; }
    public FictionProject FictionProject { get; set; } = null!;

    public string Name { get; set; } = "Default";
    public Dictionary<string, object?>? Rules { get; set; } // jsonb (voice, POV, tense, taboo, format)
}

