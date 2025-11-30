namespace Cognition.Clients.Tools.Sandbox;

public sealed record ToolSandboxWorkRequest(
    Guid ToolId,
    string ClassPath,
    IReadOnlyDictionary<string, object?> Args,
    ToolContext Context,
    ToolSandboxOptions Options);
