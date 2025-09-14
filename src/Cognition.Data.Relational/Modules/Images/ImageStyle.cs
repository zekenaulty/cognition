using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Images;

public class ImageStyle : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PromptPrefix { get; set; }
    public string? NegativePrompt { get; set; }
    public Dictionary<string, object?>? Defaults { get; set; }
    public bool IsActive { get; set; } = true;
}

