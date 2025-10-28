using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Cognition.Api.Infrastructure.Security;

public static class ApiRateLimiterPartitionKeys
{
    private const string PersonaHeaderName = "X-Persona-Id";
    private const string AgentHeaderName = "X-Agent-Id";
    private const string PersonaQueryName = "personaId";
    private const string AgentQueryName = "agentId";

    public static string? ResolveUserId(HttpContext context)
    {
        if (context.User.Identity is { IsAuthenticated: true })
        {
            var value =
                context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                context.User.FindFirstValue(ClaimTypes.Name) ??
                context.User.FindFirstValue("sub");
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    public static string? ResolvePersonaId(HttpContext context)
    {
        if (TryParseGuidHeader(context.Request.Headers, PersonaHeaderName, out var fromHeader))
        {
            return fromHeader;
        }

        if (context.Request.Query.TryGetValue(PersonaQueryName, out var queryValues))
        {
            foreach (var value in queryValues)
            {
                if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
                {
                    return parsed.ToString("N");
                }
            }
        }

        var claim = context.User.FindFirst("primary_persona")?.Value;
        if (Guid.TryParse(claim, out var fromClaim) && fromClaim != Guid.Empty)
        {
            return fromClaim.ToString("N");
        }

        return null;
    }

    public static string? ResolveAgentId(HttpContext context)
    {
        if (TryParseGuidHeader(context.Request.Headers, AgentHeaderName, out var fromHeader))
        {
            return fromHeader;
        }

        if (context.Request.Query.TryGetValue(AgentQueryName, out var queryValues))
        {
            foreach (var value in queryValues)
            {
                if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
                {
                    return parsed.ToString("N");
                }
            }
        }

        return null;
    }

    private static bool TryParseGuidHeader(IHeaderDictionary headers, string headerName, out string? partitionKey)
    {
        partitionKey = null;
        if (!headers.TryGetValue(headerName, out var values))
        {
            return false;
        }

        foreach (var value in values)
        {
            if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
            {
                partitionKey = parsed.ToString("N");
                return true;
            }
        }

        return false;
    }
}
