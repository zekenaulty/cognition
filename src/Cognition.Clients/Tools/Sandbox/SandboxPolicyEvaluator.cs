using Cognition.Data.Relational.Modules.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cognition.Clients.Tools.Sandbox;

public sealed class SandboxPolicyEvaluator : ISandboxPolicyEvaluator
{
    private readonly IOptionsMonitor<ToolSandboxOptions> _options;
    private readonly ILogger<SandboxPolicyEvaluator> _logger;

    public SandboxPolicyEvaluator(IOptionsMonitor<ToolSandboxOptions> options, ILogger<SandboxPolicyEvaluator> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public SandboxDecision Evaluate(Tool tool, ToolContext context)
    {
        if (tool is null) throw new ArgumentNullException(nameof(tool));
        if (context is null) throw new ArgumentNullException(nameof(context));

        var current = _options.CurrentValue ?? new ToolSandboxOptions();
        var mode = current.Mode;

        if (mode == SandboxMode.Disabled)
        {
            return SandboxDecision.Allow(mode, "Sandbox disabled (mode=Disabled).");
        }

        var classPath = tool.ClassPath ?? string.Empty;
        var allowlisted =
            (current.AllowedUnsafeClassPaths?.Contains(classPath, StringComparer.OrdinalIgnoreCase) ?? false) ||
            (current.AllowedUnsafeToolIds?.Contains(tool.Id) ?? false);

        if (allowlisted)
        {
            if (mode == SandboxMode.Audit)
            {
                _logger.LogWarning("Sandbox audit mode: allowlisted unsafe tool {ToolId} {ClassPath}", tool.Id, classPath);
                return SandboxDecision.Allow(mode, "Allowlisted in audit mode.", auditOnly: true);
            }

            return SandboxDecision.Allow(mode, "Allowlisted for unsafe execution.");
        }

        if (mode == SandboxMode.Audit)
        {
            _logger.LogWarning("Sandbox audit mode: allowing unsandboxed execution for tool {ToolId} {ClassPath}", tool.Id, classPath);
            return SandboxDecision.Allow(mode, "Sandbox audit mode allows unsandboxed execution.", auditOnly: true);
        }

        _logger.LogWarning("Sandbox enforcement denied tool {ToolId} {ClassPath}", tool.Id, classPath);
        return SandboxDecision.Deny(mode, "Sandbox enforcement enabled and tool is not allowlisted for unsafe execution.");
    }
}
