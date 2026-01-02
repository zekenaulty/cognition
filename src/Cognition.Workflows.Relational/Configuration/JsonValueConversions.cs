using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cognition.Workflows.Relational.Configuration;

internal static class JsonValueConversions
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static readonly ValueConverter<Dictionary<string, object?>, string> ObjectDictionaryConverter = new(
        v => JsonSerializer.Serialize(v ?? new Dictionary<string, object?>(), Options),
        v => string.IsNullOrWhiteSpace(v)
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(v, Options) ?? new Dictionary<string, object?>());

    internal static readonly ValueComparer<Dictionary<string, object?>> ObjectDictionaryComparer = new(
        (l, r) => JsonSerializer.Serialize(l ?? new Dictionary<string, object?>(), Options) ==
                  JsonSerializer.Serialize(r ?? new Dictionary<string, object?>(), Options),
        v => JsonSerializer.Serialize(v ?? new Dictionary<string, object?>(), Options).GetHashCode(),
        v => v == null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(v));
}
