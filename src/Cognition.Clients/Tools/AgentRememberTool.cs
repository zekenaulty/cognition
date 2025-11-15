using System.Linq;
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
        var planId = ResolveGuid(args, "planId") ?? ResolveGuid(args, "PlanId") ?? ResolveGuid(ctx.Metadata, "planId") ?? ResolveGuid(ctx.Metadata, "PlanId");
        var scope = new ScopeToken(
            TenantId: null,
            AppId: null,
            PersonaId: ctx.PersonaId,
            AgentId: ctx.AgentId,
            ConversationId: null,
            PlanId: planId,
            ProjectId: null,
            WorldId: null);
        var ok = await _retrieval.WriteAsync(scope, text, metadata, ctx.Ct).ConfigureAwait(false);
        return new { ok };
    }

    private static Guid? ResolveGuid(IReadOnlyDictionary<string, object?>? source, string key)
    {
        if (source is null) return null;
        if (!source.TryGetValue(key, out var value)) return null;
        return CoerceGuid(value);
    }

    private static Guid? ResolveGuid(IDictionary<string, object?> source, string key)
    {
        if (source is null) return null;
        if (!source.TryGetValue(key, out var value))
        {
            var match = source.FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
            if (string.Equals(match.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = match.Value;
            }
        }

        return CoerceGuid(value);
    }

    private static Guid? CoerceGuid(object? value)
        => value switch
        {
            Guid g when g != Guid.Empty => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => null
        };
}
