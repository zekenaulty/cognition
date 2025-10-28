using Microsoft.AspNetCore.Http;

namespace Cognition.Api.Infrastructure.Diagnostics;

public static class CorrelationHttpContextExtensions
{
    public static string? GetCorrelationId(this HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationConstants.ContextItemName, out var itemValue) && itemValue is string str && !string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        if (context.Request.Headers.TryGetValue(CorrelationConstants.HeaderName, out var headerValues))
        {
            foreach (var headerValue in headerValues)
            {
                if (!string.IsNullOrWhiteSpace(headerValue))
                {
                    return headerValue;
                }
            }
        }

        return null;
    }
}
