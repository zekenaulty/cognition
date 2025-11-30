using Cognition.Data.Relational.Modules.Tools;

namespace Cognition.Clients.Tools.Sandbox;

public interface ISandboxAlertPublisher
{
    Task PublishAsync(SandboxDecision decision, Tool tool, ToolContext context, CancellationToken ct);
}
