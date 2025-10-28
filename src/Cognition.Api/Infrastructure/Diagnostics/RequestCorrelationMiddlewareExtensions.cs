using Microsoft.AspNetCore.Builder;

namespace Cognition.Api.Infrastructure.Diagnostics;

public static class RequestCorrelationMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestCorrelation(this IApplicationBuilder app)
    {
        if (app is null) throw new ArgumentNullException(nameof(app));
        return app.UseMiddleware<RequestCorrelationMiddleware>();
    }
}
