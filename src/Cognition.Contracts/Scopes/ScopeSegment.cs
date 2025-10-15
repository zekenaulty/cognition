using System.Globalization;

namespace Cognition.Contracts.Scopes;

/// <summary>
/// Represents a single context segment (key/value) within a scope path.
/// </summary>
public readonly record struct ScopeSegment
{
    public ScopeSegment(string key, string value)
    {
        Key = NormalizeKey(key);
        Value = NormalizeValue(value);
    }

    public string Key { get; }

    public string Value { get; }

    public bool IsEmpty => string.IsNullOrEmpty(Key) || string.IsNullOrEmpty(Value);

    public string Canonical => $"{Key}={Value}";

    public static ScopeSegment FromGuid(string key, Guid guid, string format = "D")
    {
        return new ScopeSegment(key, guid.ToString(format, CultureInfo.InvariantCulture));
    }

    private static string NormalizeKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        return key.Trim().ToLower(CultureInfo.InvariantCulture);
    }

    private static string NormalizeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim();
    }
}
