namespace Cognition.Contracts;

public readonly record struct ScopeToken(
    Guid? TenantId,
    Guid? AppId,
    Guid? PersonaId,
    Guid? AgentId,
    Guid? ConversationId,
    Guid? ProjectId,
    Guid? WorldId
);

