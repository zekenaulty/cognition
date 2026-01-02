using Cognition.Domains.Common;
using Cognition.Domains.Policies;

namespace Cognition.Domains.Domains;

public class Domain : BaseEntity
{
    public string CanonicalKey { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DomainKind Kind { get; set; } = DomainKind.Business;
    public DomainStatus Status { get; set; } = DomainStatus.Draft;
    public Guid? ParentDomainId { get; set; }

    public Guid? CurrentManifestId { get; set; }

    public List<BoundedContext> BoundedContexts { get; set; } = new();
    public List<DomainManifest> ManifestHistory { get; set; } = new();
    public List<Policy> Policies { get; set; } = new();
}
