using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Data.Relational.Modules.Agents;

public class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> b)
    {
        b.ToTable("agents");
        b.HasKey(x => x.Id).HasName("pk_agents");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.PersonaId).HasColumnName("persona_id");
        b.HasOne(x => x.Persona).WithMany().HasForeignKey(x => x.PersonaId).HasConstraintName("fk_agents_personas");
        b.Property(x => x.Version).HasColumnName("version");
        b.Property(x => x.RolePlay).HasColumnName("role_play");
        b.Property(x => x.Prefix).HasColumnName("prefix");
        b.Property(x => x.Suffix).HasColumnName("suffix");
        b.Property(x => x.ClientProfileId).HasColumnName("client_profile_id");
        b.HasOne(x => x.ClientProfile).WithMany().HasForeignKey(x => x.ClientProfileId).HasConstraintName("fk_agents_client_profiles");
        b.Property(x => x.State).HasColumnName("state").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class AgentToolBindingConfiguration : IEntityTypeConfiguration<AgentToolBinding>
{
    public void Configure(EntityTypeBuilder<AgentToolBinding> b)
    {
        b.ToTable("agent_tool_bindings");
        b.HasKey(x => x.Id).HasName("pk_agent_tool_bindings");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ScopeType).HasColumnName("scope_type");
        b.Property(x => x.ScopeId).HasColumnName("scope_id");
        b.Property(x => x.ToolId).HasColumnName("tool_id");
        b.HasOne(x => x.Tool).WithMany().HasForeignKey(x => x.ToolId).HasConstraintName("fk_agent_tool_bindings_tools");
        b.Property(x => x.Enabled).HasColumnName("enabled").HasDefaultValue(true);
        b.Property(x => x.Config).HasColumnName("config").HasColumnType("jsonb");
        b.HasIndex(x => new { x.ScopeType, x.ScopeId, x.ToolId }).IsUnique().HasDatabaseName("ux_agent_tool_bindings_scope_tool");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}
