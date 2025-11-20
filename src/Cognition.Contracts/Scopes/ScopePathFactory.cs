using System;
using System.Collections.Generic;

namespace Cognition.Contracts.Scopes;

internal static class ScopePathFactory
{
    public static ScopePath Create(in ScopeToken scopeToken)
    {
        var principal = scopeToken.ResolvePrincipal();
        var segments = new List<ScopeSegment>(9);

#pragma warning disable RS0030 // Do not use banned APIs
        AddSegment("tenant", scopeToken.TenantId, principal, includeEvenIfPrincipal: false, segments);
        AddSegment("app", scopeToken.AppId, principal, includeEvenIfPrincipal: false, segments);
        AddSegment("persona", scopeToken.PersonaId, principal, includeEvenIfPrincipal: false, segments);
        AddSegment("agent", scopeToken.AgentId, principal, includeEvenIfPrincipal: false, segments);
        AddSegment("conversation", scopeToken.ConversationId, principal, includeEvenIfPrincipal: true, segments);
        AddSegment("plan", scopeToken.PlanId, principal, includeEvenIfPrincipal: true, segments);
        AddSegment("project", scopeToken.ProjectId, principal, includeEvenIfPrincipal: true, segments);
        AddSegment("world", scopeToken.WorldId, principal, includeEvenIfPrincipal: true, segments);
#pragma warning restore RS0030 // Do not use banned APIs

        return ScopePath.Create(principal, segments);
    }

    private static void AddSegment(
        string key,
        Guid? value,
        ScopePrincipal principal,
        bool includeEvenIfPrincipal,
        ICollection<ScopeSegment> segments)
    {
        if (!value.HasValue)
        {
            return;
        }

        if (!includeEvenIfPrincipal &&
            !principal.IsEmpty &&
            string.Equals(principal.PrincipalType, key, StringComparison.Ordinal) &&
            principal.RootId == value.Value)
        {
            return;
        }

        segments.Add(ScopeSegment.FromGuid(key, value.Value));
    }
}
