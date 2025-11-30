using System;

namespace Cognition.Clients.Tools.Sandbox;

public sealed class ToolSandboxOptions
{
    public const string SectionName = "Sandbox";

    public SandboxMode Mode { get; set; } = SandboxMode.Audit;

    /// <summary>
    /// When true, denied executions will be enqueued for approval rather than outright failure.
    /// </summary>
    public bool EnqueueOnDeny { get; set; }

    /// <summary>
    /// Class paths that are permitted to run without sandbox enforcement (for migration/dev only).
    /// </summary>
    public string[] AllowedUnsafeClassPaths { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Tool IDs that are permitted to run without sandbox enforcement (for migration/dev only).
    /// </summary>
    public Guid[] AllowedUnsafeToolIds { get; set; } = Array.Empty<Guid>();
}
