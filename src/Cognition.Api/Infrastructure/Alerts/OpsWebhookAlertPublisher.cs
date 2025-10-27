using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using Cognition.Api.Infrastructure.Planning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cognition.Api.Infrastructure.Alerts;

public sealed class OpsWebhookAlertPublisher : IPlannerAlertPublisher
{
    private static readonly Dictionary<string, OpsAlertRoute> EmptyRoutes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, TimeSpan> EmptyThresholds = new(StringComparer.OrdinalIgnoreCase);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpsAlertingOptions _options;
    private readonly ILogger<OpsWebhookAlertPublisher> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _lastPublished = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTime> _firstObserved = new(StringComparer.OrdinalIgnoreCase);

    public OpsWebhookAlertPublisher(
        IHttpClientFactory httpClientFactory,
        IOptions<OpsAlertingOptions> options,
        ILogger<OpsWebhookAlertPublisher> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync(IReadOnlyList<PlannerHealthAlert> alerts, CancellationToken ct)
    {
        if (!_options.Enabled || alerts.Count == 0)
        {
            return;
        }

        var actionable = PrepareAlerts(alerts);
        if (actionable.Count == 0)
        {
            return;
        }

        var grouped = actionable.GroupBy(x => new RouteKey(x.WebhookUrl, x.RoutingKey ?? _options.RoutingKey)).ToList();
        foreach (var group in grouped)
        {
            var webhook = group.Key.WebhookUrl ?? _options.WebhookUrl;
            if (string.IsNullOrWhiteSpace(webhook))
            {
                continue;
            }

            var payload = new
            {
                source = _options.Source ?? "cognition-api",
                environment = _options.Environment ?? "local",
                routingKey = group.Key.RoutingKey,
                alerts = group.Select(a => new
                {
                    a.Alert.Id,
                    severity = a.Alert.Severity.ToString().ToLowerInvariant(),
                    a.Alert.Title,
                    a.Alert.Description,
                    a.Alert.GeneratedAtUtc,
                    sloMinutes = a.SloThreshold?.TotalMinutes,
                    sloBreached = a.SloBreached
                })
            };

            try
            {
                var client = _httpClientFactory.CreateClient("ops-alerts");
                using var response = await client.PostAsJsonAsync(webhook, payload, ct).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    _logger.LogWarning("Ops alert webhook returned {StatusCode}: {Body}", response.StatusCode, body);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Ops alert webhook call failed.");
            }
        }
    }

    private List<PreparedAlert> PrepareAlerts(IReadOnlyList<PlannerHealthAlert> alerts)
    {
        var now = DateTime.UtcNow;
        var allowedSeverities = _options.SeverityFilter is { Count: > 0 }
            ? new HashSet<string>(_options.SeverityFilter, StringComparer.OrdinalIgnoreCase)
            : null;
        var routes = _options.Routes ?? EmptyRoutes;
        var sloThresholds = _options.AlertSloThresholds ?? EmptyThresholds;

        var actionable = new List<PreparedAlert>();
        foreach (var alert in alerts)
        {
            var severity = alert.Severity.ToString();
            if (allowedSeverities is not null && !allowedSeverities.Contains(severity))
            {
                continue;
            }

            var route = ResolveRoute(routes, alert);
            var webhook = route?.WebhookUrl ?? _options.WebhookUrl;
            if (string.IsNullOrWhiteSpace(webhook))
            {
                continue;
            }

            var debounceWindow = _options.DebounceWindow;
            var debounceKey = $"{alert.Id}:{alert.Severity}:{webhook}:{route?.RoutingKey ?? _options.RoutingKey}";
            if (_lastPublished.TryGetValue(debounceKey, out var last) && now - last < debounceWindow)
            {
                continue;
            }

            _lastPublished[debounceKey] = now;

            var firstKey = alert.Id;
            var firstSeen = _firstObserved.AddOrUpdate(firstKey, now, (_, existing) => existing);

            var sloThreshold = ResolveSloThreshold(sloThresholds, alert);
            var sloBreached = sloThreshold.HasValue && now - firstSeen >= sloThreshold.Value;

            actionable.Add(new PreparedAlert(alert, route?.RoutingKey, webhook, sloThreshold, sloBreached));
        }

        return actionable;
    }

    private static OpsAlertRoute? ResolveRoute(Dictionary<string, OpsAlertRoute> routes, PlannerHealthAlert alert)
    {
        if (routes.Count == 0)
        {
            return null;
        }

        var severityKey = $"severity:{alert.Severity.ToString().ToLowerInvariant()}";
        if (routes.TryGetValue($"alert:{alert.Id}", out var route))
        {
            return route;
        }

        if (routes.TryGetValue(severityKey, out route))
        {
            return route;
        }

        if (routes.TryGetValue("default", out route))
        {
            return route;
        }

        return null;
    }

    private static TimeSpan? ResolveSloThreshold(Dictionary<string, TimeSpan> thresholds, PlannerHealthAlert alert)
    {
        if (thresholds.Count == 0)
        {
            return null;
        }

        if (thresholds.TryGetValue($"alert:{alert.Id}", out var threshold))
        {
            return threshold;
        }

        if (thresholds.TryGetValue($"severity:{alert.Severity.ToString().ToLowerInvariant()}", out threshold))
        {
            return threshold;
        }

        return null;
    }

    private readonly record struct RouteKey(string? WebhookUrl, string? RoutingKey);

    private readonly record struct PreparedAlert(
        PlannerHealthAlert Alert,
        string? RoutingKey,
        string WebhookUrl,
        TimeSpan? SloThreshold,
        bool SloBreached);
}
