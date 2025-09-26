using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Conversations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.ToTable("conversations");
        b.HasKey(x => x.Id).HasName("pk_conversations");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Title).HasColumnName("title");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        b.Property(x => x.AgentId).HasColumnName("agent_id");
        b.HasOne(x => x.Agent).WithMany().HasForeignKey(x => x.AgentId).HasConstraintName("fk_conversations_agents");
        b.HasIndex(x => x.AgentId);
    }
}

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> b)
    {
        b.ToTable("conversation_participants");
        b.HasKey(x => x.Id).HasName("pk_conversation_participants");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ConversationId).HasColumnName("conversation_id");
        b.Property(x => x.PersonaId).HasColumnName("persona_id");
        b.HasOne(x => x.Conversation).WithMany(c => c.Participants).HasForeignKey(x => x.ConversationId).HasConstraintName("fk_conversation_participants_conversations");
        b.HasOne(x => x.Persona).WithMany().HasForeignKey(x => x.PersonaId).HasConstraintName("fk_conversation_participants_personas");
        b.Property(x => x.Role).HasColumnName("role");
        b.Property(x => x.JoinedAtUtc).HasColumnName("joined_at_utc");
        b.HasIndex(x => new { x.ConversationId, x.PersonaId }).HasDatabaseName("ix_conversation_participants_conversation_persona");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> b)
    {
        b.ToTable("conversation_messages");
        b.HasKey(x => x.Id).HasName("pk_conversation_messages");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ConversationId).HasColumnName("conversation_id");
        b.HasOne(x => x.Conversation).WithMany(c => c.Messages).HasForeignKey(x => x.ConversationId).HasConstraintName("fk_conversation_messages_conversations");
        b.Property(x => x.FromPersonaId).HasColumnName("from_persona_id");
        b.HasOne(x => x.FromPersona).WithMany().HasForeignKey(x => x.FromPersonaId).HasConstraintName("fk_conversation_messages_from_persona");
        b.Property(x => x.ToPersonaId).HasColumnName("to_persona_id");
        b.HasOne(x => x.ToPersona).WithMany().HasForeignKey(x => x.ToPersonaId).HasConstraintName("fk_conversation_messages_to_persona");
        b.Property(x => x.FromAgentId).HasColumnName("from_agent_id");
        b.HasOne(x => x.FromAgent).WithMany().HasForeignKey(x => x.FromAgentId).HasConstraintName("fk_conversation_messages_from_agent");
        b.HasIndex(x => x.FromAgentId);
        b.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        b.Property(x => x.Role).HasColumnName("role").HasConversion<string>();
        b.Property(x => x.Metatype).HasColumnName("metatype");
        b.Property(x => x.Content).HasColumnName("content");
        b.Property(x => x.ActiveVersionIndex).HasColumnName("active_version_index");
        b.Property(x => x.Timestamp).HasColumnName("timestamp");
        b.HasIndex(x => new { x.ConversationId, x.Timestamp }).HasDatabaseName("ix_conversation_messages_conversation_ts");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class ConversationMessageVersionConfiguration : IEntityTypeConfiguration<ConversationMessageVersion>
{
    public void Configure(EntityTypeBuilder<ConversationMessageVersion> b)
    {
        b.ToTable("conversation_message_versions");
        b.HasKey(x => x.Id).HasName("pk_conversation_message_versions");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ConversationMessageId).HasColumnName("conversation_message_id");
        b.HasOne(x => x.ConversationMessage).WithMany().HasForeignKey(x => x.ConversationMessageId).HasConstraintName("fk_conversation_message_versions_message");
        b.Property(x => x.VersionIndex).HasColumnName("version_index");
        b.Property(x => x.Content).HasColumnName("content");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => new { x.ConversationMessageId, x.VersionIndex }).IsUnique().HasDatabaseName("ux_conversation_message_versions_message_index");
    }
}

