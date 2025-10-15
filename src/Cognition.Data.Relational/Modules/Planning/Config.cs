using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Data.Relational.Modules.Planning;

public sealed class PlannerExecutionConfiguration : IEntityTypeConfiguration<PlannerExecution>
{
    public void Configure(EntityTypeBuilder<PlannerExecution> builder)
    {
        builder.ToTable("planner_executions");
        builder.HasKey(x => x.Id).HasName("pk_planner_executions");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.Property(x => x.ToolId).HasColumnName("tool_id");
        builder.Property(x => x.PlannerName).HasColumnName("planner_name").HasMaxLength(256).IsRequired();
        builder.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(64).IsRequired();
        builder.Property(x => x.AgentId).HasColumnName("agent_id");
        builder.Property(x => x.ConversationId).HasColumnName("conversation_id");
        builder.Property(x => x.PrimaryAgentId).HasColumnName("primary_agent_id");
        builder.Property(x => x.Environment).HasColumnName("environment").HasMaxLength(64);
        builder.Property(x => x.ScopePath).HasColumnName("scope_path").HasMaxLength(512);

        builder.Property(x => x.ConversationState).HasColumnName("conversation_state").HasColumnType("jsonb");
        builder.Property(x => x.Artifacts).HasColumnName("artifacts").HasColumnType("jsonb");
        builder.Property(x => x.Metrics).HasColumnName("metrics").HasColumnType("jsonb");
        builder.Property(x => x.Diagnostics).HasColumnName("diagnostics").HasColumnType("jsonb");
        builder.Property(x => x.Transcript).HasColumnName("transcript").HasColumnType("jsonb");
    }
}
