using Cognition.Data.Relational.Modules.Knowledge;

namespace Cognition.Data.Relational.Tests;

public class KnowledgeEmbeddingValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WithDimensionMismatch_Throws()
    {
        var embedding = new KnowledgeEmbedding
        {
            KnowledgeItemId = Guid.NewGuid(),
            Vector = new[] { 0.1f, 0.2f },
            Dimensions = 3
        };

        var act = async () => await KnowledgeEmbeddingValidator.ValidateAsync(embedding, NoDuplicateAsync, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("KnowledgeEmbedding vector length (*) does not match Dimensions (*).");
    }

    [Fact]
    public async Task ValidateAsync_WithNonUnitNormalizedVector_Throws()
    {
        var embedding = new KnowledgeEmbedding
        {
            KnowledgeItemId = Guid.NewGuid(),
            Vector = new[] { 1f, 1f },
            Normalized = true
        };

        var act = async () => await KnowledgeEmbeddingValidator.ValidateAsync(embedding, NoDuplicateAsync, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*L2 norm*expected ~1.0*");
    }

    [Fact]
    public async Task ValidateAsync_WithNormalizedVector_ComputesVectorL2Norm()
    {
        var embedding = new KnowledgeEmbedding
        {
            KnowledgeItemId = Guid.NewGuid(),
            Vector = new[] { 0.70710677f, 0.70710677f },
            Normalized = true,
            VectorL2Norm = null
        };

        await KnowledgeEmbeddingValidator.ValidateAsync(embedding, NoDuplicateAsync, CancellationToken.None);

        embedding.VectorL2Norm.Should().NotBeNull();
        embedding.VectorL2Norm!.Value.Should().BeApproximately(1d, 1e-6);
    }

    [Fact]
    public async Task ValidateAsync_WhenDuplicateDetected_Throws()
    {
        var embedding = new KnowledgeEmbedding
        {
            KnowledgeItemId = Guid.NewGuid(),
            Model = "test-model",
            ModelVersion = "v1",
            ChunkIndex = 1,
            Vector = new[] { 0.3f, 0.4f }
        };

        var act = async () => await KnowledgeEmbeddingValidator.ValidateAsync(embedding, DuplicateAsync, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Duplicate KnowledgeEmbedding for (KnowledgeItemId, Model, ModelVersion, ChunkIndex).");
    }

    [Fact]
    public async Task ValidateAsync_DoesNotInvokeDuplicateCheckWhenIdentifiersMissing()
    {
        var embedding = new KnowledgeEmbedding
        {
            KnowledgeItemId = Guid.NewGuid(),
            Vector = new[] { 0.1f, 0.2f },
            Model = null,
            ModelVersion = null,
            ChunkIndex = null
        };

        var invoked = false;
        Task<bool> DuplicateProbe(KnowledgeEmbedding _, CancellationToken __)
        {
            invoked = true;
            return Task.FromResult(false);
        }

        await KnowledgeEmbeddingValidator.ValidateAsync(embedding, DuplicateProbe, CancellationToken.None);

        invoked.Should().BeFalse();
    }

    private static Task<bool> NoDuplicateAsync(KnowledgeEmbedding _, CancellationToken __) => Task.FromResult(false);

    private static Task<bool> DuplicateAsync(KnowledgeEmbedding _, CancellationToken __) => Task.FromResult(true);
}