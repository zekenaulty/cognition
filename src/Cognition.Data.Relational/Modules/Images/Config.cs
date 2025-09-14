using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cognition.Data.Relational.Modules.Images;

public class ImageAssetConfiguration : IEntityTypeConfiguration<ImageAsset>
{
    public void Configure(EntityTypeBuilder<ImageAsset> b)
    {
        b.ToTable("image_assets");
        b.HasKey(x => x.Id).HasName("pk_image_assets");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ConversationId).HasColumnName("conversation_id");
        b.Property(x => x.CreatedByPersonaId).HasColumnName("created_by_persona_id");
        b.Property(x => x.Provider).HasColumnName("provider");
        b.Property(x => x.Model).HasColumnName("model");
        b.Property(x => x.MimeType).HasColumnName("mime_type");
        b.Property(x => x.Width).HasColumnName("width");
        b.Property(x => x.Height).HasColumnName("height");
        b.Property(x => x.Bytes).HasColumnName("bytes").HasColumnType("bytea");
        b.Property(x => x.Sha256).HasColumnName("sha256");
        b.Property(x => x.Prompt).HasColumnName("prompt");
        b.Property(x => x.NegativePrompt).HasColumnName("negative_prompt");
        b.Property(x => x.StyleId).HasColumnName("style_id");
        b.Property(x => x.Steps).HasColumnName("steps");
        b.Property(x => x.Guidance).HasColumnName("guidance");
        b.Property(x => x.Seed).HasColumnName("seed");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        b.HasOne(x => x.Conversation).WithMany().HasForeignKey(x => x.ConversationId).HasConstraintName("fk_image_assets_conversations");
        b.HasOne(x => x.CreatedByPersona).WithMany().HasForeignKey(x => x.CreatedByPersonaId).HasConstraintName("fk_image_assets_personas");
        b.HasOne(x => x.Style).WithMany().HasForeignKey(x => x.StyleId).HasConstraintName("fk_image_assets_styles");
        b.HasIndex(x => new { x.ConversationId, x.CreatedAtUtc }).HasDatabaseName("ix_image_assets_conversation_time");
        b.HasIndex(x => x.Sha256).HasDatabaseName("ix_image_assets_sha256");
    }
}

public class ImageStyleConfiguration : IEntityTypeConfiguration<ImageStyle>
{
    public void Configure(EntityTypeBuilder<ImageStyle> b)
    {
        b.ToTable("image_styles");
        b.HasKey(x => x.Id).HasName("pk_image_styles");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ux_image_styles_name");
        b.Property(x => x.Description).HasColumnName("description");
        b.Property(x => x.PromptPrefix).HasColumnName("prompt_prefix");
        b.Property(x => x.NegativePrompt).HasColumnName("negative_prompt");
        b.Property(x => x.Defaults).HasColumnName("defaults").HasColumnType("jsonb");
        b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}
