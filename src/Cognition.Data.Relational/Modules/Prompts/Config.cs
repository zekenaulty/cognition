using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Prompts;

public class PromptTemplateConfiguration : IEntityTypeConfiguration<PromptTemplate>
{
    public void Configure(EntityTypeBuilder<PromptTemplate> b)
    {
        b.ToTable("prompt_templates");
        b.HasKey(x => x.Id).HasName("pk_prompt_templates");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(x => x.PromptType).HasColumnName("prompt_type").HasConversion<string>();
        b.Property(x => x.Template).HasColumnName("template").IsRequired();
        b.Property(x => x.Tokens).HasColumnName("tokens").HasColumnType("jsonb");
        b.Property(x => x.Example).HasColumnName("example");
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}
