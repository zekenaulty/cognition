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

        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var transcriptConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<PlannerExecutionTranscriptEntry>?, string?>(
            v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrWhiteSpace(v) ? null : System.Text.Json.JsonSerializer.Deserialize<List<PlannerExecutionTranscriptEntry>>(v!, jsonOptions));
        var transcriptComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<PlannerExecutionTranscriptEntry>?>(
            (l, r) => System.Text.Json.JsonSerializer.Serialize(l ?? new List<PlannerExecutionTranscriptEntry>(), jsonOptions) ==
                      System.Text.Json.JsonSerializer.Serialize(r ?? new List<PlannerExecutionTranscriptEntry>(), jsonOptions),
            v => (v == null ? 0 : System.Text.Json.JsonSerializer.Serialize(v, jsonOptions).GetHashCode()),
            v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<List<PlannerExecutionTranscriptEntry>>(System.Text.Json.JsonSerializer.Serialize(v, jsonOptions), jsonOptions)!);

        builder.Property(x => x.Transcript)
            .HasColumnName("transcript")
            .HasColumnType("jsonb")
            .HasConversion(transcriptConverter)
            .Metadata.SetValueComparer(transcriptComparer);
    }
}
