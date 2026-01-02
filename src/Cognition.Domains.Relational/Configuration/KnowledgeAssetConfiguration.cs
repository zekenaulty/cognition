using Cognition.Domains.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Domains.Relational.Configuration;

public class KnowledgeAssetConfiguration : IEntityTypeConfiguration<KnowledgeAsset>
{
    public void Configure(EntityTypeBuilder<KnowledgeAsset> builder)
    {
        builder.ToTable("knowledge_assets");

        builder.HasKey(x => x.Id).HasName("pk_knowledge_assets");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DomainId).HasColumnName("domain_id");
        builder.Property(x => x.BoundedContextId).HasColumnName("bounded_context_id");
        builder.Property(x => x.ScopeString).HasColumnName("scope_string").HasMaxLength(512).IsRequired();
        builder.Property(x => x.AssetType).HasColumnName("asset_type").HasConversion<string>();
        builder.Property(x => x.ContentRef).HasColumnName("content_ref").HasMaxLength(512).IsRequired();
        builder.Property(x => x.ContentHash).HasColumnName("content_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(JsonValueConversions.ObjectDictionaryConverter)
            .Metadata.SetValueComparer(JsonValueConversions.ObjectDictionaryComparer);
        builder.Property(x => x.DerivedFromAssetId).HasColumnName("derived_from_asset_id");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(x => new { x.DomainId, x.ContentHash })
            .HasDatabaseName("ix_knowledge_assets_domain_hash");

        builder.HasIndex(x => x.ScopeString)
            .HasDatabaseName("ix_knowledge_assets_scope_string");
    }
}
