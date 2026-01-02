using Cognition.Domains.Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Domains.Relational.Configuration;

public class BoundedContextConfiguration : IEntityTypeConfiguration<BoundedContext>
{
    public void Configure(EntityTypeBuilder<BoundedContext> builder)
    {
        builder.ToTable("bounded_contexts");

        builder.HasKey(x => x.Id).HasName("pk_bounded_contexts");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DomainId).HasColumnName("domain_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContextKey).HasColumnName("context_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(x => new { x.DomainId, x.ContextKey })
            .IsUnique()
            .HasDatabaseName("ux_bounded_contexts_domain_context_key");

        builder.HasIndex(x => x.DomainId)
            .HasDatabaseName("ix_bounded_contexts_domain_id");
    }
}
