using Cognition.Workflows.Executions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Workflows.Relational.Configuration;

public class WorkflowExecutionConfiguration : IEntityTypeConfiguration<WorkflowExecution>
{
    public void Configure(EntityTypeBuilder<WorkflowExecution> builder)
    {
        builder.ToTable("workflow_executions");

        builder.HasKey(x => x.Id).HasName("pk_workflow_executions");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>();
        builder.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc");
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
            .HasDatabaseName("ix_workflow_executions_definition_id");

        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.Status })
            .HasDatabaseName("ix_workflow_executions_definition_status");
    }
}
