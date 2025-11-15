using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Cognition.Contracts.Scopes;

public static class ScopeTokenFactory
{
    public static bool TryCreateScopeToken(IReadOnlyDictionary<string, object?>? metadata, out ScopeToken scope)
    {
        scope = default;
        if (metadata is null || metadata.Count == 0)
        {
            return false;
        }

        var tenantId = TryGetGuid(metadata, "tenantId", "TenantId");
        var appId = TryGetGuid(metadata, "appId", "AppId");
        var personaId = TryGetGuid(metadata, "personaId", "PersonaId");
        var agentId = TryGetGuid(metadata, "agentId", "AgentId");
        var conversationId = TryGetGuid(metadata, "conversationId", "ConversationId");
        var planId = TryGetGuid(metadata, "planId", "PlanId");
        var projectId = TryGetGuid(metadata, "projectId", "ProjectId");
        var worldId = TryGetGuid(metadata, "worldId", "WorldId");

        if (tenantId is null &&
            appId is null &&
            personaId is null &&
            agentId is null &&
            conversationId is null &&
            planId is null &&
            projectId is null &&
            worldId is null)
        {
            return false;
        }

        scope = new ScopeToken(tenantId, appId, personaId, agentId, conversationId, planId, projectId, worldId);
        return true;
    }

    private static Guid? TryGetGuid(IReadOnlyDictionary<string, object?> metadata, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!metadata.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            if (value is Guid guid)
            {
                return guid;
            }

            if (value is string s && Guid.TryParse(s, out var parsed))
            {
                return parsed;
            }

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.String && Guid.TryParse(jsonElement.GetString(), out var parsedFromJson))
                {
                    return parsedFromJson;
                }
            }
        }

        return null;
    }
}
