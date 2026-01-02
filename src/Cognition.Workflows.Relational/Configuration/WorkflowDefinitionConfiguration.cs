using Cognition.Workflows.Definitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Workflows.Relational.Configuration;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("workflow_definitions");

        builder.HasKey(x => x.Id).HasName("pk_workflow_definitions");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Version).HasColumnName("version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(x => new { x.Name, x.Version })
            .IsUnique()
            .HasDatabaseName("ux_workflow_definitions_name_version");

        builder.HasMany(x => x.Nodes)
            .WithOne()
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .HasConstraintName("fk_workflow_nodes_definition");

        builder.HasMany(x => x.Edges)
            .WithOne()
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .HasConstraintName("fk_workflow_edges_definition");
    }
}
