using System;
using System.Collections.Generic;
using Cognition.Api.Infrastructure.Alerts;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cognition.Api.Tests.Infrastructure;

public class OpsAlertingOptionsValidatorTests
{
    private readonly OpsAlertingOptionsValidator _validator = new();

    [Fact]
    public void Validate_succeeds_when_disabled()
    {
        var options = new OpsAlertingOptions
        {
            Enabled = false
        };

        var result = _validator.Validate(Options.DefaultName, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_fails_when_enabled_without_webhook()
    {
        var options = new OpsAlertingOptions
        {
            Enabled = true,
            WebhookUrl = null,
            Routes = null
        };

        var result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("WebhookUrl");
    }

    [Fact]
    public void Validate_succeeds_when_route_provides_webhook()
    {
        var options = new OpsAlertingOptions
        {
            Enabled = true,
            WebhookUrl = null,
            Routes = new Dictionary<string, OpsAlertRoute>
            {
                ["alert:planner:recent-failures"] = new OpsAlertRoute
                {
                    WebhookUrl = "https://example.test/webhook",
                    RoutingKey = "planner"
                }
            }
        };

        var result = _validator.Validate(Options.DefaultName, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_fails_when_slo_threshold_non_positive()
    {
        var options = new OpsAlertingOptions
        {
            Enabled = true,
            WebhookUrl = "https://ops",
            AlertSloThresholds = new Dictionary<string, TimeSpan>
            {
                ["alert:planner:recent-failures"] = TimeSpan.Zero
            }
        };

        var result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("must be greater than zero");
    }

    [Fact]
    public void Validate_fails_when_debounce_negative()
    {
        var options = new OpsAlertingOptions
        {
            Enabled = true,
            WebhookUrl = "https://ops",
            DebounceWindow = TimeSpan.FromMinutes(-1)
        };

        var result = _validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DebounceWindow");
    }
}