public class ConversationSummaryConfiguration : IEntityTypeConfiguration<ConversationSummary>
{
    public void Configure(EntityTypeBuilder<ConversationSummary> b)
    {
        b.ToTable("conversation_summaries");
        b.HasKey(x => x.Id).HasName("pk_conversation_summaries");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ConversationId).HasColumnName("conversation_id");
        b.HasOne(x => x.Conversation).WithMany(c => c.Summaries).HasForeignKey(x => x.ConversationId).HasConstraintName("fk_conversation_summaries_conversations");
        b.Property(x => x.ByPersonaId).HasColumnName("by_persona_id");
        b.HasOne(x => x.ByPersona).WithMany().HasForeignKey(x => x.ByPersonaId).HasConstraintName("fk_conversation_summaries_by_persona");
        b.Property(x => x.ReferencesPersonaId).HasColumnName("references_persona_id");
        b.HasOne(x => x.ReferencesPersona).WithMany().HasForeignKey(x => x.ReferencesPersonaId).HasConstraintName("fk_conversation_summaries_ref_persona");
        b.Property(x => x.Content).HasColumnName("content");
        b.Property(x => x.Timestamp).HasColumnName("timestamp");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class ConversationPlanConfiguration : IEntityTypeConfiguration<ConversationPlan>
{
    public void Configure(EntityTypeBuilder<ConversationPlan> b)
    {
        b.ToTable("conversation_plans");
        b.HasKey(x => x.Id).HasName("pk_conversation_plans");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ConversationId).HasColumnName("conversation_id");
        b.HasOne(x => x.Conversation).WithMany().HasForeignKey(x => x.ConversationId).HasConstraintName("fk_conversation_plans_conversations");
        b.Property(x => x.PersonaId).HasColumnName("persona_id");
        b.HasOne(x => x.Persona).WithMany().HasForeignKey(x => x.PersonaId).HasConstraintName("fk_conversation_plans_persona");
    b.Property(x => x.Title).HasColumnName("title");
    b.Property(x => x.Description).HasColumnName("description");
    b.Property(x => x.CreatedAt).HasColumnName("created_at");
    b.Property(x => x.OutlineJson).HasColumnName("outline_json").HasColumnType("jsonb");
    }
}

public class ConversationTaskConfiguration : IEntityTypeConfiguration<ConversationTask>
{
    public void Configure(EntityTypeBuilder<ConversationTask> b)
    {
        b.ToTable("conversation_tasks");
        b.HasKey(x => x.Id).HasName("pk_conversation_tasks");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ConversationPlanId).HasColumnName("conversation_plan_id");
        b.HasOne(x => x.ConversationPlan).WithMany(p => p.Tasks).HasForeignKey(x => x.ConversationPlanId).HasConstraintName("fk_conversation_tasks_plan");
    b.Property(x => x.StepNumber).HasColumnName("step_number");
    b.Property(x => x.Thought).HasColumnName("thought");
    b.Property(x => x.Goal).HasColumnName("goal");
    b.Property(x => x.Rationale).HasColumnName("rationale");
    b.Property(x => x.ToolId).HasColumnName("tool_id");
    b.Property(x => x.ToolName).HasColumnName("tool_name");
    b.Property(x => x.ArgsJson).HasColumnName("args_json").HasColumnType("jsonb");
    b.Property(x => x.Observation).HasColumnName("observation");
    b.Property(x => x.Error).HasColumnName("error");
    b.Property(x => x.Finish).HasColumnName("finish");
    b.Property(x => x.FinalAnswer).HasColumnName("final_answer");
    b.Property(x => x.Status).HasColumnName("status");
    b.Property(x => x.CreatedAt).HasColumnName("created_at");
    }
}

public class ConversationThoughtConfiguration : IEntityTypeConfiguration<ConversationThought>
{
    public void Configure(EntityTypeBuilder<ConversationThought> b)
    {
        b.ToTable("conversation_thoughts");
        b.HasKey(x => x.Id).HasName("pk_conversation_thoughts");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ConversationId).HasColumnName("conversation_id");
        b.HasOne(x => x.Conversation).WithMany().HasForeignKey(x => x.ConversationId).HasConstraintName("fk_conversation_thoughts_conversations");
        b.Property(x => x.PersonaId).HasColumnName("persona_id");
        b.HasOne(x => x.Persona).WithMany().HasForeignKey(x => x.PersonaId).HasConstraintName("fk_conversation_thoughts_persona");
    b.Property(x => x.Thought).HasColumnName("thought");
    b.Property(x => x.StepNumber).HasColumnName("step_number");
    b.Property(x => x.Rationale).HasColumnName("rationale");
    b.Property(x => x.PlanSnapshotJson).HasColumnName("plan_snapshot_json").HasColumnType("jsonb");
    b.Property(x => x.Prompt).HasColumnName("prompt");
    b.Property(x => x.ParentThoughtId).HasColumnName("parent_thought_id");
    b.Property(x => x.Timestamp).HasColumnName("timestamp");
    }
}
