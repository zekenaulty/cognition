namespace Cognition.Clients.Tools;

public class KnowledgeQueryTool : ITool
{
    public string Name => "Knowledge Query";
    public string ClassPath => typeof(KnowledgeQueryTool).FullName! + ", " + typeof(KnowledgeQueryTool).Assembly.GetName().Name;

    public Task<object?> ExecuteAsync(ToolContext ctx, IDictionary<string, object?> args)
    {
        var query = args.TryGetValue("query", out var v) ? v : null;
        var category = (args.TryGetValue("category", out var c) ? c as string : null) ?? "general";
        return Task.FromResult<object?>(new { query, category });
    }
}

