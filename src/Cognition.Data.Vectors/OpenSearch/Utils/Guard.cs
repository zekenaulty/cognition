namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Utils;

public static class Guard
{
    public static void NotNullOrEmpty(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} is required.", name);
    }

    public static void EnsureDimension(float[]? vector, int expected)
    {
        if (vector is null)
            throw new ArgumentNullException(nameof(vector));
        if (vector.Length != expected)
            throw new ArgumentException($"Vector dimension mismatch. Expected {expected}, got {vector.Length}.");
    }
}

