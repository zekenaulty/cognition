namespace Cognition.Api.Infrastructure.Diagnostics;

public static class CorrelationConstants
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ContextItemName = "CorrelationId";
    public const string LoggerScopeKey = "CorrelationId";
}
