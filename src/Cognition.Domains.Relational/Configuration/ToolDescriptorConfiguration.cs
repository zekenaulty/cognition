using Cognition.Domains.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Domains.Relational.Configuration;

public class ToolDescriptorConfiguration : IEntityTypeConfiguration<ToolDescriptor>
{
    public void Configure(EntityTypeBuilder<ToolDescriptor> builder)
    {
        builder.ToTable("tool_descriptors");

        builder.HasKey(x => x.Id).HasName("pk_tool_descriptors");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Category).HasColumnName("category").HasConversion<string>();
        builder.Property(x => x.OwningDomainId).HasColumnName("owning_domain_id");
        builder.Property(x => x.InputSchema).HasColumnName("input_schema").HasColumnType("jsonb");
        builder.Property(x => x.OutputSchema).HasColumnName("output_schema").HasColumnType("jsonb");
        builder.Property(x => x.SideEffectProfile).HasColumnName("side_effect_profile").HasConversion<string>();
        builder.Property(x => x.HumanGateRequired).HasColumnName("human_gate_required").HasDefaultValue(false);
        builder.Property(x => x.RequiredApprovals)
            .HasColumnName("required_approvals")
            .HasColumnType("jsonb")
            .HasConversion(JsonValueConversions.StringListConverter)
            .Metadata.SetValueComparer(JsonValueConversions.StringListComparer);
        builder.Property(x => x.AuditTags)
            .HasColumnName("audit_tags")
            .HasColumnType("jsonb")
            .HasConversion(JsonValueConversions.StringDictionaryConverter)
            .Metadata.SetValueComparer(JsonValueConversions.StringDictionaryComparer);
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(x => new { x.OwningDomainId, x.Name })
            .IsUnique()
            .HasDatabaseName("ux_tool_descriptors_domain_name");

        builder.HasIndex(x => x.OwningDomainId)
            .HasDatabaseName("ix_tool_descriptors_domain_id");
    }
}
