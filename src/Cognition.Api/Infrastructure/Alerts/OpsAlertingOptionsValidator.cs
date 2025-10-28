using Microsoft.Extensions.Options;

namespace Cognition.Api.Infrastructure.Alerts;

public sealed class OpsAlertingOptionsValidator : IValidateOptions<OpsAlertingOptions>
{
    public ValidateOptionsResult Validate(string? name, OpsAlertingOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.DebounceWindow < TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail("OpsAlerting: DebounceWindow cannot be negative.");
        }

        if (!HasConfiguredWebhook(options))
        {
            return ValidateOptionsResult.Fail("OpsAlerting is enabled but no WebhookUrl is configured. Set OpsAlerting:WebhookUrl or provide a route override with a WebhookUrl.");
        }

        if (options.AlertSloThresholds is { Count: > 0 })
        {
            foreach (var kvp in options.AlertSloThresholds)
            {
                if (kvp.Value <= TimeSpan.Zero)
                {
                    return ValidateOptionsResult.Fail($"OpsAlerting AlertSloThresholds entry '{kvp.Key}' must be greater than zero.");
                }
            }
        }

        return ValidateOptionsResult.Success;
    }

    private static bool HasConfiguredWebhook(OpsAlertingOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.WebhookUrl))
        {
            return true;
        }

        if (options.Routes is null || options.Routes.Count == 0)
        {
            return false;
        }

        foreach (var route in options.Routes.Values)
        {
            if (!string.IsNullOrWhiteSpace(route?.WebhookUrl))
            {
                return true;
            }
        }

        return false;
    }
}

