using System;

namespace Cognition.Jobs;

public sealed class FictionAutomationOptions
{
    public const string SectionName = "Fiction:Automation";

    public TimeSpan LoreAutoFulfillmentSla { get; set; } = TimeSpan.FromMinutes(45);
}
