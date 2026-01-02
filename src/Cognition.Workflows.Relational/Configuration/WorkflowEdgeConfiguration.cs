using Cognition.Workflows.Definitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Workflows.Relational.Configuration;

public class WorkflowEdgeConfiguration : IEntityTypeConfiguration<WorkflowEdge>
{
    public void Configure(EntityTypeBuilder<WorkflowEdge> builder)
    {
        builder.ToTable("workflow_edges");

        builder.HasKey(x => x.Id).HasName("pk_workflow_edges");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
        builder.Property(x => x.FromNodeId).HasColumnName("from_node_id");
        builder.Property(x => x.ToNodeId).HasColumnName("to_node_id");
        builder.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(200).IsRequired();
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

        builder.HasIndex(x => x.WorkflowDefinitionId)
            .HasDatabaseName("ix_workflow_edges_definition_id");

        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.FromNodeId })
            .HasDatabaseName("ix_workflow_edges_from_node");

        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.ToNodeId })
            .HasDatabaseName("ix_workflow_edges_to_node");

        builder.HasOne<WorkflowNode>()
            .WithMany()
            .HasForeignKey(x => x.FromNodeId)
            .HasConstraintName("fk_workflow_edges_from_node");

        builder.HasOne<WorkflowNode>()
            .WithMany()
            .HasForeignKey(x => x.ToNodeId)
            .HasConstraintName("fk_workflow_edges_to_node");
    }
}
