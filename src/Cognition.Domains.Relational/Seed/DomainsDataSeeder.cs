using Cognition.Domains.Common;
using Cognition.Domains.Domains;
using Cognition.Domains.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cognition.Domains.Relational.Seed;

public static class DomainsDataSeeder
{
    public static async Task SeedAsync(CognitionDomainsDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await db.Domains.AnyAsync(cancellationToken))
        {
            logger.LogInformation("DoD seed skipped: domains already present.");
            return;
        }

        var domainId = Guid.NewGuid();
        var manifestId = Guid.NewGuid();

        var domain = new Domain
        {
            Id = domainId,
            CanonicalKey = "dod",
            Name = "Domain of Domains",
            Description = "Governance registry for domain boundaries, scopes, and policies.",
            Kind = DomainKind.Technical,
            Status = DomainStatus.Draft,
            CurrentManifestId = manifestId
        };

        var manifest = new DomainManifest
        {
            Id = manifestId,
            DomainId = domainId,
            Version = "v1",
            AllowedEmbeddingFlavors = new List<string>(),
            DefaultEmbeddingFlavor = null,
            IndexIsolationPolicy = IndexIsolationPolicy.Shared,
            AllowedToolCategories = new List<ToolCategory> { ToolCategory.ReadOnly, ToolCategory.Compute },
            RequiredMetadataSchema = null,
            SafetyProfile = "Sensitive",
            PublishedAtUtc = null
        };

        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            DomainId = domainId,
            Name = "Default Deny",
            Description = "Deny by default until explicit grants are defined.",
            DenyByDefault = true,
            AppliesToScope = null,
            RulesJson = null
        };

        db.Domains.Add(domain);
        db.DomainManifests.Add(manifest);
        db.Policies.Add(policy);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("DoD seed completed: {DomainId}", domainId);
    }
}
