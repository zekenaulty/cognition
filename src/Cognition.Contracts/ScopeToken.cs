using Cognition.Contracts.Scopes;

namespace Cognition.Contracts;

public readonly partial record struct ScopeToken(
    Guid? TenantId,
    Guid? AppId,
    Guid? PersonaId,
    Guid? AgentId,
    Guid? ConversationId,
    Guid? PlanId,
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

    public override string ToString()
    {
#pragma warning disable RS0030 // Do not use banned APIs
        return ScopePathFactory.Create(this).Canonical;
#pragma warning restore RS0030 // Do not use banned APIs
    }
}
