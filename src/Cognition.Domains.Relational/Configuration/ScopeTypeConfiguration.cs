using Cognition.Domains.Scopes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Domains.Relational.Configuration;

public class ScopeTypeConfiguration : IEntityTypeConfiguration<ScopeType>
{
    public void Configure(EntityTypeBuilder<ScopeType> builder)
    {
        builder.ToTable("scope_types");

        builder.HasKey(x => x.Id).HasName("pk_scope_types");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.OrderedDimensions)
            .HasColumnName("ordered_dimensions")
            .HasColumnType("jsonb")
            .HasConversion(JsonValueConversions.StringListConverter)
            .Metadata.SetValueComparer(JsonValueConversions.StringListComparer);
        builder.Property(x => x.FormatPattern).HasColumnName("format_pattern");
        builder.Property(x => x.CanonicalizationRules).HasColumnName("canonicalization_rules").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("ux_scope_types_name");
    }
}
