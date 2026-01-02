using Cognition.Domains.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Domains.Relational.Configuration;

public class EventTypeConfiguration : IEntityTypeConfiguration<EventType>
{
    public void Configure(EntityTypeBuilder<EventType> builder)
    {
        builder.ToTable("event_types");

        builder.HasKey(x => x.Id).HasName("pk_event_types");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DomainId).HasColumnName("domain_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.SchemaJson).HasColumnName("schema_json").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(x => new { x.DomainId, x.Name })
            .IsUnique()
            .HasDatabaseName("ux_event_types_domain_name");

        builder.HasIndex(x => x.DomainId)
            .HasDatabaseName("ix_event_types_domain_id");
    }
}
