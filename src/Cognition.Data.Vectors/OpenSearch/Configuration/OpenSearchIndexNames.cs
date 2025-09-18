namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Configuration;

public static class OpenSearchIndexNames
{
    public const string Knowledge = "vectors-knowledge";
    public const string Persona = "vectors-persona";
    public const string Messages = "vectors-messages";

    public static string ResolveIndex(string? kind, string tenantKey, string defaultIndex)
    {
        // If multi-index by kind is desired, switch here
        if (!string.IsNullOrWhiteSpace(kind))
        {
            return kind.ToLowerInvariant() switch
            {
                "knowledge" => Knowledge,
                "persona" => Persona,
                "message" or "messages" => Messages,
                _ => defaultIndex
            };
        }

        return defaultIndex;
    }
}

