using System;
using System.Collections.Generic;
using System.Linq;
using Cognition.Domains.Domains;
using Cognition.Domains.Policies;
using Cognition.Domains.Relational;
using Cognition.Domains.Scopes;
using Cognition.Domains.Tools;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace Cognition.Domains.Relational.Tests;

public class DomainsConfigurationTests
{
    [Fact]
    public void DomainConfiguration_MapsExpectedSchema()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var entity = model.FindEntityType(typeof(Domain));
        entity.Should().NotBeNull();

        entity!.GetTableName().Should().Be("domains");
        entity!.GetSchema().Should().BeNull();

        AssertColumn(entity!, nameof(Domain.Id), "id", isNullable: false);
        AssertColumn(entity!, nameof(Domain.CanonicalKey), "canonical_key", isNullable: false, maxLength: 256);
        AssertColumn(entity!, nameof(Domain.Name), "name", isNullable: false, maxLength: 200);
        AssertColumn(entity!, nameof(Domain.ParentDomainId), "parent_domain_id", isNullable: true);
        AssertColumn(entity!, nameof(Domain.CurrentManifestId), "current_manifest_id", isNullable: true);

        var canonicalIndex = FindIndex(entity!, nameof(Domain.CanonicalKey));
        canonicalIndex.IsUnique.Should().BeTrue();
        GetRelationalName(canonicalIndex).Should().Be("ux_domains_canonical_key");

        var parentIndex = FindIndex(entity!, nameof(Domain.ParentDomainId));
        parentIndex.IsUnique.Should().BeFalse();
        GetRelationalName(parentIndex).Should().Be("ix_domains_parent_domain_id");

