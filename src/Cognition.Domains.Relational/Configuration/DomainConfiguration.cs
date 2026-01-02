using Cognition.Domains.Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Domains.Relational.Configuration;

public class DomainConfiguration : IEntityTypeConfiguration<Domain>
{
    public void Configure(EntityTypeBuilder<Domain> builder)
    {
        builder.ToTable("domains");

        builder.HasKey(x => x.Id).HasName("pk_domains");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CanonicalKey).HasColumnName("canonical_key").HasMaxLength(256).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(x => x.ParentDomainId).HasColumnName("parent_domain_id");
        builder.Property(x => x.CurrentManifestId).HasColumnName("current_manifest_id");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(x => x.CanonicalKey)
            .IsUnique()
            .HasDatabaseName("ux_domains_canonical_key");

        builder.HasIndex(x => x.ParentDomainId)
            .HasDatabaseName("ix_domains_parent_domain_id");

        builder.HasMany(x => x.BoundedContexts)
            .WithOne()
            .HasForeignKey(x => x.DomainId)
            .HasConstraintName("fk_bounded_contexts_domain");

        builder.HasMany(x => x.ManifestHistory)
            .WithOne()
            .HasForeignKey(x => x.DomainId)
            .HasConstraintName("fk_domain_manifests_domain");

        builder.HasMany(x => x.Policies)
            .WithOne()
            .HasForeignKey(x => x.DomainId)
            .HasConstraintName("fk_policies_domain");

        builder.HasOne<DomainManifest>()
            .WithMany()
            .HasForeignKey(x => x.CurrentManifestId)
            .HasConstraintName("fk_domains_current_manifest");
    }
}
