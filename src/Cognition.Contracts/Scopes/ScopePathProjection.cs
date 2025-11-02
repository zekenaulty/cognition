using System;
using System.Collections.Generic;

namespace Cognition.Contracts.Scopes;

public readonly record struct ScopePathProjection(
    string Canonical,
    string PrincipalType,
    string? PrincipalId,
    IReadOnlyList<ScopeSegment> Segments)
{
    public static bool TryCreate(ScopeToken scope, out ScopePathProjection projection)
    {
        var path = ScopePathFactory.Create(scope);
        if (path.Principal.IsEmpty)
        {
            projection = default;
            return false;
        }

        var principalId = path.Principal.RootId == Guid.Empty ? null : path.Principal.RootId.ToString("D");
        projection = new ScopePathProjection(
            Canonical: path.Canonical,
            PrincipalType: path.Principal.PrincipalType,
            PrincipalId: principalId,
            Segments: path.Segments.ToArray());
        return true;
    }

    public Dictionary<string, string> ToSegmentDictionary()
    {
        var dict = new Dictionary<string, string>(Segments.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var segment in Segments)
        {
            if (!dict.ContainsKey(segment.Key))
            {
                dict[segment.Key] = segment.Value;
            }
        }
        return dict;
    }
}
