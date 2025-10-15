using System.Collections.Generic;
using Cognition.Contracts.Scopes;

namespace Cognition.Contracts;

public readonly partial record struct ScopeToken(
    Guid? TenantId,
    Guid? AppId,
    Guid? PersonaId,
    Guid? AgentId,
    Guid? ConversationId,
    Guid? ProjectId,
    Guid? WorldId
)
{
    public ScopePrincipal ResolvePrincipal()
    {
        if (AgentId.HasValue) return ScopePrincipal.From("agent", AgentId.Value);
        if (PersonaId.HasValue) return ScopePrincipal.From("persona", PersonaId.Value);
        if (AppId.HasValue) return ScopePrincipal.From("app", AppId.Value);
        if (TenantId.HasValue) return ScopePrincipal.From("tenant", TenantId.Value);
        return ScopePrincipal.None;
    }

    public ScopePath ToScopePath()
    {
        var principal = ResolvePrincipal();
        var segments = new List<ScopeSegment>(8);

        AddSegment("tenant", TenantId, principal);
        AddSegment("app", AppId, principal);
        AddSegment("persona", PersonaId, principal);
        AddSegment("agent", AgentId, principal);
        AddSegment("conversation", ConversationId, principal, includeEvenIfPrincipal: true);
        AddSegment("project", ProjectId, principal, includeEvenIfPrincipal: true);
        AddSegment("world", WorldId, principal, includeEvenIfPrincipal: true);

        return ScopePath.Create(principal, segments);

        void AddSegment(string key, Guid? value, ScopePrincipal targetPrincipal, bool includeEvenIfPrincipal = false)
        {
            if (!value.HasValue) return;
            if (!includeEvenIfPrincipal && !targetPrincipal.IsEmpty && string.Equals(targetPrincipal.PrincipalType, key, StringComparison.Ordinal) && targetPrincipal.RootId == value.Value)
            {
                return;
            }

            segments.Add(ScopeSegment.FromGuid(key, value.Value));
        }
    }

    public override string ToString()
    {
        return ToScopePath().Canonical;
    }
}
