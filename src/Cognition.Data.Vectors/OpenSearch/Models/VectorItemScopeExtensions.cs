using System.Collections.Generic;
using Cognition.Contracts;
using Cognition.Contracts.Scopes;

namespace Cognition.Data.Vectors.OpenSearch.OpenSearch.Models;

public static class VectorItemScopeExtensions
{
    public static bool ApplyScopeFromMetadata(this VectorItem item, IReadOnlyDictionary<string, object?>? properties, Dictionary<string, object> targetMetadata)
    {
        if (!ScopeTokenFactory.TryCreateScopeToken(properties, out var scope))
        {
            return false;
        }

        if (!ScopePathProjection.TryCreate(scope, out var projection))
        {
            return false;
        }

        item.ScopePath = projection.Canonical;
        item.ScopePrincipalType = projection.PrincipalType;
        item.ScopePrincipalId = projection.PrincipalId;
        var segments = projection.ToSegmentDictionary();
        item.ScopeSegments = segments;

        targetMetadata["ScopePath"] = item.ScopePath;
        targetMetadata["ScopePrincipalType"] = item.ScopePrincipalType ?? "none";
        if (!string.IsNullOrEmpty(item.ScopePrincipalId))
        {
            targetMetadata["ScopePrincipalId"] = item.ScopePrincipalId;
        }
        if (segments.Count > 0)
        {
            targetMetadata["ScopeSegments"] = segments;
        }

        return true;
    }
}
