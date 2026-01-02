using Cognition.Workflows.Definitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Workflows.Relational.Configuration;

public class WorkflowNodeConfiguration : IEntityTypeConfiguration<WorkflowNode>
{
    public void Configure(EntityTypeBuilder<WorkflowNode> builder)
    {
        builder.ToTable("workflow_nodes");

        builder.HasKey(x => x.Id).HasName("pk_workflow_nodes");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
        builder.Property(x => x.Key).HasColumnName("key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.NodeType).HasColumnName("node_type").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
        builder.Property(x => x.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(JsonValueConversions.ObjectDictionaryConverter)
            .Metadata.SetValueComparer(JsonValueConversions.ObjectDictionaryComparer);
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.Key })
            .IsUnique()
            .HasDatabaseName("ux_workflow_nodes_definition_key");

        builder.HasIndex(x => x.WorkflowDefinitionId)
            .HasDatabaseName("ix_workflow_nodes_definition_id");
    }
}
