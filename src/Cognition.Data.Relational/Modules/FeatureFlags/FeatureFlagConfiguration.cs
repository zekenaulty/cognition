using Cognition.Data.Relational.Modules.FeatureFlags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Data.Relational.Modules.FeatureFlags;

public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("feature_flags");

        builder.HasKey(f => f.Id).HasName("pk_feature_flags");

        builder.Property(f => f.Id)
            .HasColumnName("id");

        builder.Property(f => f.Key)
            .HasColumnName("key")
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(f => f.Key)
            .IsUnique()
            .HasDatabaseName("ux_feature_flags_key");

        builder.Property(f => f.Description)
            .HasColumnName("description");

        builder.Property(f => f.IsEnabled)
            .HasColumnName("is_enabled")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(f => f.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(f => f.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");
    }
}
