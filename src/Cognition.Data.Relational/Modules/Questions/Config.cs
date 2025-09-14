using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Data.Relational.Modules.Questions;

public class QuestionCategoryConfiguration : IEntityTypeConfiguration<QuestionCategory>
{
    public void Configure(EntityTypeBuilder<QuestionCategory> b)
    {
        b.ToTable("question_categories");
        b.HasKey(x => x.Id).HasName("pk_question_categories");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(x => x.Description).HasColumnName("description");
        b.HasIndex(x => x.Key).IsUnique().HasDatabaseName("ux_question_categories_key");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> b)
    {
        b.ToTable("questions");
        b.HasKey(x => x.Id).HasName("pk_questions");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CategoryId).HasColumnName("category_id");
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).HasConstraintName("fk_questions_categories");
        b.Property(x => x.Text).HasColumnName("text").IsRequired();
        b.Property(x => x.Tags).HasColumnName("tags");
        b.Property(x => x.Difficulty).HasColumnName("difficulty");
        b.HasIndex(x => x.CategoryId).HasDatabaseName("ix_questions_category");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}
