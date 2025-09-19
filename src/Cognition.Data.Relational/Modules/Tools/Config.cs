using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Tools;

public class ToolConfiguration : IEntityTypeConfiguration<Tool>
{
    public void Configure(EntityTypeBuilder<Tool> b)
    {
        b.ToTable("tools");
        b.HasKey(x => x.Id).HasName("pk_tools");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ux_tools_name");
        b.Property(x => x.ClassPath).HasColumnName("class_path").HasMaxLength(256).IsRequired();
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.Example).HasColumnName("example");
        b.Property(x => x.Tags).HasColumnName("tags");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(x => x.ClientProfileId).HasColumnName("client_profile_id");
        b.HasOne(x => x.ClientProfile).WithMany().HasForeignKey(x => x.ClientProfileId).HasConstraintName("fk_tools_client_profiles");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class ToolParameterConfiguration : IEntityTypeConfiguration<ToolParameter>
{
    public void Configure(EntityTypeBuilder<ToolParameter> b)
    {
        b.ToTable("tool_parameters");
        b.HasKey(x => x.Id).HasName("pk_tool_parameters");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ToolId).HasColumnName("tool_id");
        b.HasOne(x => x.Tool).WithMany(t => t.Parameters).HasForeignKey(x => x.ToolId).HasConstraintName("fk_tool_parameters_tools");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(x => x.Type).HasColumnName("type").HasMaxLength(128).IsRequired();
        b.Property(x => x.Direction).HasColumnName("direction").HasConversion<string>();
        b.Property(x => x.Required).HasColumnName("required");
        b.Property(x => x.DefaultValue).HasColumnName("default_value").HasColumnType("jsonb");
        b.Property(x => x.Options).HasColumnName("options").HasColumnType("jsonb");
        b.Property(x => x.Description).HasColumnName("description");
        b.HasIndex(x => new { x.ToolId, x.Name, x.Direction }).IsUnique().HasDatabaseName("ux_tool_parameters_tool_name_dir");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class ToolProviderSupportConfiguration : IEntityTypeConfiguration<ToolProviderSupport>
{
    public void Configure(EntityTypeBuilder<ToolProviderSupport> b)
    {
        b.ToTable("tool_provider_supports");
        b.HasKey(x => x.Id).HasName("pk_tool_provider_supports");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ToolId).HasColumnName("tool_id");
        b.HasOne(x => x.Tool).WithMany().HasForeignKey(x => x.ToolId).HasConstraintName("fk_tool_provider_supports_tools");
        b.Property(x => x.ProviderId).HasColumnName("provider_id");
        b.HasOne(x => x.Provider).WithMany().HasForeignKey(x => x.ProviderId).HasConstraintName("fk_tool_provider_supports_providers");
        b.Property(x => x.ModelId).HasColumnName("model_id");
        b.HasOne(x => x.Model).WithMany().HasForeignKey(x => x.ModelId).HasConstraintName("fk_tool_provider_supports_models");
        b.Property(x => x.SupportLevel).HasColumnName("support_level").HasConversion<string>();
        b.Property(x => x.Notes).HasColumnName("notes");
        b.HasIndex(x => new { x.ToolId, x.ProviderId, x.ModelId }).IsUnique().HasDatabaseName("ux_tool_provider_support_key");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class ToolExecutionLogConfiguration : IEntityTypeConfiguration<ToolExecutionLog>
{
    public void Configure(EntityTypeBuilder<ToolExecutionLog> b)
    {
        b.ToTable("tool_execution_logs");
        b.HasKey(x => x.Id).HasName("pk_tool_execution_logs");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ToolId).HasColumnName("tool_id");
        b.HasOne(x => x.Tool).WithMany().HasForeignKey(x => x.ToolId).HasConstraintName("fk_tool_execution_logs_tools");
        b.Property(x => x.AgentId).HasColumnName("agent_id");
        b.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc");
        b.Property(x => x.DurationMs).HasColumnName("duration_ms");
        b.Property(x => x.Success).HasColumnName("success");
        b.Property(x => x.Request).HasColumnName("request").HasColumnType("jsonb");
        b.Property(x => x.Response).HasColumnName("response").HasColumnType("jsonb");
        b.Property(x => x.Error).HasColumnName("error");
        b.HasIndex(x => new { x.ToolId, x.StartedAtUtc }).HasDatabaseName("ix_tool_execution_logs_tool_time");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}
