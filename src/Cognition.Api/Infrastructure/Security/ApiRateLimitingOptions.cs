using System.Threading.RateLimiting;

namespace Cognition.Api.Infrastructure.Security;

public sealed class ApiRateLimitingOptions
{
    public const string SectionName = "ApiRateLimiting";
    public const string UserPolicyName = "api-per-user";
    public const string PersonaPolicyName = "api-per-persona";
    public const string AgentPolicyName = "api-per-agent";

    public FixedWindowLimiterSettings? Global { get; set; } = new()
    {
        PermitLimit = 120,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public FixedWindowLimiterSettings? PerUser { get; set; } = new()
    {
        PermitLimit = 60,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public FixedWindowLimiterSettings? PerPersona { get; set; } = new()
    {
        PermitLimit = 45,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public FixedWindowLimiterSettings? PerAgent { get; set; }
        = new FixedWindowLimiterSettings
        {
            PermitLimit = 45,
            WindowSeconds = 60,
            QueueLimit = 0
        };

    public long? MaxRequestBodyBytes { get; set; } = 1_048_576;
}

public sealed class FixedWindowLimiterSettings
{
    public int PermitLimit { get; set; } = 60;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; } = 0;
    public QueueProcessingOrder QueueProcessingOrder { get; set; } = QueueProcessingOrder.OldestFirst;
    public bool AutoReplenishment { get; set; } = true;

    public bool IsEnabled => PermitLimit > 0 && WindowSeconds > 0;

    public FixedWindowRateLimiterOptions ToLimiterOptions()
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = PermitLimit,
            Window = TimeSpan.FromSeconds(WindowSeconds),
            QueueLimit = Math.Max(QueueLimit, 0),
            QueueProcessingOrder = QueueProcessingOrder,
            AutoReplenishment = AutoReplenishment
        };
    }
}
