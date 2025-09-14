using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cognition.Data.Relational.Modules.Common;

namespace Cognition.Data.Relational.Modules.Knowledge;

public class KnowledgeItemConfiguration : IEntityTypeConfiguration<KnowledgeItem>
{
    public void Configure(EntityTypeBuilder<KnowledgeItem> b)
    {
        b.ToTable("knowledge_items");
        b.HasKey(x => x.Id).HasName("pk_knowledge_items");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ContentType).HasColumnName("content_type").HasConversion<string>();
        b.Property(x => x.Content).HasColumnName("content");
        b.Property(x => x.Categories).HasColumnName("categories");
        b.Property(x => x.Keywords).HasColumnName("keywords");
        b.Property(x => x.Source).HasColumnName("source");
        b.Property(x => x.Timestamp).HasColumnName("timestamp");
        b.Property(x => x.Properties).HasColumnName("properties").HasColumnType("jsonb");
        b.HasIndex(x => x.Timestamp).HasDatabaseName("ix_knowledge_items_timestamp");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        b.HasMany(x => x.Embeddings)
            .WithOne(e => e.KnowledgeItem)
            .HasForeignKey(e => e.KnowledgeItemId)
            .HasConstraintName("fk_knowledge_embeddings_item");
    }
}

public class KnowledgeRelationConfiguration : IEntityTypeConfiguration<KnowledgeRelation>
{
    public void Configure(EntityTypeBuilder<KnowledgeRelation> b)
    {
        b.ToTable("knowledge_relations");
        b.HasKey(x => x.Id).HasName("pk_knowledge_relations");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.FromKnowledgeItemId).HasColumnName("from_item_id");
        b.Property(x => x.ToKnowledgeItemId).HasColumnName("to_item_id");
        b.HasOne(x => x.FromKnowledgeItem).WithMany().HasForeignKey(x => x.FromKnowledgeItemId).HasConstraintName("fk_knowledge_relations_from");
        b.HasOne(x => x.ToKnowledgeItem).WithMany().HasForeignKey(x => x.ToKnowledgeItemId).HasConstraintName("fk_knowledge_relations_to");
        b.Property(x => x.RelationshipType).HasColumnName("relationship_type");
        b.Property(x => x.Weight).HasColumnName("weight");
        b.Property(x => x.Description).HasColumnName("description");
        b.HasIndex(x => new { x.FromKnowledgeItemId, x.ToKnowledgeItemId }).HasDatabaseName("ix_knowledge_relations_pair");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}

public class KnowledgeEmbeddingConfiguration : IEntityTypeConfiguration<KnowledgeEmbedding>
{
    public void Configure(EntityTypeBuilder<KnowledgeEmbedding> b)
    {
        b.ToTable("knowledge_embeddings");
        b.HasKey(x => x.Id).HasName("pk_knowledge_embeddings");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.KnowledgeItemId).HasColumnName("knowledge_item_id");
        b.Property(x => x.Label).HasColumnName("label");
        b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        b.Property(x => x.Vector).HasColumnName("vector");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.HasIndex(x => x.KnowledgeItemId).HasDatabaseName("ix_knowledge_embeddings_item_id");
    }
}
