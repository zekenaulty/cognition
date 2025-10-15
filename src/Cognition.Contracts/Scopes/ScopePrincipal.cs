using System.Globalization;

namespace Cognition.Contracts.Scopes;

/// <summary>
/// Defines the root steward of a scope path (e.g. tenant, app, persona, agent).
/// </summary>
public readonly record struct ScopePrincipal
{
    public static ScopePrincipal None { get; } = new(Guid.Empty, "none");

    public ScopePrincipal(Guid rootId, string principalType)
    {
        RootId = rootId;
        PrincipalType = NormalizeType(principalType);
    }

    public Guid RootId { get; }

    public string PrincipalType { get; }

    public bool IsEmpty => RootId == Guid.Empty || string.Equals(PrincipalType, "none", StringComparison.Ordinal);

    public string Canonical => $"{PrincipalType}:{RootId:D}";

    public static ScopePrincipal From(string principalType, Guid rootId)
    {
        return new ScopePrincipal(rootId, principalType);
    }

    private static string NormalizeType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return "none";
        }

        return type.Trim().ToLower(CultureInfo.InvariantCulture);
    }
}
