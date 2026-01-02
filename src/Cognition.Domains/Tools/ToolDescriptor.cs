using Cognition.Domains.Common;

namespace Cognition.Domains.Tools;

public class ToolDescriptor : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public ToolCategory Category { get; set; } = ToolCategory.ReadOnly;
    public Guid OwningDomainId { get; set; }
    public string? InputSchema { get; set; }
    public string? OutputSchema { get; set; }
    public SideEffectProfile SideEffectProfile { get; set; } = SideEffectProfile.None;
    public bool HumanGateRequired { get; set; }
    public List<string> RequiredApprovals { get; set; } = new();
    public Dictionary<string, string> AuditTags { get; set; } = new();
}
