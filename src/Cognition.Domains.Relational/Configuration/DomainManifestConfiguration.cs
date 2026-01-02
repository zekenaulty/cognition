using Cognition.Domains.Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Domains.Relational.Configuration;

public class DomainManifestConfiguration : IEntityTypeConfiguration<DomainManifest>
{
    public void Configure(EntityTypeBuilder<DomainManifest> builder)
    {
        builder.ToTable("domain_manifests");

        builder.HasKey(x => x.Id).HasName("pk_domain_manifests");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DomainId).HasColumnName("domain_id");
        builder.Property(x => x.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.AllowedEmbeddingFlavors)
            .HasColumnName("allowed_embedding_flavors")
            .HasColumnType("jsonb")
            .HasConversion(JsonValueConversions.StringListConverter)
            .Metadata.SetValueComparer(JsonValueConversions.StringListComparer);
        builder.Property(x => x.DefaultEmbeddingFlavor).HasColumnName("default_embedding_flavor");
        builder.Property(x => x.IndexIsolationPolicy).HasColumnName("index_isolation_policy").HasConversion<string>();
        builder.Property(x => x.AllowedToolCategories)
            .HasColumnName("allowed_tool_categories")
            .HasColumnType("jsonb")
            .HasConversion(JsonValueConversions.ToolCategoryListConverter)
            .Metadata.SetValueComparer(JsonValueConversions.ToolCategoryListComparer);
        builder.Property(x => x.RequiredMetadataSchema).HasColumnName("required_metadata_schema").HasColumnType("jsonb");
        builder.Property(x => x.SafetyProfile).HasColumnName("safety_profile");
        builder.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(x => new { x.DomainId, x.Version })
            .IsUnique()
            .HasDatabaseName("ux_domain_manifests_domain_version");

        builder.HasIndex(x => x.DomainId)
            .HasDatabaseName("ix_domain_manifests_domain_id");
    }
}
