namespace Cognition.Clients.Tools.Sandbox;

public interface ISandboxTelemetry
{
    void RecordDecision(Guid toolId, string classPath, SandboxDecision decision, ToolContext context);
}
