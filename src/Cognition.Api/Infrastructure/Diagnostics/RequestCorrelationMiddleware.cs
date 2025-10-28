using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cognition.Api.Infrastructure.Diagnostics;

public sealed class RequestCorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestCorrelationMiddleware> _logger;

    public RequestCorrelationMiddleware(RequestDelegate next, ILogger<RequestCorrelationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[CorrelationConstants.ContextItemName] = correlationId;
        context.Response.Headers[CorrelationConstants.HeaderName] = correlationId;
        context.TraceIdentifier = correlationId;

        var activity = Activity.Current;
        Activity? createdActivity = null;
        if (activity is null)
        {
            createdActivity = new Activity("HttpRequest");
            createdActivity.SetIdFormat(ActivityIdFormat.W3C);
            createdActivity.Start();
            activity = createdActivity;
        }

        activity?.SetTag("correlation.id", correlationId);
        activity?.SetBaggage("correlation.id", correlationId);

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            [CorrelationConstants.LoggerScopeKey] = correlationId
        }))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                if (createdActivity is not null)
                {
                    createdActivity.Stop();
                }
            }
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationConstants.ContextItemName, out var existing) &&
            existing is string stored &&
            !string.IsNullOrWhiteSpace(stored))
        {
            return stored;
        }

        if (context.Request.Headers.TryGetValue(CorrelationConstants.HeaderName, out var headerValues))
        {
            foreach (var value in headerValues)
            {
                if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
                {
                    return parsed.ToString("N");
                }
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}
