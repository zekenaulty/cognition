namespace Cognition.Data.Relational.Modules.Knowledge;

public static class KnowledgeEmbeddingValidator
{
    public static async Task ValidateAsync(
        KnowledgeEmbedding embedding,
        Func<KnowledgeEmbedding, CancellationToken, Task<bool>> duplicateExists,
        CancellationToken cancellationToken)
    {
        var vector = embedding.Vector ?? Array.Empty<float>();

        if (embedding.Dimensions.HasValue && vector.Length != embedding.Dimensions.Value)
        {
            throw new InvalidOperationException($"KnowledgeEmbedding vector length ({vector.Length}) does not match Dimensions ({embedding.Dimensions}).");
        }

        if (embedding.Normalized == true)
        {
            double l2 = embedding.VectorL2Norm ?? Math.Sqrt(vector.Sum(v => v * (double)v));
            if (Math.Abs(l2 - 1.0) > 1e-3)
            {
                throw new InvalidOperationException($"KnowledgeEmbedding marked Normalized but L2 norm {l2:0.########} (expected ~1.0).");
            }

            if (embedding.VectorL2Norm == null)
            {
                embedding.VectorL2Norm = l2;
            }
        }

        if (embedding.Model != null && embedding.ModelVersion != null && embedding.ChunkIndex.HasValue)
        {
            if (await duplicateExists(embedding, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Duplicate KnowledgeEmbedding for (KnowledgeItemId, Model, ModelVersion, ChunkIndex).");
            }
        }
    }
}