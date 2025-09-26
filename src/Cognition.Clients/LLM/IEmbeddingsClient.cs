namespace Cognition.Clients.LLM;

public interface IEmbeddingsClient
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}

