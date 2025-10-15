using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;

public sealed class VectorItem
{
    public string Id { get; set; } = string.Empty;
    public string TenantKey { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public float[]? Embedding { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public int SchemaVersion { get; set; } = 1;
    public string? ScopePath { get; set; }
    public string? ScopePrincipalType { get; set; }
    public string? ScopePrincipalId { get; set; }
    public Dictionary<string, string>? ScopeSegments { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? Extensions { get; set; }
}
