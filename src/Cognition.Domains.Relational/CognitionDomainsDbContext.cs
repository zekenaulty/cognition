using Cognition.Domains.Assets;
using Cognition.Domains.Domains;
using Cognition.Domains.Events;
using Cognition.Domains.Policies;
using Cognition.Domains.Scopes;
using Cognition.Domains.Tools;
using Microsoft.EntityFrameworkCore;

namespace Cognition.Domains.Relational;

public class CognitionDomainsDbContext : DbContext
{
    public CognitionDomainsDbContext(DbContextOptions<CognitionDomainsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Domain> Domains => Set<Domain>();
    public DbSet<BoundedContext> BoundedContexts => Set<BoundedContext>();
    public DbSet<DomainManifest> DomainManifests => Set<DomainManifest>();
    public DbSet<ScopeType> ScopeTypes => Set<ScopeType>();
    public DbSet<ScopeInstance> ScopeInstances => Set<ScopeInstance>();
    public DbSet<KnowledgeAsset> KnowledgeAssets => Set<KnowledgeAsset>();
    public DbSet<ToolDescriptor> ToolDescriptors => Set<ToolDescriptor>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<EventType> EventTypes => Set<EventType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CognitionDomainsDbContext).Assembly);
    }
}
