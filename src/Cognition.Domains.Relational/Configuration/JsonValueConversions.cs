using System.Text.Json;
using Cognition.Domains.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cognition.Domains.Relational.Configuration;

internal static class JsonValueConversions
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static readonly ValueConverter<List<string>, string> StringListConverter = new(
        v => JsonSerializer.Serialize(v ?? new List<string>(), Options),
        v => string.IsNullOrWhiteSpace(v)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(v, Options) ?? new List<string>());

    internal static readonly ValueComparer<List<string>> StringListComparer = new(
        (l, r) => JsonSerializer.Serialize(l ?? new List<string>(), Options) ==
                  JsonSerializer.Serialize(r ?? new List<string>(), Options),
        v => JsonSerializer.Serialize(v ?? new List<string>(), Options).GetHashCode(),
        v => v == null ? new List<string>() : new List<string>(v));

    internal static readonly ValueConverter<List<ToolCategory>, string> ToolCategoryListConverter = new(
        v => JsonSerializer.Serialize((v ?? new List<ToolCategory>()).Select(x => x.ToString()).ToList(), Options),
        v => DeserializeEnumList<ToolCategory>(v));

    internal static readonly ValueComparer<List<ToolCategory>> ToolCategoryListComparer = new(
        (l, r) => JsonSerializer.Serialize(l ?? new List<ToolCategory>(), Options) ==
                  JsonSerializer.Serialize(r ?? new List<ToolCategory>(), Options),
        v => JsonSerializer.Serialize(v ?? new List<ToolCategory>(), Options).GetHashCode(),
        v => v == null ? new List<ToolCategory>() : new List<ToolCategory>(v));

    internal static readonly ValueConverter<Dictionary<string, string>, string> StringDictionaryConverter = new(
        v => JsonSerializer.Serialize(v ?? new Dictionary<string, string>(), Options),
        v => string.IsNullOrWhiteSpace(v)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(v, Options) ?? new Dictionary<string, string>());

    internal static readonly ValueComparer<Dictionary<string, string>> StringDictionaryComparer = new(
        (l, r) => JsonSerializer.Serialize(l ?? new Dictionary<string, string>(), Options) ==
                  JsonSerializer.Serialize(r ?? new Dictionary<string, string>(), Options),
        v => JsonSerializer.Serialize(v ?? new Dictionary<string, string>(), Options).GetHashCode(),
        v => v == null ? new Dictionary<string, string>() : new Dictionary<string, string>(v));

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

    private static List<TEnum> DeserializeEnumList<TEnum>(string? json) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<TEnum>();
        }

        var names = JsonSerializer.Deserialize<List<string>>(json, Options) ?? new List<string>();
        var results = new List<TEnum>(names.Count);
        foreach (var name in names)
        {
            if (Enum.TryParse<TEnum>(name, out var value))
            {
                results.Add(value);
            }
        }

        return results;
    }
}
