using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Instructions;

public class InstructionConfiguration : IEntityTypeConfiguration<Instruction>
{
    public void Configure(EntityTypeBuilder<Instruction> b)
    {
        b.ToTable("instructions");
        b.HasKey(x => x.Id).HasName("pk_instructions");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>();
        b.Property(x => x.Content).HasColumnName("content").IsRequired();
        b.Property(x => x.RolePlay).HasColumnName("role_play");
        b.Property(x => x.Tags).HasColumnName("tags");
        b.Property(x => x.Version).HasColumnName("version");
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class InstructionSetConfiguration : IEntityTypeConfiguration<InstructionSet>
{
    public void Configure(EntityTypeBuilder<InstructionSet> b)
    {
        b.ToTable("instruction_sets");
        b.HasKey(x => x.Id).HasName("pk_instruction_sets");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.Name).HasDatabaseName("ix_instruction_sets_name");
        b.Property(x => x.Scope).HasColumnName("scope");
        b.Property(x => x.ScopeRefId).HasColumnName("scope_ref_id");
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class InstructionSetItemConfiguration : IEntityTypeConfiguration<InstructionSetItem>
{
    public void Configure(EntityTypeBuilder<InstructionSetItem> b)
    {
        b.ToTable("instruction_set_items");
        b.HasKey(x => x.Id).HasName("pk_instruction_set_items");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.InstructionSetId).HasColumnName("instruction_set_id");
        b.Property(x => x.InstructionId).HasColumnName("instruction_id");
        b.HasOne(x => x.InstructionSet).WithMany(s => s.Items).HasForeignKey(x => x.InstructionSetId).HasConstraintName("fk_instruction_set_items_sets");
        b.HasOne(x => x.Instruction).WithMany(i => i.InstructionSetItems).HasForeignKey(x => x.InstructionId).HasConstraintName("fk_instruction_set_items_instructions");
        b.Property(x => x.Order).HasColumnName("order");
        b.Property(x => x.Enabled).HasColumnName("enabled").HasDefaultValue(true);
        b.HasIndex(x => new { x.InstructionSetId, x.Order }).HasDatabaseName("ix_instruction_set_items_order");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}
