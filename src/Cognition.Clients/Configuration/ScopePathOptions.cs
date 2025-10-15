namespace Cognition.Clients.Configuration;

/// <summary>
/// Feature flags and configuration for scope-path aware behaviors.
/// </summary>
public sealed class ScopePathOptions
{
    public const string SectionName = "ScopePath";

    /// <summary>
    /// When enabled, content hashes include the canonical scope path representation.
    /// </summary>
    public bool PathAwareHashingEnabled { get; set; }

    /// <summary>
    /// When enabled, persistence layers dual-write the canonical scope principal and path metadata.
    /// </summary>
    public bool DualWriteEnabled { get; set; }
}
