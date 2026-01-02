using Cognition.Domains.Scopes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Domains.Relational.Configuration;

public class ScopeInstanceConfiguration : IEntityTypeConfiguration<ScopeInstance>
{
    public void Configure(EntityTypeBuilder<ScopeInstance> builder)
    {
        builder.ToTable("scope_instances");

        builder.HasKey(x => x.Id).HasName("pk_scope_instances");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ScopeTypeId).HasColumnName("scope_type_id");
        builder.Property(x => x.DimensionValues)
            .HasColumnName("dimension_values")
            .HasColumnType("jsonb")
            .HasConversion(JsonValueConversions.StringDictionaryConverter)
            .Metadata.SetValueComparer(JsonValueConversions.StringDictionaryComparer);
        builder.Property(x => x.CompiledScopeString).HasColumnName("compiled_scope_string").HasMaxLength(512).IsRequired();
        builder.Property(x => x.DomainId).HasColumnName("domain_id");
        builder.Property(x => x.BoundedContextId).HasColumnName("bounded_context_id");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(x => new { x.ScopeTypeId, x.CompiledScopeString })
            .IsUnique()
            .HasDatabaseName("ux_scope_instances_type_compiled");

        builder.HasIndex(x => x.DomainId)
            .HasDatabaseName("ix_scope_instances_domain_id");

        builder.HasIndex(x => x.BoundedContextId)
            .HasDatabaseName("ix_scope_instances_context_id");
    }
}
