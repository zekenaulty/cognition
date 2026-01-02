using Cognition.Domains.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Domains.Relational.Configuration;

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("policies");

        builder.HasKey(x => x.Id).HasName("pk_policies");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DomainId).HasColumnName("domain_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.DenyByDefault).HasColumnName("deny_by_default").HasDefaultValue(true);
        builder.Property(x => x.RulesJson).HasColumnName("rules_json").HasColumnType("jsonb");
        builder.Property(x => x.AppliesToScope).HasColumnName("applies_to_scope");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(x => new { x.DomainId, x.Name })
            .IsUnique()
            .HasDatabaseName("ux_policies_domain_name");

        builder.HasIndex(x => x.DomainId)
            .HasDatabaseName("ix_policies_domain_id");
    }
}
