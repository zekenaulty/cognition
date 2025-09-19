using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Fiction;

public class WorldAsset : BaseEntity
{
    public Guid FictionProjectId { get; set; }
    public FictionProject FictionProject { get; set; } = null!;

    public WorldAssetType Type { get; set; } = WorldAssetType.Other;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int ActiveVersionIndex { get; set; } = -1; // -1 = none yet

    // Optional link to Persona when Type == Character
    public Guid? PersonaId { get; set; }

    public List<WorldAssetVersion> Versions { get; set; } = [];
}

public enum WorldAssetType
{
    Character,
    Location,
    Culture,
    Organization,
    MagicSystem,
    Technology,
    Other
}

public class WorldAssetVersion : BaseEntity
{
    public Guid WorldAssetId { get; set; }
    public WorldAsset WorldAsset { get; set; } = null!;

    public int VersionIndex { get; set; }
    public Dictionary<string, object?>? Content { get; set; } // jsonb: attributes, summary, traits, etc.

    // Optional projection link
    public Guid? KnowledgeItemId { get; set; }
}

