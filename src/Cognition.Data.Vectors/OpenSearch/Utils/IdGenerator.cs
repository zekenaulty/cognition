namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Utils;

public static class IdGenerator
{
    public static string FromGuids(Guid a, Guid b)
    {
        // Deterministic composite string id
        Span<byte> bytes = stackalloc byte[32];
        a.TryWriteBytes(bytes);
        b.TryWriteBytes(bytes[16..]);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

