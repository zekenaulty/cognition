namespace Cognition.Clients.Tools.Sandbox;

public sealed class ToolSandboxAlertOptions
{
    public const string SectionName = "SandboxAlerts";

    public bool Enabled { get; set; }

    public string? WebhookUrl { get; set; }
}
