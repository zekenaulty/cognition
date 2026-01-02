using Cognition.Domains.Common;

namespace Cognition.Domains.Scopes;

public class ScopeType : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public List<string> OrderedDimensions { get; set; } = new();
    public string? FormatPattern { get; set; }
    public string? CanonicalizationRules { get; set; }
}
