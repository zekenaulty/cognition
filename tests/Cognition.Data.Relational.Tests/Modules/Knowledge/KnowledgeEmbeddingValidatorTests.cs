using Cognition.Data.Relational.Modules.Knowledge;
using FluentAssertions;
using Xunit;

namespace Cognition.Data.Relational.Tests.Modules.Knowledge;

public class KnowledgeEmbeddingValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ShouldThrow_WhenVectorDimensionsMismatch()
    {
        var embedding = new KnowledgeEmbedding
        {
            Vector = new[] { 0.1f },
            Dimensions = 2
        };

        Func<Task> act = () => KnowledgeEmbeddingValidator.ValidateAsync(embedding, (_, _) => Task.FromResult(false), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*vector length*");
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrow_WhenNormalizedEmbeddingHasInvalidL2()
    {
        var embedding = new KnowledgeEmbedding
        {
            Vector = new[] { 1f, 1f },
            Dimensions = 2,
            Normalized = true,
            VectorL2Norm = 0.5
        };

        Func<Task> act = () => KnowledgeEmbeddingValidator.ValidateAsync(embedding, (_, _) => Task.FromResult(false), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Normalized but L2 norm*");
    }

    [Fact]
    public async Task ValidateAsync_ShouldComputeL2_WhenMissing()
    {
        var embedding = new KnowledgeEmbedding
        {
            Vector = new[] { 0.6f, 0.8f },
            Dimensions = 2,
            Normalized = true,
            VectorL2Norm = null
        };

        await KnowledgeEmbeddingValidator.ValidateAsync(embedding, (_, _) => Task.FromResult(false), CancellationToken.None);

        embedding.VectorL2Norm.Should().NotBeNull().And.BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrow_WhenDuplicateExists()
    {
        var embedding = new KnowledgeEmbedding
        {
            KnowledgeItemId = Guid.NewGuid(),
            Model = "model",
            ModelVersion = "v1",
            ChunkIndex = 1,
            Vector = new[] { 0.1f },
            Dimensions = 1
        };

        Func<Task> act = () => KnowledgeEmbeddingValidator.ValidateAsync(embedding, (_, _) => Task.FromResult(true), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Duplicate KnowledgeEmbedding*");
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_ForValidEmbedding()
    {
        var embedding = new KnowledgeEmbedding
        {
            Vector = new[] { 1f, 0f },
            Dimensions = 2,
            Normalized = false
        };

        await KnowledgeEmbeddingValidator.ValidateAsync(embedding, (_, _) => Task.FromResult(false), CancellationToken.None);
    }
}
