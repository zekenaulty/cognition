using Cognition.Clients.Retrieval;
using Cognition.Contracts;

namespace Cognition.Clients.Tools;

public class AgentRememberTool : ITool
{
    public string Name => "Agent Remember";
    public string ClassPath => typeof(AgentRememberTool).FullName! + ", " + typeof(AgentRememberTool).Assembly.GetName().Name;

    private readonly IRetrievalService _retrieval;
    public AgentRememberTool(IRetrievalService retrieval) => _retrieval = retrieval;

    public async Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        if (!ctx.AgentId.HasValue) throw new InvalidOperationException("AgentId required");
        var text = (args.TryGetValue("text", out var v) ? v as string : null) ?? string.Empty;
        var metadata = args.TryGetValue("metadata", out var m) && m is Dictionary<string, object?> md ? md : new Dictionary<string, object?>();
        metadata["Source"] = metadata.ContainsKey("Source") ? metadata["Source"] : "tool_remember";
        var scope = new ScopeToken(null, null, ctx.PersonaId, ctx.AgentId, null, null, null);
        var ok = await _retrieval.WriteAsync(scope, text, metadata, ctx.Ct).ConfigureAwait(false);
        return new { ok };
    }
}

