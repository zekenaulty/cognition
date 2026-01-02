using Cognition.Domains.Common;

namespace Cognition.Domains.Scopes;

public class ScopeInstance : BaseEntity
{
    public Guid ScopeTypeId { get; set; }
    public Dictionary<string, string> DimensionValues { get; set; } = new();
    public string CompiledScopeString { get; set; } = "";
    public Guid? DomainId { get; set; }
    public Guid? BoundedContextId { get; set; }
}
