using Cognition.Api.Infrastructure.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cognition.Api.Tests.Infrastructure;

public class RequestCorrelationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AssignsCorrelationId_WhenHeaderMissing()
    {
        string? observedCorrelation = null;
        RequestDelegate next = context =>
        {
            observedCorrelation = context.GetCorrelationId();
            return Task.CompletedTask;
        };

        var middleware = new RequestCorrelationMiddleware(next, NullLogger<RequestCorrelationMiddleware>.Instance);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.False(string.IsNullOrWhiteSpace(observedCorrelation));
        Assert.Equal(observedCorrelation, context.Response.Headers[CorrelationConstants.HeaderName]);
    }

    [Fact]
    public async Task InvokeAsync_PreservesIncomingCorrelationHeader()
    {
        var incomingId = Guid.NewGuid().ToString("N");
        string? observedCorrelation = null;
        RequestDelegate next = context =>
        {
            observedCorrelation = context.GetCorrelationId();
            return Task.CompletedTask;
        };

        var middleware = new RequestCorrelationMiddleware(next, NullLogger<RequestCorrelationMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationConstants.HeaderName] = incomingId;

        await middleware.InvokeAsync(context);

        Assert.Equal(incomingId, observedCorrelation);
        Assert.Equal(incomingId, context.Response.Headers[CorrelationConstants.HeaderName]);
    }
}
