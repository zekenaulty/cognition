using System;

namespace Cognition.Api.Infrastructure.Alerts;

public sealed class OpsAlertingOptions
{
    public const string SectionName = "OpsAlerting";

    public bool Enabled { get; set; }
    public string? WebhookUrl { get; set; }
    public string? RoutingKey { get; set; }
    public string? Environment { get; set; }
    public string? Source { get; set; }
    public TimeSpan DebounceWindow { get; set; } = TimeSpan.FromMinutes(5);
    public IReadOnlyCollection<string>? SeverityFilter { get; set; }
    public Dictionary<string, OpsAlertRoute>? Routes { get; set; }
    public Dictionary<string, TimeSpan>? AlertSloThresholds { get; set; }
}

public sealed class OpsAlertRoute
{
    public string? WebhookUrl { get; set; }
    public string? RoutingKey { get; set; }
}
