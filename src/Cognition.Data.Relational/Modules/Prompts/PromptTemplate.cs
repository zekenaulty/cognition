using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Prompts;

public class PromptTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public PromptType PromptType { get; set; } = PromptType.None;
    public string Template { get; set; } = string.Empty;
    public Dictionary<string, object?>? Tokens { get; set; }
    public string? Example { get; set; }
    public bool IsActive { get; set; } = true;
}
