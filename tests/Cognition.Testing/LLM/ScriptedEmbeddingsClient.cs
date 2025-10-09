using Cognition.Clients.LLM;

namespace Cognition.Testing.LLM;

public sealed class ScriptedEmbeddingsClient : IEmbeddingsClient
{
    private readonly List<(Func<string, bool> Match, Func<float[]> Factory)> _rules = new();

    public float[] DefaultEmbedding { get; set; } = new[] { 1f };

    public ScriptedEmbeddingsClient When(Func<string, bool> predicate, float[] embedding)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }
        if (embedding is null)
        {
            throw new ArgumentNullException(nameof(embedding));
        }

        _rules.Add((predicate, () => (float[])embedding.Clone()));
        return this;
    }

    public ScriptedEmbeddingsClient When(Func<string, bool> predicate, Func<float[]> factory)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _rules.Add((predicate, factory));
        return this;
    }

    public Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        foreach (var rule in _rules)
        {
            if (rule.Match(text))
            {
                var vector = rule.Factory();
                return Task.FromResult(vector.Length == 0 ? DefaultEmbedding : vector);
            }
        }

        return Task.FromResult(DefaultEmbedding);
    }
}
