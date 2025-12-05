using System;
using Cognition.Data.Relational.Modules.LLM;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Data.Relational.Configuration.LLM;

public sealed class LlmGlobalDefaultConfiguration : IEntityTypeConfiguration<LlmGlobalDefault>
{
    public void Configure(EntityTypeBuilder<LlmGlobalDefault> builder)
    {
        builder.ToTable("LlmGlobalDefaults");
        builder.HasIndex(x => x.ModelId);
        builder.HasIndex(x => new { x.IsActive, x.Priority });
        builder.HasOne(d => d.Model)
            .WithMany()
            .HasForeignKey(d => d.ModelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
