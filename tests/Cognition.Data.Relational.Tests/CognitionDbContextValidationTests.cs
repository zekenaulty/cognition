using System.Collections.Generic;
using Cognition.Data.Relational;
using Cognition.Data.Relational.Modules.Knowledge;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace Cognition.Data.Relational.Tests;

public class CognitionDbContextValidationTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldThrow_WhenEmbeddingVectorLengthDoesNotMatchDimensions()
    {
        await using var context = CreateContext();

        var knowledgeItem = new KnowledgeItem { Id = Guid.NewGuid(), Content = "vector" };
        context.KnowledgeItems.Add(knowledgeItem);
        context.KnowledgeEmbeddings.Add(new KnowledgeEmbedding
        {
            Id = Guid.NewGuid(),
            KnowledgeItemId = knowledgeItem.Id,
            KnowledgeItem = knowledgeItem,
            Dimensions = 3,
            Vector = new[] { 0.1f, 0.2f }
        });

        Func<Task> act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*vector length*");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldThrow_WhenNormalizedEmbeddingHasInvalidL2Norm()
    {
        await using var context = CreateContext();

        var knowledgeItem = new KnowledgeItem { Id = Guid.NewGuid(), Content = "normalized" };
        context.KnowledgeItems.Add(knowledgeItem);
        context.KnowledgeEmbeddings.Add(new KnowledgeEmbedding
        {
            Id = Guid.NewGuid(),
            KnowledgeItemId = knowledgeItem.Id,
            KnowledgeItem = knowledgeItem,
            Dimensions = 2,
            Vector = new[] { 1f, 1f },
            Normalized = true,
            VectorL2Norm = 0.5
        });

        Func<Task> act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Normalized but L2 norm*");
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldComputeVectorL2Norm_WhenMarkedNormalizedWithoutValue()
    {
        await using var context = CreateContext();

        var knowledgeItem = new KnowledgeItem { Id = Guid.NewGuid(), Content = "compute" };
        context.KnowledgeItems.Add(knowledgeItem);
        var embedding = new KnowledgeEmbedding
        {
            Id = Guid.NewGuid(),
            KnowledgeItemId = knowledgeItem.Id,
            KnowledgeItem = knowledgeItem,
            Dimensions = 2,
            Vector = new[] { 0.6f, 0.8f },
            Normalized = true,
            VectorL2Norm = null
        };
        context.KnowledgeEmbeddings.Add(embedding);

        await context.SaveChangesAsync();

        embedding.VectorL2Norm.Should().NotBeNull();
        embedding.VectorL2Norm!.Value.Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPreventDuplicateEmbeddingsForSameCombination()
    {
        await using var context = CreateContext();

        var knowledgeItem = new KnowledgeItem { Id = Guid.NewGuid(), Content = "duplicate" };
        context.KnowledgeItems.Add(knowledgeItem);

        var existing = new KnowledgeEmbedding
        {
            Id = Guid.NewGuid(),
            KnowledgeItemId = knowledgeItem.Id,
            KnowledgeItem = knowledgeItem,
            Dimensions = 2,
            Vector = new[] { 0.5f, 0.5f },
            Model = "model",
            ModelVersion = "v1",
            ChunkIndex = 0
        };
        context.KnowledgeEmbeddings.Add(existing);
        await context.SaveChangesAsync();

        context.KnowledgeEmbeddings.Add(new KnowledgeEmbedding
        {
            Id = Guid.NewGuid(),
            KnowledgeItemId = knowledgeItem.Id,
            KnowledgeItem = knowledgeItem,
            Dimensions = 2,
            Vector = new[] { 0.5f, 0.5f },
            Model = "model",
            ModelVersion = "v1",
            ChunkIndex = 0
        });

        Func<Task> act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Duplicate KnowledgeEmbedding*");
    }

    private static CognitionDbContext CreateContext()
    {
        using var conventionContext = new DbContext(new DbContextOptionsBuilder().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var conventionSet = ConventionSet.CreateConventionSet(conventionContext);
        var modelBuilder = new ModelBuilder(conventionSet);

        modelBuilder.Entity<KnowledgeItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Ignore(x => x.Properties);
            b.Ignore(x => x.Categories);
            b.Ignore(x => x.Keywords);
            b.Ignore(x => x.Embeddings);
        });

        modelBuilder.Entity<KnowledgeEmbedding>(b =>
        {
            b.HasKey(x => x.Id);
            b.Ignore(x => x.Metadata);
            b.Property(x => x.Vector);
            b.Ignore(x => x.ScopeSegments);
            b.Ignore(x => x.ScopePrincipalId);
            b.Ignore(x => x.ScopePrincipalType);
            b.Ignore(x => x.ScopePath);
            b.HasOne(x => x.KnowledgeItem)
                .WithMany()
                .HasForeignKey(x => x.KnowledgeItemId);
        });

        var mutableModel = modelBuilder.Model;
        var model = (IModel)mutableModel;

        var options = new DbContextOptionsBuilder<CognitionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseModel(model)
            .Options;

        return new CognitionDbContext(options);
    }
}

