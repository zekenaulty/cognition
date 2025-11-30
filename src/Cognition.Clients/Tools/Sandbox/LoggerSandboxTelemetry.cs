using Microsoft.Extensions.Logging;

namespace Cognition.Clients.Tools.Sandbox;

public sealed class LoggerSandboxTelemetry : ISandboxTelemetry
{
    private readonly ILogger<LoggerSandboxTelemetry> _logger;

    public LoggerSandboxTelemetry(ILogger<LoggerSandboxTelemetry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordDecision(Guid toolId, string classPath, SandboxDecision decision, ToolContext context)
    {
        var props = new
        {
            toolId,
            classPath,
            decision.Mode,
            decision.IsAllowed,
            decision.AuditOnly,
            decision.Reason,
            context.AgentId,
            context.PersonaId,
            context.ConversationId
        };

        if (!decision.IsAllowed)
        {
            _logger.LogWarning("Sandbox denied tool {ToolId} {ClassPath} mode={Mode} reason={Reason} ctx={@Context}", toolId, classPath, decision.Mode, decision.Reason, props);
        }
        else if (decision.AuditOnly)
        {
            _logger.LogWarning("Sandbox audit-only tool {ToolId} {ClassPath} mode={Mode} ctx={@Context}", toolId, classPath, decision.Mode, props);
        }
        else
        {
            _logger.LogDebug("Sandbox allowed tool {ToolId} {ClassPath} mode={Mode} ctx={@Context}", toolId, classPath, decision.Mode, props);
        }
    }
}
