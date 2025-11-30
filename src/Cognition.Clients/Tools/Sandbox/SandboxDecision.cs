namespace Cognition.Clients.Tools.Sandbox;

public sealed record SandboxDecision(bool IsAllowed, bool AuditOnly, SandboxMode Mode, string? Reason)
{
    public static SandboxDecision Allow(SandboxMode mode, string? reason = null, bool auditOnly = false) => new(true, auditOnly, mode, reason);
    public static SandboxDecision Deny(SandboxMode mode, string? reason = null) => new(false, false, mode, reason);
}
