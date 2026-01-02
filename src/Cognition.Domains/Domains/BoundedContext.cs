using Cognition.Domains.Common;

namespace Cognition.Domains.Domains;

public class BoundedContext : BaseEntity
{
    public Guid DomainId { get; set; }
    public string Name { get; set; } = "";
    public string ContextKey { get; set; } = "";
    public string? Description { get; set; }
}