        var manifestForeignKey = FindForeignKey(entity!, nameof(Domain.CurrentManifestId));
        GetRelationalName(manifestForeignKey).Should().Be("fk_domains_current_manifest");
    }

    [Fact]
    public void BoundedContextConfiguration_MapsExpectedSchema()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var entity = model.FindEntityType(typeof(BoundedContext));
        entity.Should().NotBeNull();

        entity!.GetTableName().Should().Be("bounded_contexts");
        entity!.GetSchema().Should().BeNull();

        AssertColumn(entity!, nameof(BoundedContext.Id), "id", isNullable: false);
        AssertColumn(entity!, nameof(BoundedContext.DomainId), "domain_id", isNullable: false);
        AssertColumn(entity!, nameof(BoundedContext.Name), "name", isNullable: false, maxLength: 200);
        AssertColumn(entity!, nameof(BoundedContext.ContextKey), "context_key", isNullable: false, maxLength: 200);

        var keyIndex = FindIndex(entity!, nameof(BoundedContext.DomainId), nameof(BoundedContext.ContextKey));
        keyIndex.IsUnique.Should().BeTrue();
        GetRelationalName(keyIndex).Should().Be("ux_bounded_contexts_domain_context_key");

        var domainIndex = FindIndex(entity!, nameof(BoundedContext.DomainId));
        domainIndex.IsUnique.Should().BeFalse();
        GetRelationalName(domainIndex).Should().Be("ix_bounded_contexts_domain_id");

        var domainForeignKey = FindForeignKey(entity!, nameof(BoundedContext.DomainId));
        GetRelationalName(domainForeignKey).Should().Be("fk_bounded_contexts_domain");
    }

    [Fact]
    public void DomainManifestConfiguration_MapsExpectedSchema()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var entity = model.FindEntityType(typeof(DomainManifest));
        entity.Should().NotBeNull();

        entity!.GetTableName().Should().Be("domain_manifests");
        entity!.GetSchema().Should().BeNull();

        AssertColumn(entity!, nameof(DomainManifest.Id), "id", isNullable: false);
        AssertColumn(entity!, nameof(DomainManifest.DomainId), "domain_id", isNullable: false);
        AssertColumn(entity!, nameof(DomainManifest.Version), "version", isNullable: false, maxLength: 64);

        var uniqueIndex = FindIndex(entity!, nameof(DomainManifest.DomainId), nameof(DomainManifest.Version));
        uniqueIndex.IsUnique.Should().BeTrue();
        GetRelationalName(uniqueIndex).Should().Be("ux_domain_manifests_domain_version");

        var domainIndex = FindIndex(entity!, nameof(DomainManifest.DomainId));
        domainIndex.IsUnique.Should().BeFalse();
        GetRelationalName(domainIndex).Should().Be("ix_domain_manifests_domain_id");

        var domainForeignKey = FindForeignKey(entity!, nameof(DomainManifest.DomainId));
        GetRelationalName(domainForeignKey).Should().Be("fk_domain_manifests_domain");
    }

    [Fact]
    public void ScopeTypeConfiguration_MapsExpectedSchema()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var entity = model.FindEntityType(typeof(ScopeType));
        entity.Should().NotBeNull();

        entity!.GetTableName().Should().Be("scope_types");
        entity!.GetSchema().Should().BeNull();

        AssertColumn(entity!, nameof(ScopeType.Id), "id", isNullable: false);
        AssertColumn(entity!, nameof(ScopeType.Name), "name", isNullable: false, maxLength: 200);

        var nameIndex = FindIndex(entity!, nameof(ScopeType.Name));
        nameIndex.IsUnique.Should().BeTrue();
        GetRelationalName(nameIndex).Should().Be("ux_scope_types_name");
    }

    [Fact]
    public void ToolDescriptorConfiguration_MapsExpectedSchema()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var entity = model.FindEntityType(typeof(ToolDescriptor));
        entity.Should().NotBeNull();

        entity!.GetTableName().Should().Be("tool_descriptors");
        entity!.GetSchema().Should().BeNull();

        AssertColumn(entity!, nameof(ToolDescriptor.Id), "id", isNullable: false);
        AssertColumn(entity!, nameof(ToolDescriptor.Name), "name", isNullable: false, maxLength: 200);
        AssertColumn(entity!, nameof(ToolDescriptor.OwningDomainId), "owning_domain_id", isNullable: false);

        var nameIndex = FindIndex(entity!, nameof(ToolDescriptor.OwningDomainId), nameof(ToolDescriptor.Name));
        nameIndex.IsUnique.Should().BeTrue();
        GetRelationalName(nameIndex).Should().Be("ux_tool_descriptors_domain_name");

        var domainIndex = FindIndex(entity!, nameof(ToolDescriptor.OwningDomainId));
        domainIndex.IsUnique.Should().BeFalse();
        GetRelationalName(domainIndex).Should().Be("ix_tool_descriptors_domain_id");
    }

    [Fact]
    public void PolicyConfiguration_MapsExpectedSchema()
    {
        using var context = CreateContext();
        var model = TryGetModel(context);
        var entity = model.FindEntityType(typeof(Policy));
        entity.Should().NotBeNull();

        entity!.GetTableName().Should().Be("policies");
        entity!.GetSchema().Should().BeNull();

        AssertColumn(entity!, nameof(Policy.Id), "id", isNullable: false);
        AssertColumn(entity!, nameof(Policy.DomainId), "domain_id", isNullable: false);
        AssertColumn(entity!, nameof(Policy.Name), "name", isNullable: false, maxLength: 200);

        var nameIndex = FindIndex(entity!, nameof(Policy.DomainId), nameof(Policy.Name));
        nameIndex.IsUnique.Should().BeTrue();
        GetRelationalName(nameIndex).Should().Be("ux_policies_domain_name");

        var domainIndex = FindIndex(entity!, nameof(Policy.DomainId));
        domainIndex.IsUnique.Should().BeFalse();
        GetRelationalName(domainIndex).Should().Be("ix_policies_domain_id");
    }

    private static CognitionDomainsDbContext CreateContext()
    {
        var services = new ServiceCollection();
        services.AddEntityFrameworkNpgsql();
        var provider = services.BuildServiceProvider();

        var options = new DbContextOptionsBuilder<CognitionDomainsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=cognition_domains_shadow;Username=postgres;Password=postgres")
            .UseInternalServiceProvider(provider)
            .Options;

        return new CognitionDomainsDbContext(options);
    }

    private static IModel TryGetModel(CognitionDomainsDbContext context)
    {
        try
        {
            return context.Model;
        }
        catch (Exception ex)
        {
            throw new XunitException($"Failed to build CognitionDomainsDbContext model for configuration tests: {ex}");
        }
    }

    private static void AssertColumn(IEntityType entity, string propertyName, string expectedColumnName, bool isNullable, int? maxLength = null)
    {
        var property = entity.FindProperty(propertyName);
        property.Should().NotBeNull($"Property {propertyName} should be configured on {entity.Name}");
        property!.IsNullable.Should().Be(isNullable);
        GetRelationalColumnName(property).Should().Be(expectedColumnName);
        if (maxLength.HasValue)
        {
            property.GetMaxLength().Should().Be(maxLength.Value);
        }
    }

    private static IIndex FindIndex(IEntityType entity, params string[] propertyNames)
    {
        var matches = entity.GetIndexes()
            .Where(index => index.Properties.Select(p => p.Name).SequenceEqual(propertyNames))
            .ToList();

        matches.Should().ContainSingle($"Expected index on {entity.ClrType.Name}({string.Join(", ", propertyNames)})");
        return matches[0];
    }

    private static IForeignKey FindForeignKey(IEntityType entity, params string[] propertyNames)
    {
        var matches = entity.GetForeignKeys()
            .Where(foreignKey => foreignKey.Properties.Select(p => p.Name).SequenceEqual(propertyNames))
            .ToList();

        matches.Should().ContainSingle($"Expected foreign key on {entity.ClrType.Name}({string.Join(", ", propertyNames)})");
        return matches[0];
    }

    private static string? GetRelationalColumnName(IProperty property)
    {
        return property.FindAnnotation(RelationalAnnotationNames.ColumnName)?.Value as string;
    }

    private static string? GetRelationalName(IIndex index)
    {
        return index.FindAnnotation(RelationalAnnotationNames.Name)?.Value as string;
    }

    private static string? GetRelationalName(IForeignKey foreignKey)
    {
        return foreignKey.FindAnnotation(RelationalAnnotationNames.Name)?.Value as string;
    }
}
