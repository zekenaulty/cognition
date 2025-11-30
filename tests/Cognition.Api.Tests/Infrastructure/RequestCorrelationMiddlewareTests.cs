using System.Threading.Tasks;
using Cognition.Api.Infrastructure.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cognition.Api.Tests.Infrastructure;

public class RequestCorrelationMiddlewareTests
{
    [Fact]
    public async Task AddsCorrelationHeaderAndTraceIdentifier()
    {
        var httpContext = new DefaultHttpContext();
        RequestDelegate terminal = ctx =>
        {
            Assert.True(ctx.Response.Headers.ContainsKey(CorrelationConstants.HeaderName));
            Assert.False(string.IsNullOrWhiteSpace(ctx.TraceIdentifier));
            return Task.CompletedTask;
        };

        var middleware = new RequestCorrelationMiddleware(terminal, NullLogger<RequestCorrelationMiddleware>.Instance);
        await middleware.InvokeAsync(httpContext);

        Assert.True(httpContext.Response.Headers.ContainsKey(CorrelationConstants.HeaderName));
        var header = httpContext.Response.Headers[CorrelationConstants.HeaderName];
        Assert.False(string.IsNullOrWhiteSpace(header));
        Assert.False(string.IsNullOrWhiteSpace(httpContext.TraceIdentifier));
    }
}
