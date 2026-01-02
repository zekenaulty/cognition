using Cognition.Domains.Common;

namespace Cognition.Domains.Policies;

public class Policy : BaseEntity
{
    public Guid DomainId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool DenyByDefault { get; set; } = true;
    public string? RulesJson { get; set; }
    public string? AppliesToScope { get; set; }
}
