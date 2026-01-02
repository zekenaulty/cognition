using Cognition.Domains.Common;

namespace Cognition.Domains.Events;

public class EventType : BaseEntity
{
    public Guid? DomainId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? SchemaJson { get; set; }
}
